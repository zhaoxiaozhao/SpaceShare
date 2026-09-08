using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FriendlySeat.Application.Services;

public class ShareService
{
    private readonly IAppDbContext _db;
    private readonly ConfigService _config;
    private readonly INotificationService _notifications;
    private readonly IRedisCache _cache;
    private readonly RiskService _risk;
    private readonly CreditService _credit;
    private readonly ILogger _logger;

    public ShareService(IAppDbContext db, ConfigService config, INotificationService notifications, IRedisCache cache, RiskService risk, CreditService credit, ILogger<ShareService> logger)
    {
        _db = db;
        _config = config;
        _notifications = notifications;
        _cache = cache;
        _risk = risk;
        _credit = credit;
        _logger = logger;
    }

    public async Task<SeatShareDto> CreateShareAsync(long userId, ShareCreateRequest request, CancellationToken ct = default)
    {
        var rules = await _config.GetReservationRulesAsync(ct);
        var now = DateTime.UtcNow;

        // 信用与风控拦截：低信用/高风险/封禁用户不能分享座位
        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        if (user.Status == UserStatus.Banned)
            throw AppException.Forbidden("账号已被封禁，无法分享座位");
        if (user.CreditScore < 30)
            throw AppException.Forbidden("信用分过低，暂时无法分享座位");
        if (await _risk.IsRestrictedAsync(userId, ct))
            throw AppException.Forbidden("账号存在风险记录，暂时无法分享座位");

        if (request.EndAt <= request.StartAt)
            throw AppException.BadRequest("share_time_invalid", "共享结束时间必须晚于开始时间");
        if ((request.EndAt - request.StartAt).TotalMinutes < rules.MinMinutes)
            throw AppException.BadRequest("share_too_short", $"共享时长不能少于{rules.MinMinutes}分钟");
        // 分享起点允许为“现在”或未来（前端默认从现在开始；也支持分享未来时段）

        // 备注仅限座位属性描述，拦截联系方式与社交类内容（整改要求：备注不演变为社交入口）
        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            var note = request.Note.Trim();
            if (ContainsContactInfo(note))
                throw AppException.BadRequest("note_contact_forbidden", "备注中不允许包含联系方式");
            request.Note = note;
        }

        var seat = await _db.Seats
            .Include(s => s.Zone)
            .FirstOrDefaultAsync(s => s.Id == request.SeatId, ct)
            ?? throw AppException.NotFound("座位不存在");

        // 不再要求“确认到座”：平台无法核验分享者是否真的在使用该座位，
        // 分享本身只是把某位置标记为可预约，真实性由预约者到座时自行核验/举报。

        // 时间段不能与既有可用分享重叠
        var conflict = await _db.SeatShares.AnyAsync(
            s => s.SeatId == request.SeatId
                && s.Status == SeatShareStatus.Available
                && request.StartAt < s.EndAt && request.EndAt > s.StartAt, ct);
        if (conflict)
            throw AppException.Conflict("share_overlap", "该座位在该时间段已有其他分享");

        var share = new SeatShare
        {
            SeatId = request.SeatId,
            OwnerUserId = userId,
            SourceSessionId = null,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Status = SeatShareStatus.Available,
            Note = request.Note,
            AllowContact = request.AllowContact,
            CheckInCode = GenerateCheckInCode(),
            CreatedAt = now
        };

        _db.SeatShares.Add(share);
        await _db.SaveChangesAsync(ct);

        // 友邻贡献：累计分享次数与分享时长
        var shareHours = Math.Round((request.EndAt - request.StartAt).TotalHours, 1);
        await _credit.TrackContributionAsync(userId, "share_created", shareHours, ct);

        await InvalidateSeatCacheAsync(request.SeatId, ct);

