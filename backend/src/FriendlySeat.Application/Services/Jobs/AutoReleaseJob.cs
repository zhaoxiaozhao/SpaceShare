using FriendlySeat.Application.Common;
using FriendlySeat.Application.Services;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FriendlySeat.Application.Services.Jobs;

public interface IAutoReleaseJob
{
    Task RunAsync(CancellationToken ct = default);
}

public class AutoReleaseJob : IAutoReleaseJob
{
    private readonly IAppDbContext _db;
    private readonly ConfigService _config;
    private readonly CreditService _credit;
    private readonly INotificationService _notifications;
    private readonly ReservationService _reservationService;
    private readonly ILogger<AutoReleaseJob> _logger;

    private readonly RiskService _risk;

    public AutoReleaseJob(
        IAppDbContext db,
        ConfigService config,
        CreditService credit,
        INotificationService notifications,
        ReservationService reservationService,
        RiskService risk,
        ILogger<AutoReleaseJob> logger)
    {
        _db = db;
        _config = config;
        _credit = credit;
        _notifications = notifications;
        _reservationService = reservationService;
        _risk = risk;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var creditRules = await _config.GetCreditRulesAsync(ct);
        var now = DateTime.UtcNow;

        // 1. 处理到座超时：预约结束仍未确认到座 → no_show（到结束时间才判定为爽约）
        var overdueReservations = await _db.Reservations
            .Include(r => r.Seat)
            .Where(r => r.Status == ReservationStatus.Reserved && r.EndAt < now)
            .ToListAsync(ct);

        foreach (var reservation in overdueReservations)
        {
            _logger.LogInformation("自动释放超时预约 {ReservationId}", reservation.Id);

            reservation.Status = ReservationStatus.NoShow;
            reservation.ExpiredAt = now;

            // 扣信用
            await _credit.AdjustAsync(reservation.UserId, creditRules.NoShowPenalty, "爽约未到", "reservation", reservation.Id, ct);

            // 风控：当天爽约 >= 阈值 记录风险
            var noShowThreshold = await _config.GetIntAsync(ConfigCategory.RiskRules, "no_show_threshold", 2, ct);
            var todayNoShows = await _db.Reservations.CountAsync(
                r => r.UserId == reservation.UserId && r.Status == ReservationStatus.NoShow && r.ExpiredAt >= now.Date, ct);
            if (todayNoShows >= noShowThreshold)
            {
                await _risk.RecordEventAsync(reservation.UserId, "no_show", 20,
                    $"当天爽约{todayNoShows}次", ct);
            }

            await _notifications.SendAsync(reservation.UserId, NotificationType.ReservationExpired,
                "预约未到座", "预约已结束仍未确认到座，本单记为爽约。", null, ct);
        }

        // 1.5 到座提醒：预约已开始但未确认到座，提醒一次（结束仍未到座将视为爽约）
        var toRemind = await _db.Reservations
            .Include(r => r.Seat)
            .Where(r => r.Status == ReservationStatus.Reserved
                && r.StartAt <= now
                && r.EndAt > now)
            .ToListAsync(ct);

        foreach (var reservation in toRemind)
        {
            // 防重：该预约已发过到座提醒则跳过（Data 中带 arrival_reminder:{id} 标记）
            var dataTag = $"arrival_reminder:{reservation.Id}";
            var alreadyReminded = await _db.Notifications
                .AnyAsync(n => n.UserId == reservation.UserId && n.Data != null && n.Data.Contains(dataTag), ct);
            if (alreadyReminded) continue;

            // 座位用短展示编号（如 3F-A-001），避免原始编号过长
            var seatCode = reservation.Seat is not null
                ? await SeatDisplayHelper.ShortCodeAsync(_db, reservation.Seat, ct)
                : "";

            var minutesLeft = (int)Math.Max(0, (reservation.EndAt - now).TotalMinutes);
            var payload = JsonSerializer.Serialize(new
            {
                tag = dataTag,
                seat = seatCode,
                countdown = $"{minutesLeft}分钟",
                deadline = reservation.EndAt.ToUniversalTime()
            });

            await _notifications.SendAsync(reservation.UserId, NotificationType.ArrivalRequired,
                "到座提醒", "你预约的座位已开始计时，请尽快在小程序确认到座；预约结束仍未到座将视为爽约。", payload, ct);
        }

        // 2. 处理到座后未结束但超时：arrived 且超过 end → completed
        var overArrived = await _db.Reservations
            .Where(r => r.Status == ReservationStatus.Arrived && r.EndAt < now)
            .ToListAsync(ct);

        foreach (var reservation in overArrived)
        {
            reservation.Status = ReservationStatus.Completed;
            reservation.CompletedAt = now;

            var session = await _db.SeatSessions.FirstOrDefaultAsync(
                s => s.SeatId == reservation.SeatId && s.UserId == reservation.UserId && s.Status == SeatSessionStatus.Active, ct);
            if (session is not null)
            {
                session.Status = SeatSessionStatus.Completed;
                session.ActualEndAt = now;
            }

            var seat = await _db.Seats.FirstAsync(s => s.Id == reservation.SeatId, ct);
            seat.Status = SeatStatus.Available;
        }

        // 3. 过期 share 标记为 expired
        var expiredShares = await _db.SeatShares
            .Where(s => (s.Status == SeatShareStatus.Available || s.Status == SeatShareStatus.Reserved) && s.EndAt < now)
            .ToListAsync(ct);

        foreach (var share in expiredShares)
        {
            share.Status = SeatShareStatus.Expired;
        }

        // 4. 过期候补：标记过期后，通知同分享下一位候补（候补链条）
        var expiredWaitlists = await _db.ReservationWaitlists
            .Where(w => (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Notified) && w.ExpiredAt.HasValue && w.ExpiredAt < now)
            .ToListAsync(ct);
        var shareIdsToRecheck = new List<long>();
        foreach (var w in expiredWaitlists)
        {
            w.Status = WaitlistStatus.Expired;
            shareIdsToRecheck.Add(w.ShareId);
        }
        if (expiredWaitlists.Count > 0) await _db.SaveChangesAsync(ct);

        // 过期候补对应的分享：若座位仍被该候补预留（Reserved + Hold），恢复 Available 供下一位候补接力
        var sharesToRelease = await _db.SeatShares
            .Where(s => s.Status == SeatShareStatus.Reserved
                && s.HoldForUserId.HasValue
                && expiredWaitlists.Select(w => w.UserId).Contains(s.HoldForUserId.Value))
            .ToListAsync(ct);
        foreach (var share in sharesToRelease)
        {
            share.Status = SeatShareStatus.Available;
            share.HoldForUserId = null;
        }
        if (sharesToRelease.Count > 0) await _db.SaveChangesAsync(ct);

        // 对每条过期候补对应的分享，通知当前队首候补
        foreach (var shareId in shareIdsToRecheck.Distinct())
        {
            await NotifyNextWaitlistAsync(shareId, ct);
            // 无具体座位候补队列时，由范围偏好自动代约兜底
            await _reservationService.TryAutoBookPreferenceAsync(shareId, ct);
        }

        // 5. 清理过期使用会话
        var staleSessions = await _db.SeatSessions
            .Where(s => s.Status == SeatSessionStatus.Active && s.ExpectedEndAt.HasValue && s.ExpectedEndAt.Value.AddHours(2) < now)
            .ToListAsync(ct);
        foreach (var session in staleSessions)
        {
            session.Status = SeatSessionStatus.Completed;
            session.ActualEndAt = now;
        }

        // 5.5 过期候补偏好：超过截止时间仍未自动预约 → Expired（历史无截止时间的也一并失效）
        var expiredPrefs = await _db.WaitlistPreferences
            .Where(p => p.Status == WaitlistPreferenceStatus.Active && (!p.ExpireAt.HasValue || p.ExpireAt < now))
            .ToListAsync(ct);
        foreach (var p in expiredPrefs)
        {
            p.Status = WaitlistPreferenceStatus.Expired;
        }

        // 5.6 过期换座意向：超过有效期仍未匹配 → Expired
        var expiredSwaps = await _db.SeatSwapRequests
            .Where(r => r.Status == SeatSwapStatus.Open && r.ExpireAt < now)
            .ToListAsync(ct);
        foreach (var s in expiredSwaps)
        {
            s.Status = SeatSwapStatus.Expired;
        }

        // 5.7 已结束活动：已发布且结束时间已过 → Finished
        var endedActivities = await _db.Activities
            .Where(a => a.Status == ActivityStatus.Published && a.EndAt < now)
            .ToListAsync(ct);
        foreach (var a in endedActivities)
        {
            a.Status = ActivityStatus.Finished;
        }

        await _db.SaveChangesAsync(ct);

        // 6. 风险晋升检查：风险分达到阈值自动升级处罚（可配置）
        await AutoBanHighRiskUsersAsync(now, ct);
    }

