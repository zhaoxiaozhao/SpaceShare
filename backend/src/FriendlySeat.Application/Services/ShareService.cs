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
    private readonly ILogger _logger;

    public ShareService(IAppDbContext db, ConfigService config, INotificationService notifications, IRedisCache cache, ILogger<ShareService> logger)
    {
        _db = db;
        _config = config;
        _notifications = notifications;
        _cache = cache;
        _logger = logger;
    }

    public async Task<SeatShareDto> CreateShareAsync(long userId, ShareCreateRequest request, CancellationToken ct = default)
    {
        var rules = await _config.GetReservationRulesAsync(ct);
        var now = DateTime.UtcNow;

        if (request.EndAt <= request.StartAt)
            throw AppException.BadRequest("share_time_invalid", "共享结束时间必须晚于开始时间");
        if ((request.EndAt - request.StartAt).TotalMinutes < rules.MinMinutes)
            throw AppException.BadRequest("share_too_short", $"共享时长不能少于{rules.MinMinutes}分钟");
        // 分享起点允许为“现在”或未来（前端默认从现在开始；也支持分享未来时段）

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
            CreatedAt = now
        };

        _db.SeatShares.Add(share);
        await _db.SaveChangesAsync(ct);

        await InvalidateSeatCacheAsync(request.SeatId, ct);

        return await GetShareDtoAsync(share.Id, ct) ?? throw AppException.NotFound();
    }

    public async Task<List<SeatShareDto>> GetSharesBySeatIdsAsync(List<long> seatIds, CancellationToken ct = default)
    {
        if (seatIds.Count == 0) return new List<SeatShareDto>();

        var now = DateTime.UtcNow;
        var shares = await _db.SeatShares
            .Where(s => seatIds.Contains(s.SeatId) && s.Status == SeatShareStatus.Available && s.EndAt > now)
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
                OwnerUserId = s.OwnerUserId,
                OwnerNickname = s.OwnerUser!.Nickname,
                StartAt = s.StartAt,
                EndAt = s.EndAt,
                Status = s.Status.ToString(),
                Note = s.Note,
                AllowContact = s.AllowContact,
                CreatedAt = s.CreatedAt
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
        var waitlistCount = await _db.ReservationWaitlists.CountAsync(
            w => w.ShareId == share.Id && w.Status == WaitlistStatus.Waiting, ct);

        var isReservable = share.Status == SeatShareStatus.Available && share.StartAt > now;

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
}
