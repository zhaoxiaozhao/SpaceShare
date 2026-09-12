using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FriendlySeat.Application.Services;

public class ReservationService
{
    private readonly IAppDbContext _db;
    private readonly IDistributedLock _lock;
    private readonly ConfigService _config;
    private readonly INotificationService _notifications;
    private readonly IRedisCache _cache;
    private readonly RiskService _risk;
    private readonly CreditService _credit;
    private readonly ILogger _logger;

    public ReservationService(
        IAppDbContext db,
        IDistributedLock @lock,
        ConfigService config,
        INotificationService notifications,
        IRedisCache cache,
        RiskService risk,
        CreditService credit,
        ILogger<ReservationService> logger)
    {
        _db = db;
        _lock = @lock;
        _config = config;
        _notifications = notifications;
        _cache = cache;
        _risk = risk;
        _credit = credit;
        _logger = logger;
    }

    public async Task<ReservationDto> CreateAsync(long userId, ReservationCreateRequest request, CancellationToken ct = default)
    {
        var rules = await _config.GetReservationRulesAsync(ct);
        var now = DateTime.UtcNow;

        var share = await _db.SeatShares
            .Include(s => s.Seat)
            .Include(s => s.OwnerUser)
            .FirstOrDefaultAsync(s => s.Id == request.ShareId, ct)
            ?? throw AppException.NotFound("分享不存在或已失效");

        if (share.OwnerUserId == userId)
            throw AppException.BadRequest("cannot_reserve_own", "不能预约自己分享的座位");

        // 候补优先预约权：座位被预留给队首候补时，只有该候补用户可以预约
        if (share.HoldForUserId.HasValue && share.HoldForUserId != userId)
            throw AppException.Conflict("share_held_for_waitlist", "该座位已预留给候补用户，暂不可预约");
        if (share.Status != SeatShareStatus.Available && !(share.Status == SeatShareStatus.Reserved && share.HoldForUserId == userId))
            throw AppException.Conflict("share_not_available", "该分享已不可预约");
        // 分享起点为“现在”或未来都可预约（到座确认后使用）；只要求结束时间在未来
        if (share.EndAt <= now)
            throw AppException.BadRequest("share_expired", "该分享已结束，无法预约");
        if (share.EndAt - now < TimeSpan.FromMinutes(rules.MinMinutes))
            throw AppException.BadRequest("share_too_short", "该分享剩余时间过短，无法预约");
        if (share.StartAt > now.AddHours(rules.MaxAdvanceHours))
            throw AppException.BadRequest("too_early", $"最多提前{rules.MaxAdvanceHours}小时预约");

        // 用户信用与风控门槛
        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        if (user.Status == UserStatus.Banned)
            throw AppException.Forbidden("账号已被封禁");
        if (user.CreditScore < 30)
            throw AppException.Forbidden("信用分过低，暂时无法预约");
        if (await _risk.IsRestrictedAsync(userId, ct))
            throw AppException.Forbidden("账号存在风险记录，暂时无法预约");

        // 关键并发控制：分布式锁 + 事务
        var lockKey = $"seat:reservation:{share.SeatId}:{share.StartAt:yyyyMMdd}";
        await using var handle = await _lock.AcquireAsync(lockKey, TimeSpan.FromSeconds(15), ct);
        if (handle is null || !handle.IsAcquired)
            throw AppException.Conflict("seat_busy", "操作过于频繁，请稍后重试");

        // 事务内二次校验，数据库是最终事实来源
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var freshShare = await _db.SeatShares.FirstAsync(s => s.Id == request.ShareId, ct);
        if (freshShare.Status != SeatShareStatus.Available
            && !(freshShare.Status == SeatShareStatus.Reserved && freshShare.HoldForUserId == userId))
            throw AppException.Conflict("share_not_available", "该分享已被预约");

        // 校验同一用户有效预约数
        var activeCount = await _db.Reservations.CountAsync(
            r => r.UserId == userId
                && r.EndAt > now
                && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Arrived), ct);
        if (activeCount >= rules.MaxActiveReservations)
            throw AppException.Conflict("too_many_active", "同一时间只能有一个有效预约");