    // 风险分 ≥ 阈值自动封禁（重复爽约/频繁取消等累积导致），封禁并通知用户
    private async Task AutoBanHighRiskUsersAsync(DateTime now, CancellationToken ct)
    {
        var banThreshold = await _config.GetIntAsync(ConfigCategory.RiskRules, "auto_ban_threshold", 80, ct);
        if (banThreshold <= 0) return; // 配置为 0 或负数表示关闭自动封禁

        var highRiskUsers = await _db.Users
            .Where(u => u.Status == UserStatus.Active && u.RiskScore >= banThreshold)
            .ToListAsync(ct);

        foreach (var user in highRiskUsers)
        {
            user.Status = UserStatus.Banned;
            user.UpdatedAt = now;
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("风险分达到 {Threshold}，自动封禁用户 {UserId}", banThreshold, user.Id);
            await _notifications.SendAsync(user.Id, NotificationType.CreditChanged,
                "账号已被封禁", "你的账号因多次违规被系统封禁，如有疑问请联系管理员申诉。", null, ct);
        }
    }

    // 候补链条：通知分享当前队首候补（若有空位），并为其预留座位
    private async Task NotifyNextWaitlistAsync(long shareId, CancellationToken ct)
    {
        var rules = await _config.GetReservationRulesAsync(ct);
        var next = await _db.ReservationWaitlists
            .Where(w => w.ShareId == shareId && w.Status == WaitlistStatus.Waiting)
            .OrderBy(w => w.Position)
            .FirstOrDefaultAsync(ct);
        if (next is null) return;

        var share = await _db.SeatShares.Include(s => s.Seat).FirstOrDefaultAsync(s => s.Id == shareId, ct);
        if (share is null || share.Status != SeatShareStatus.Available) return;

        // 候补优先预约权：座位释放后预留给队首候补，窗口期内只有他能预约
        next.Status = WaitlistStatus.Notified;
        next.NotifiedAt = DateTime.UtcNow;
        next.ExpiredAt = DateTime.UtcNow.AddMinutes(rules.WaitlistWindowMinutes);
        share.Status = SeatShareStatus.Reserved;
        share.HoldForUserId = next.UserId;
        await _db.SaveChangesAsync(ct);

        await _notifications.SendAsync(next.UserId, NotificationType.WaitlistAvailable,
            "候补成功", $"「{share.Seat?.Code}」有空位了，座位已为你预留，请在{rules.WaitlistWindowMinutes}分钟内预约", null, ct);
    }
}