        return await GetShareDtoAsync(share.Id, ct) ?? throw AppException.NotFound();
    }

    public async Task<List<SeatShareDto>> GetSharesBySeatIdsAsync(List<long> seatIds, CancellationToken ct = default)
    {
        if (seatIds.Count == 0) return new List<SeatShareDto>();

        var rules = await _config.GetReservationRulesAsync(ct);
        var now = DateTime.UtcNow;
        var shares = await _db.SeatShares
            .Where(s => seatIds.Contains(s.SeatId)
                && s.Status != SeatShareStatus.Cancelled && s.Status != SeatShareStatus.Expired
                && s.EndAt > now)
            .Include(s => s.Seat!)
                .ThenInclude(s => s.Zone!)
                    .ThenInclude(z => z.Floor!)
                        .ThenInclude(f => f.Venue)
            .OrderBy(s => s.StartAt)
            .Select(s => new SeatShareDto
            {
                Id = s.Id,
                SeatId = s.SeatId,
                SeatCode = s.Seat!.Code,
                VenueName = s.Seat.Zone!.Floor!.Venue!.Name,
                FloorName = s.Seat.Zone!.Floor!.Name,
                AreaName = s.Seat.Zone!.Area != null ? s.Seat.Zone.Area.Name : null,
                OwnerUserId = s.OwnerUserId,
                OwnerNickname = s.OwnerUser!.Nickname,
                StartAt = s.StartAt,
                EndAt = s.EndAt,
                Status = s.Status.ToString(),
                Note = s.Note,
                AllowContact = s.AllowContact,
                CreatedAt = s.CreatedAt,
                IsReservable = s.Status == SeatShareStatus.Available && s.StartAt > now
                    && s.StartAt <= now.AddHours(rules.MaxAdvanceHours)
                    && (s.EndAt - now) >= TimeSpan.FromMinutes(rules.MinMinutes)
            })
            .ToListAsync(ct);

        await EnrichDisplayCodesAsync(shares, ct);
        return shares;
    }

    /// <summary>计算区块字母并生成展示编号（B区-002）</summary>
    private async Task EnrichDisplayCodesAsync(List<SeatShareDto> shares, CancellationToken ct)
    {
        if (shares.Count == 0) return;

        var seatIds = shares.Select(s => s.SeatId).Distinct().ToList();
        var seats = await _db.Seats
            .Where(s => seatIds.Contains(s.Id))
            .Select(s => new { s.Id, s.ZoneId, s.Code })
            .ToListAsync(ct);

        var zoneIds = seats.Select(s => s.ZoneId).Distinct().ToList();
        var zones = await _db.Zones
            .Where(z => zoneIds.Contains(z.Id))
            .Select(z => new { z.Id, z.FloorId, z.AreaId, z.SortOrder, z.OffsetX })
            .ToListAsync(ct);
        var floorIds = zones.Select(z => z.FloorId).Distinct().ToList();
        var floors = await _db.Floors
            .Where(f => floorIds.Contains(f.Id))
            .Include(f => f.Areas)
            .Include(f => f.Zones)
            .ToListAsync(ct);

        // zoneId -> 区块字母：同一楼层内唯一，按 区域排序 → 区块排序 统一编号
        static IOrderedEnumerable<Zone> OrderZones(IEnumerable<Zone> zones) =>
            zones.OrderBy(z => z.SortOrder).ThenBy(z => z.OffsetX).ThenBy(z => z.Id);

        var letterMap = new Dictionary<long, char>();
        var floorNameMap = new Dictionary<long, string>();
        var areaNameMap = new Dictionary<long, string>();
        foreach (var floor in floors.OrderBy(f => f.SortOrder))
        {
            var ordered = new List<Zone>();
            foreach (var area in floor.Areas.OrderBy(a => a.SortOrder))
            {
                var zg = OrderZones(floor.Zones.Where(z => z.AreaId == area.Id)).ToList();
                foreach (var zone in zg)
                {
                    floorNameMap[zone.Id] = floor.Name;
                    areaNameMap[zone.Id] = area.Name;
                }
                ordered.AddRange(zg);
            }
            var unassigned = OrderZones(floor.Zones.Where(z => z.AreaId == null)).ToList();
            foreach (var zone in unassigned)
            {
                floorNameMap[zone.Id] = floor.Name;
                areaNameMap[zone.Id] = string.Empty;
            }
            ordered.AddRange(unassigned);

            for (var i = 0; i < ordered.Count; i++)
            {
                letterMap[ordered[i].Id] = (char)('A' + i);
            }
        }

        var seatZoneMap = seats.ToDictionary(s => s.Id, s => s.ZoneId);
        foreach (var share in shares)
        {
            if (!seatZoneMap.TryGetValue(share.SeatId, out var zoneId)) continue;
            var letter = letterMap.TryGetValue(zoneId, out var c) ? c : 'A';
            var seatNo = share.SeatCode.Split('-').LastOrDefault() ?? share.SeatCode;
            share.DisplayCode = $"{letter}区-{seatNo}";
            share.FloorName = floorNameMap.GetValueOrDefault(zoneId) ?? string.Empty;
            share.AreaName = areaNameMap.GetValueOrDefault(zoneId) ?? string.Empty;
        }
    }

    public async Task<List<SeatShareDto>> GetMySharesAsync(long userId, CancellationToken ct = default)
    {
        var shares = await _db.SeatShares
            .Where(s => s.OwnerUserId == userId)
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
                OwnerNickname = null,
                StartAt = s.StartAt,
                EndAt = s.EndAt,
                Status = s.Status.ToString(),
                Note = s.Note,
                AllowContact = s.AllowContact,
                CheckInCode = s.CheckInCode,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(ct);

        await EnrichDisplayCodesAsync(shares, ct);
        return shares;
    }

    public async Task<ShareDetailDto?> GetShareAsync(long id, long? userId, CancellationToken ct = default)
    {
        var share = await _db.SeatShares
            .Include(s => s.Seat!)
                .ThenInclude(s => s.Zone!)
                    .ThenInclude(z => z.Floor!)
                        .ThenInclude(f => f.Venue)
            .Include(s => s.Seat!)
                .ThenInclude(s => s.Zone!)
                    .ThenInclude(z => z.Area)
            .Include(s => s.OwnerUser)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (share is null) return null;

        var now = DateTime.UtcNow;
        var rules = await _config.GetReservationRulesAsync(ct);
        var waitlistCount = await _db.ReservationWaitlists.CountAsync(
            w => w.ShareId == share.Id && w.Status == WaitlistStatus.Waiting, ct);

        var isReservable = share.Status == SeatShareStatus.Available && share.StartAt > now
            && share.StartAt <= now.AddHours(rules.MaxAdvanceHours)
            && (share.EndAt - now) >= TimeSpan.FromMinutes(rules.MinMinutes);

        var dto = new ShareDetailDto
        {
            Id = share.Id,
            SeatId = share.SeatId,
            SeatCode = share.Seat!.Code,
            VenueName = share.Seat.Zone!.Floor!.Venue!.Name,
            FloorName = share.Seat.Zone!.Floor!.Name,
            AreaName = share.Seat.Zone!.Area != null ? share.Seat.Zone.Area.Name : null,
            OwnerUserId = share.OwnerUserId,
            OwnerNickname = share.OwnerUser?.Nickname,
            StartAt = share.StartAt,
            EndAt = share.EndAt,
            Status = share.Status.ToString(),
            Note = share.Note,
            AllowContact = share.AllowContact,
            CreatedAt = share.CreatedAt,
            WaitlistCount = waitlistCount,
            IsMine = share.OwnerUserId == userId,
            IsReservable = isReservable
        };

        // 核销码仅分享者本人可见（防止他人代打卡）
        if (dto.IsMine)
        {
            dto.CheckInCode = share.CheckInCode;
        }

        // 展示编号（B区-002）
        var letter = await GetZoneLetterAsync(share.SeatId, ct);
        var seatNo = share.Seat!.Code.Split('-').LastOrDefault() ?? share.Seat!.Code;
        dto.DisplayCode = $"{letter}区-{seatNo}";
        return dto;
    }

    private async Task<char> GetZoneLetterAsync(long seatId, CancellationToken ct)
    {
        var zoneId = await _db.Seats.Where(s => s.Id == seatId).Select(s => (long?)s.ZoneId).FirstOrDefaultAsync(ct);
        if (zoneId == null) return 'A';

        var floor = await _db.Floors
            .Include(f => f.Areas)
            .Include(f => f.Zones)
            .FirstOrDefaultAsync(f => f.Zones.Any(z => z.Id == zoneId.Value), ct);
        if (floor is null) return 'A';

        // 同一楼层内区块字母唯一：按 区域排序 → 区块排序（SortOrder → OffsetX → Id）统一编号
        static IOrderedEnumerable<Zone> OrderZones(IEnumerable<Zone> zones) =>
            zones.OrderBy(z => z.SortOrder).ThenBy(z => z.OffsetX).ThenBy(z => z.Id);

        var ordered = new List<Zone>();
        foreach (var area in floor.Areas.OrderBy(a => a.SortOrder))
        {
            ordered.AddRange(OrderZones(floor.Zones.Where(z => z.AreaId == area.Id)));
        }
        ordered.AddRange(OrderZones(floor.Zones.Where(z => z.AreaId == null)));

        var idx = ordered.FindIndex(z => z.Id == zoneId.Value);
        if (idx < 0) return 'A';
        return (char)('A' + idx);
    }

    public async Task CancelShareAsync(long id, long userId, CancellationToken ct = default)
    {
        var share = await _db.SeatShares.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw AppException.NotFound("分享不存在");

        if (share.OwnerUserId != userId)
            throw AppException.Forbidden("只能取消自己的分享");

        if (share.Status == SeatShareStatus.Cancelled || share.Status == SeatShareStatus.Completed || share.Status == SeatShareStatus.Expired)
            throw AppException.BadRequest("share_not_active", "该分享已结束，无法取消");

        var reservations = await _db.Reservations
            .Where(r => r.ShareId == share.Id && r.Status == ReservationStatus.Reserved)
            .Include(r => r.User)
            .ToListAsync(ct);

        // 通知已预约用户
        foreach (var r in reservations)
        {
            r.Status = ReservationStatus.Cancelled;
            r.CancelledAt = DateTime.UtcNow;
            await _notifications.SendAsync(r.UserId, NotificationType.ReservationCancelled,
                "分享被取消", $"你预约的座位「{share.Seat?.Code}」分享被取消", null, ct);
        }

        share.Status = SeatShareStatus.Cancelled;
        share.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // 友邻贡献：取消分享撤销该次贡献（次数/时长），保持贡献反映有效分享
        var cancelledHours = Math.Round((share.EndAt - share.StartAt).TotalHours, 1);
        await _credit.TrackContributionAsync(userId, "share_cancelled", cancelledHours, ct);

        await InvalidateSeatCacheAsync(share.SeatId, ct);
    }

    private async Task InvalidateSeatCacheAsync(long seatId, CancellationToken ct)
    {
        var venueIds = await _db.Zones
            .Where(z => z.Seats.Any(s => s.Id == seatId))
            .Select(z => z.Floor!.VenueId)
            .Distinct()
            .ToListAsync(ct);
        foreach (var venueId in venueIds)
        {
            await _cache.RemoveAsync($"venue:{venueId}", ct);
        }
    }

    private async Task<SeatShareDto?> GetShareDtoAsync(long id, CancellationToken ct)
    {
        var dto = await _db.SeatShares
            .Where(s => s.Id == id)
            .Include(s => s.Seat!)
                .ThenInclude(s => s.Zone!)
                    .ThenInclude(z => z.Floor!)
                        .ThenInclude(f => f.Venue)
            .Include(s => s.Seat!)
                .ThenInclude(s => s.Zone!)
                    .ThenInclude(z => z.Area)
            .Select(s => new SeatShareDto
            {
                Id = s.Id,
                SeatId = s.SeatId,
                SeatCode = s.Seat!.Code,
                VenueName = s.Seat.Zone!.Floor!.Venue!.Name,
                FloorName = s.Seat.Zone!.Floor!.Name,
                AreaName = s.Seat.Zone!.Area != null ? s.Seat.Zone.Area.Name : null,
                OwnerUserId = s.OwnerUserId,
                OwnerNickname = s.OwnerUser!.Nickname,
                StartAt = s.StartAt,
                EndAt = s.EndAt,
                Status = s.Status.ToString(),
                Note = s.Note,
                AllowContact = s.AllowContact,
                CreatedAt = s.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (dto is not null)
        {
            var letter = await GetZoneLetterAsync(dto.SeatId, ct);
            var seatNo = dto.SeatCode.Split('-').LastOrDefault() ?? dto.SeatCode;
            dto.DisplayCode = $"{letter}区-{seatNo}";
        }
        return dto;
    }

    // 生成 6 位到座核销码（数字，供预约者输入核销）
    private static string GenerateCheckInCode()
        => Random.Shared.Next(100000, 1000000).ToString();

    // 备注中检测联系方式：手机号、微信号、QQ、网址等，防止备注演变为社交/交易入口
    private static bool ContainsContactInfo(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        // 手机号（11位数字）、微信号/QQ（字母数字混排的常见联系方式）、邮箱、网址、vx/q/wx 等缩写
        return System.Text.RegularExpressions.Regex.IsMatch(text,
            @"1[3-9]\d{9}|[a-zA-Z0-9_]{4,20}@[\w.-]+|(?:https?://|www\.)[\w.-]+|(?:vx|wx|qq|微信|加我|联系我|wechat|weixin)\s*[:：]?[\s0-9a-zA-Z_-]{1,20}",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