        // 校验每日预约次数
        var todayStart = now.Date;
        var todayCount = await _db.Reservations.CountAsync(
            r => r.UserId == userId && r.ReservedAt >= todayStart
                && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Arrived
                    || r.Status == ReservationStatus.Using || r.Status == ReservationStatus.Completed), ct);
        if (todayCount >= rules.DailyReservationLimit)
            throw AppException.Conflict("daily_limit", "今日预约次数已达上限");

        // 校验座位在该时间段没有其他已占用预约（理论上被 share 互斥覆盖，双保险）
        var overlap = await _db.Reservations.AnyAsync(
            r => r.SeatId == freshShare.SeatId
                && freshShare.StartAt < r.EndAt && freshShare.EndAt > r.StartAt
                && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Arrived
                    || r.Status == ReservationStatus.Using), ct);
        if (overlap)
            throw AppException.Conflict("seat_overlap", "该座位在该时间段已被占用");

        // 标记 share 已预约
        freshShare.Status = SeatShareStatus.Reserved;
        freshShare.HoldForUserId = null;

        // 候补状态：若当前预约者为被通知的候补，将其候补记录置为 Reserved（代表已成功预约）
        var heldWaitlist = await _db.ReservationWaitlists
            .FirstOrDefaultAsync(w => w.ShareId == freshShare.Id && w.UserId == userId && w.Status == WaitlistStatus.Notified, ct);
        if (heldWaitlist is not null)
        {
            heldWaitlist.Status = WaitlistStatus.Reserved;
        }

        var reservation = new Reservation
        {
            SeatId = freshShare.SeatId,
            ShareId = freshShare.Id,
            UserId = userId,
            StartAt = freshShare.StartAt,
            EndAt = freshShare.EndAt,
            Status = ReservationStatus.Reserved,
            ReservedAt = now
        };
        _db.Reservations.Add(reservation);

        // 同步等待中的候补自动失效（超出窗口部分保留）
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // 通知分享者
        if (share.OwnerUserId != userId)
        {
            await _notifications.SendAsync(share.OwnerUserId, NotificationType.ReservationCreated,
                "你的分享被预约", $"友邻 {user.Nickname ?? "某位友邻"} 预约了你的座位「{share.Seat?.Code}」", null, ct);
        }

        await InvalidateVenueCacheAsync(share.SeatId, ct);

        return await GetDtoAsync(reservation.Id, ct) ?? throw AppException.NotFound();
    }

    public async Task CancelAsync(long reservationId, long userId, CancellationToken ct = default)
    {
        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId, ct)
            ?? throw AppException.NotFound("预约不存在");

        if (reservation.UserId != userId)
            throw AppException.Forbidden("只能取消自己的预约");

        if (reservation.Status != ReservationStatus.Reserved)
            throw AppException.BadRequest("reservation_not_active", "只有待到达的预约可以取消");

        var now = DateTime.UtcNow;
        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledAt = now;

        // 释放对应的 share
        if (reservation.ShareId.HasValue)
        {
            var share = await _db.SeatShares.FirstAsync(s => s.Id == reservation.ShareId.Value, ct);
            if (share.Status == SeatShareStatus.Reserved)
            {
                share.Status = SeatShareStatus.Available;
                share.HoldForUserId = null;
            }
        }

        await _db.SaveChangesAsync(ct);
        await InvalidateVenueCacheAsync(reservation.SeatId, ct);

        // 风控：统计近24小时取消次数，连续取消触发风险记录
        var cancelThreshold = await _config.GetIntAsync(ConfigCategory.RiskRules, "cancel_threshold", 5, ct);
        var recentCancels = await _db.Reservations.CountAsync(
            r => r.UserId == userId && r.Status == ReservationStatus.Cancelled && r.CancelledAt >= now.AddHours(-24), ct);
        if (recentCancels >= cancelThreshold)
        {
            await _risk.RecordEventAsync(userId, "repeated_cancel", 10,
                $"24小时内取消预约{recentCancels}次", ct);
        }

        // 通知候补
        await NotifyNextWaitlistAsync(reservation.ShareId, ct);

        // 范围偏好自动代约：无具体座位候补队列时（share 仍 Available），按最早提交的偏好直接预约
        await TryAutoBookPreferenceAsync(reservation.ShareId, ct);
    }

    public async Task<ArrivalResultDto> ArriveAsync(long reservationId, long userId, double? lat, double? lng, CancellationToken ct = default)
    {
        var reservation = await _db.Reservations
            .Include(r => r.Seat)
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct)
            ?? throw AppException.NotFound("预约不存在");

        if (reservation.UserId != userId)
            throw AppException.Forbidden("只能确认自己的预约");

        var now = DateTime.UtcNow;

        if (reservation.Status == ReservationStatus.Arrived || reservation.Status == ReservationStatus.Using)
            return new ArrivalResultDto { ReservationId = reservation.Id, Confirmed = true, Message = "已到座" };
        if (reservation.Status != ReservationStatus.Reserved)
            throw AppException.BadRequest("reservation_not_pending", "当前预约状态无法确认到座");
        if (now < reservation.StartAt.AddMinutes(-30))
            throw AppException.BadRequest("arrive_too_early", "未到到座时间");

        // 到达校验：在场馆允许范围
        var seat = reservation.Seat;
        if (seat is not null)
        {
            var zone = await _db.Zones.Include(z => z.Floor).ThenInclude(f => f.Venue)
                .FirstAsync(z => z.Id == seat.ZoneId, ct);
            var venue = zone.Floor!.Venue!;
            if (lat.HasValue && lng.HasValue && venue.Latitude.HasValue && venue.Longitude.HasValue)
            {
                var dist = HaversineKm(lat.Value, lng.Value, venue.Latitude.Value, venue.Longitude.Value);
                if (dist > 5.0)
                    throw AppException.BadRequest("arrive_out_of_range", "你不在场馆附近，无法确认到座");
            }
        }

        reservation.Status = ReservationStatus.Arrived;
        reservation.ArrivedAt = now;

        // 创建使用会话
        var existingSession = await _db.SeatSessions.FirstOrDefaultAsync(
            s => s.SeatId == reservation.SeatId && s.UserId == userId && s.Status == SeatSessionStatus.Active, ct);
        if (existingSession is null)
        {
            _db.SeatSessions.Add(new SeatSession
            {
                SeatId = reservation.SeatId,
                UserId = userId,
                StartedAt = now,
                ArrivalAt = now,
                ExpectedEndAt = reservation.EndAt,
                Status = SeatSessionStatus.Active,
                CreatedAt = now
            });
        }

        var seatEntity = await _db.Seats.FirstAsync(s => s.Id == reservation.SeatId, ct);
        seatEntity.Status = SeatStatus.Occupied;

        // 到座后，对应分享进入使用中（Active），前端状态从“待到达”变为“使用中”
        if (reservation.ShareId.HasValue)
        {
            var shareEntity = await _db.SeatShares
                .FirstOrDefaultAsync(s => s.Id == reservation.ShareId.Value && s.Status == SeatShareStatus.Reserved, ct);
            if (shareEntity is not null)
            {
                shareEntity.Status = SeatShareStatus.Active;
            }
        }

        await _db.SaveChangesAsync(ct);
        await InvalidateVenueCacheAsync(reservation.SeatId, ct);

        // 信用加分
        await AwardCreditAsync(userId, "arrival", "到座确认", reservation.Id, ct);

        // 友邻贡献：预约者准时到座 +1；分享者的分享被真实使用，帮助人数 +1
        await _credit.TrackContributionAsync(userId, "on_time", 0, ct);
        if (reservation.ShareId.HasValue)
        {
            var shareOwner = await _db.SeatShares
                .Where(s => s.Id == reservation.ShareId.Value)
                .Select(s => (long?)s.OwnerUserId)
                .FirstOrDefaultAsync(ct);
            if (shareOwner.HasValue)
            {
                await _credit.TrackContributionAsync(shareOwner.Value, "helped", 0, ct);
            }
        }

        return new ArrivalResultDto { ReservationId = reservation.Id, Confirmed = true, Message = "欢迎到座" };
    }

    /// <summary>
    /// 扫码/输码核销到座：预约者输入分享者出示的核销码完成到座确认（无需 GPS）
    /// </summary>
    public async Task<ArrivalResultDto> CheckInByCodeAsync(long reservationId, long userId, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw AppException.BadRequest("code_required", "请输入核销码");

        var reservation = await _db.Reservations
            .Include(r => r.Seat)
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct)
            ?? throw AppException.NotFound("预约不存在");
        if (reservation.UserId != userId)
            throw AppException.Forbidden("只能核销自己的预约");

        // 防暴力猜测：错误 ≥5 次锁定 30 分钟
        var failKey = $"checkin:fail:{reservationId}:{userId}";
        var failCount = await _cache.GetAsync<int>(failKey, ct);
        if (failCount >= 5)
            throw AppException.BadRequest("checkin_locked", "错误次数过多，请30分钟后再试");

        var share = reservation.ShareId.HasValue
            ? await _db.SeatShares.FirstOrDefaultAsync(s => s.Id == reservation.ShareId.Value, ct)
            : null;
        if (share is null || string.IsNullOrEmpty(share.CheckInCode))
            throw AppException.BadRequest("checkin_code_missing", "该预约不支持核销码确认，请使用定位确认到座");

        if (share.CheckInCode != code.Trim())
        {
            await _cache.SetAsync(failKey, failCount + 1, TimeSpan.FromMinutes(30), ct);
            throw AppException.BadRequest("checkin_code_wrong", $"核销码不正确（还可尝试 {4 - failCount} 次）");
        }

        // 核销成功：清除失败计数，走标准到座流程（无 GPS，核销码即到场凭证）
        await _cache.RemoveAsync(failKey, ct);
        return await ArriveAsync(reservationId, userId, null, null, ct);
    }

    public async Task CompleteAsync(long reservationId, long userId, CancellationToken ct = default)
    {
        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId, ct)
            ?? throw AppException.NotFound("预约不存在");
        if (reservation.UserId != userId)
            throw AppException.Forbidden("只能结束自己的预约");

        if (reservation.Status != ReservationStatus.Arrived && reservation.Status != ReservationStatus.Using)
            throw AppException.BadRequest("reservation_not_using", "当前状态无法结束");

        var now = DateTime.UtcNow;
        reservation.Status = ReservationStatus.Completed;
        reservation.CompletedAt = now;

        var session = await _db.SeatSessions.FirstOrDefaultAsync(
            s => s.SeatId == reservation.SeatId && s.UserId == userId && s.Status == SeatSessionStatus.Active, ct);
        if (session is not null)
        {
            session.Status = SeatSessionStatus.Completed;
            session.ActualEndAt = now;
        }

        var seat = await _db.Seats.FirstAsync(s => s.Id == reservation.SeatId, ct);
        seat.Status = SeatStatus.Available;

        // 使用结束后，对应分享标记为已完成
        if (reservation.ShareId.HasValue)
        {
            var shareEntity = await _db.SeatShares
                .FirstOrDefaultAsync(s => s.Id == reservation.ShareId.Value && s.Status == SeatShareStatus.Active, ct);
            if (shareEntity is not null)
            {
                shareEntity.Status = SeatShareStatus.Completed;
            }
        }

        await _db.SaveChangesAsync(ct);
        await InvalidateVenueCacheAsync(reservation.SeatId, ct);

        await AwardCreditAsync(userId, "completion", "正常完成预约", reservation.Id, ct);
        await _notifications.SendAsync(userId, NotificationType.System,
            "交接这一席", "感谢你守约。可以把接下来的空闲时间分享给下一位友邻。", null, ct);
    }

    public async Task<MyReservationSummaryDto> GetMySummaryAsync(long userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var upcoming = await _db.Reservations
            .Where(r => r.UserId == userId && r.EndAt > now
                && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Arrived))
            .Include(r => r.Seat!)
                .ThenInclude(s => s.Zone!)
                    .ThenInclude(z => z.Floor!)
                        .ThenInclude(f => f.Venue)
            .OrderBy(r => r.StartAt)
            .Select(r => new ReservationDto
            {
                Id = r.Id,
                SeatId = r.SeatId,
                SeatCode = r.Seat!.Code,
                VenueName = r.Seat.Zone!.Floor!.Venue!.Name,
                ShareId = r.ShareId,
                UserId = r.UserId,
                StartAt = r.StartAt,
                EndAt = r.EndAt,
                Status = r.Status.ToString(),
                ReservedAt = r.ReservedAt,
                ArrivedAt = r.ArrivedAt,
                CancelledAt = r.CancelledAt,
                CompletedAt = r.CompletedAt,
                ExpiredAt = r.ExpiredAt
            })
            .ToListAsync(ct);

        var history = await _db.Reservations
            .Where(r => r.UserId == userId && (r.EndAt <= now
                || r.Status == ReservationStatus.Cancelled
                || r.Status == ReservationStatus.NoShow
                || r.Status == ReservationStatus.Completed
                || r.Status == ReservationStatus.Expired))
            .Include(r => r.Seat!)
                .ThenInclude(s => s.Zone!)
                    .ThenInclude(z => z.Floor!)
                        .ThenInclude(f => f.Venue)
            .OrderByDescending(r => r.StartAt)
            .Take(50)
            .Select(r => new ReservationDto
            {
                Id = r.Id,
                SeatId = r.SeatId,
                SeatCode = r.Seat!.Code,
                VenueName = r.Seat.Zone!.Floor!.Venue!.Name,
                ShareId = r.ShareId,
                UserId = r.UserId,
                StartAt = r.StartAt,
                EndAt = r.EndAt,
                Status = r.Status.ToString(),
                ReservedAt = r.ReservedAt,
                ArrivedAt = r.ArrivedAt,
                CancelledAt = r.CancelledAt,
                CompletedAt = r.CompletedAt,
                ExpiredAt = r.ExpiredAt
            })
            .ToListAsync(ct);

        var shareService = await _db.SeatShares
            .Where(s => s.OwnerUserId == userId && s.Status != SeatShareStatus.Cancelled && s.Status != SeatShareStatus.Expired)
            .Include(s => s.Seat!)
                .ThenInclude(s => s.Zone!)
                    .ThenInclude(z => z.Floor!)
                        .ThenInclude(f => f.Venue)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SeatShareDto
            {
                Id = s.Id,
                SeatId = s.SeatId,
                SeatCode = s.Seat!.Code,
                VenueName = s.Seat.Zone!.Floor!.Venue!.Name,
                OwnerUserId = s.OwnerUserId,
                StartAt = s.StartAt,
                EndAt = s.EndAt,
                Status = s.Status.ToString(),
                Note = s.Note,
                AllowContact = s.AllowContact,
                CheckInCode = s.CheckInCode,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(ct);

        // 统一填充展示编号与楼层（预约 + 我的分享）
        var allSeatIds = upcoming.Select(r => r.SeatId).Concat(history.Select(r => r.SeatId))
            .Concat(shareService.Select(s => s.SeatId)).Distinct().ToList();
        var seatZone = await _db.Seats
            .Where(s => allSeatIds.Contains(s.Id))
            .Select(s => new { s.Id, s.ZoneId })
            .ToDictionaryAsync(s => s.Id, s => s.ZoneId, ct);
        var zoneMap = await SeatDisplayHelper.BuildZoneMapAsync(_db, seatZone.Values, ct);

        foreach (var r in upcoming.Concat(history))
        {
            if (seatZone.TryGetValue(r.SeatId, out var zoneId) && zoneMap.TryGetValue(zoneId, out var info))
            {
                r.DisplayCode = SeatDisplayHelper.DisplayCode(info.Letter, r.SeatCode);
                r.FloorName = info.FloorName;
                r.AreaName = info.AreaName;
            }
        }
        foreach (var s in shareService)
        {
            if (seatZone.TryGetValue(s.SeatId, out var zoneId) && zoneMap.TryGetValue(zoneId, out var info))
            {
                s.DisplayCode = SeatDisplayHelper.DisplayCode(info.Letter, s.SeatCode);
                s.FloorName = info.FloorName;
                s.AreaName = info.AreaName;
            }
        }

        return new MyReservationSummaryDto { Upcoming = upcoming, History = history, MyShares = shareService };
    }

    // 候补优先预约权：座位释放后通知并预留队首候补（管理员强制取消/爽约释放等场景复用）
    public async Task NotifyNextWaitlistAsync(long? shareId, CancellationToken ct)
    {
        if (!shareId.HasValue) return;

        var rules = await _config.GetReservationRulesAsync(ct);
        var next = await _db.ReservationWaitlists
            .Where(w => w.ShareId == shareId.Value && w.Status == WaitlistStatus.Waiting)
            .OrderBy(w => w.Position)
            .FirstOrDefaultAsync(ct);

        if (next is null) return;

        var share = await _db.SeatShares.Include(s => s.Seat).FirstAsync(s => s.Id == shareId.Value, ct);
        if (share.Status != SeatShareStatus.Available) return;

        // 候补优先预约权：座位释放后预留给队首候补，10 分钟内只有他能预约
        next.Status = WaitlistStatus.Notified;
        next.NotifiedAt = DateTime.UtcNow;
        next.ExpiredAt = DateTime.UtcNow.AddMinutes(rules.WaitlistWindowMinutes);
        share.Status = SeatShareStatus.Reserved;
        share.HoldForUserId = next.UserId;
        await _db.SaveChangesAsync(ct);

        await _notifications.SendAsync(next.UserId, NotificationType.WaitlistAvailable,
            "候补成功", $"「{share.Seat?.Code}」有空位了，座位已为你预留，请在{rules.WaitlistWindowMinutes}分钟内预约", null, ct);
    }

    // 范围偏好自动代约：share 仍可预约时，按提交顺序为最早匹配的偏好用户直接预约（无具体候补队列时）
    public async Task TryAutoBookPreferenceAsync(long? shareId, CancellationToken ct)
    {
        if (!shareId.HasValue) return;

        var share = await _db.SeatShares
            .Include(s => s.Seat)
                .ThenInclude(s => s!.Zone)
            .FirstOrDefaultAsync(s => s.Id == shareId.Value, ct);
        if (share is null || share.Status != SeatShareStatus.Available) return;

        var seat = share.Seat;
        if (seat?.Zone is null) return;

        var venueId = await _db.Floors
            .Where(f => f.Id == seat.Zone!.FloorId)
            .Select(f => f.VenueId)
            .FirstOrDefaultAsync(ct);

        var prefs = await _db.WaitlistPreferences
            .Where(p => p.Status == WaitlistPreferenceStatus.Active
                && p.VenueId == venueId
                && (!p.FloorId.HasValue || p.FloorId == seat.Zone!.FloorId)
                && (!p.AreaId.HasValue || p.AreaId == seat.Zone!.AreaId))
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);

        foreach (var pref in prefs)
        {
            if (pref.UserId == share.OwnerUserId) continue;
            if (!PreferenceMatchesSeat(pref.Preference, seat)) continue;

            try
            {
                var reservation = await CreateAsync(pref.UserId, new ReservationCreateRequest { ShareId = share.Id }, ct);
                pref.Status = WaitlistPreferenceStatus.Booked;
                pref.BookedAt = DateTime.UtcNow;
                pref.ReservationId = reservation.Id;
                await _db.SaveChangesAsync(ct);

                await _notifications.SendAsync(pref.UserId, NotificationType.WaitlistAvailable,
                    "候补成功，已自动预约", $"「{seat.Code}」有空位，已自动为你预约，按时到座确认即可。", null, ct);
                return;
            }
            catch (AppException)
            {
                // 该偏好用户当前无法预约（已有申请/信用不足/座位被抢等），尝试下一位
                continue;
            }
        }
    }

    private static bool PreferenceMatchesSeat(string preference, Seat seat)
    {
        return preference switch
        {
            "window" => seat.Window,
            "socket" => seat.PowerSocket,
            "quiet" => seat.QuietLevel == 3,
            _ => true
        };
    }

    private async Task AwardCreditAsync(long userId, string referenceType, string reason, long referenceId, CancellationToken ct)
    {
        var rules = await _config.GetCreditRulesAsync(ct);
        var change = referenceType switch
        {
            "arrival" => rules.ArrivalBonus,
            "completion" => rules.CompletionBonus,
            _ => 0
        };
        if (change == 0) return;

        // 信用修复机制：连续守约达到阈值额外加分（如每连续 5 次到座 +2），帮助低分用户恢复
        var bonusReasons = new List<string>();
        if (referenceType == "arrival" && rules.StreakBonusInterval > 0 && rules.StreakBonus != 0)
        {
            var recent = await _db.Reservations
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.StartAt)
                .Take(rules.StreakBonusInterval)
                .Select(r => r.Status)
                .ToListAsync(ct);
            // 最近 N 次预约全部守约（到座/使用/完成）且数量足够 → 触发奖励
            var goodStatuses = new[] { ReservationStatus.Arrived, ReservationStatus.Using, ReservationStatus.Completed };
            if (recent.Count >= rules.StreakBonusInterval && recent.All(s => goodStatuses.Contains(s)))
            {
                change += rules.StreakBonus;
                bonusReasons.Add($"连续守约{recent.Count}次奖励 +{rules.StreakBonus}");
            }
        }

        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        var newScore = Math.Clamp(user.CreditScore + change, 0, rules.MaxScore);

        var fullReason = bonusReasons.Count > 0 ? $"{reason}（{string.Join("，", bonusReasons)}）" : reason;
        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = userId,
            Change = change,
            Reason = fullReason,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            CreatedAt = DateTime.UtcNow
        });
        user.CreditScore = newScore;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<ReservationDto?> GetDtoAsync(long id, CancellationToken ct)
    {
        return await _db.Reservations
            .Where(r => r.Id == id)
            .Include(r => r.Seat!)
                .ThenInclude(s => s.Zone!)
                    .ThenInclude(z => z.Floor!)
                        .ThenInclude(f => f.Venue)
            .Select(r => new ReservationDto
            {
                Id = r.Id,
                SeatId = r.SeatId,
                SeatCode = r.Seat!.Code,
                VenueName = r.Seat.Zone!.Floor!.Venue!.Name,
                ShareId = r.ShareId,
                UserId = r.UserId,
                StartAt = r.StartAt,
                EndAt = r.EndAt,
                Status = r.Status.ToString(),
                ReservedAt = r.ReservedAt,
                ArrivedAt = r.ArrivedAt,
                CancelledAt = r.CancelledAt,
                CompletedAt = r.CompletedAt,
                ExpiredAt = r.ExpiredAt
            })
            .FirstOrDefaultAsync(ct);
    }

    private async Task InvalidateVenueCacheAsync(long seatId, CancellationToken ct)
    {
        var venueIds = await _db.Zones
            .Where(z => z.Seats.Any(s => s.Id == seatId))
            .Select(z => z.Floor!.VenueId)
            .Distinct()
            .ToListAsync(ct);
        foreach (var venueId in venueIds)
        {
            await _cache.RemoveAsync(CacheKeys.Venue(venueId), ct);
        }
    }

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double r = 6371.0;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLng = (lng2 - lng1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * r * Math.Asin(Math.Sqrt(a));
    }
}
