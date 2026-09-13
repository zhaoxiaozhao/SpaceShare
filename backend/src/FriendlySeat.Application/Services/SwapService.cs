using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

/// <summary>
/// 换座意向撮合：用户在座位详情页标记自己的座位并发布"想换到哪"，他人自愿响应并提交自己的座位，
/// 发布者确认后线下物理交换。免费、无聊天、无联系方式，平台仅提供信息撮合，以场馆规定为准。
/// </summary>
public class SwapService
{
    private static readonly string[] ValidReasons =
        { "light", "cold", "hot", "noise", "together", "window", "socket", "other" };
    private static readonly int[] AllowedDurations = { 30, 60, 120 };

    private readonly IAppDbContext _db;
    private readonly INotificationService _notifications;

    public SwapService(IAppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<SeatSwapDto> CreateAsync(long userId, SeatSwapCreateRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var seat = await _db.Seats.Include(s => s.Zone!).ThenInclude(z => z.Floor!).ThenInclude(f => f.Venue)
            .FirstOrDefaultAsync(s => s.Id == request.SeatId, ct)
            ?? throw AppException.NotFound("座位不存在");
        var venueId = seat.Zone!.Floor!.VenueId;

        await ValidateWantLocationAsync(venueId, request.WantFloorId, request.WantAreaId, request.WantZoneId, ct);

        var reasons = (request.Reasons ?? new List<string>())
            .Where(r => ValidReasons.Contains(r))
            .Distinct()
            .ToList();
        if (reasons.Count == 0)
            throw AppException.BadRequest("reason_required", "请选择至少一个换座原因");

        var duration = AllowedDurations.Contains(request.DurationMinutes) ? request.DurationMinutes : 60;

        // 同一用户同时只允许一条进行中的换座意向
        var hasOpen = await _db.SeatSwapRequests
            .AnyAsync(r => r.UserId == userId && r.Status == SeatSwapStatus.Open && r.ExpireAt > now, ct);
        if (hasOpen)
            throw AppException.Conflict("swap_exists", "你已有一条进行中的换座意向，请先取消或等待结束");

        var entity = new SeatSwapRequest
        {
            UserId = userId,
            VenueId = venueId,
            SeatId = seat.Id,
            WantFloorId = request.WantFloorId,
            WantAreaId = request.WantAreaId,
            WantZoneId = request.WantZoneId,
            Reasons = string.Join(',', reasons),
            Status = SeatSwapStatus.Open,
            CreatedAt = now,
            ExpireAt = now.AddMinutes(duration)
        };
        _db.SeatSwapRequests.Add(entity);
        await _db.SaveChangesAsync(ct);

        return await GetDtoAsync(entity.Id, userId, includeResponses: true, ct) ?? throw AppException.NotFound();
    }

    public async Task<List<SeatSwapDto>> GetOpenAsync(long venueId, long viewerUserId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var list = await _db.SeatSwapRequests
            .Where(r => r.VenueId == venueId && r.Status == SeatSwapStatus.Open && r.ExpireAt > now)
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return await BuildViewerAwareAsync(list, viewerUserId, ct);
    }

    /// <summary>最近换座意向（跨场馆，供首页展示）</summary>
    public async Task<List<SeatSwapDto>> GetRecentAsync(long viewerUserId, int take, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var list = await _db.SeatSwapRequests
            .Where(r => r.Status == SeatSwapStatus.Open && r.ExpireAt > now)
            .OrderByDescending(r => r.CreatedAt)
            .Take(Math.Clamp(take, 1, 50))
            .ToListAsync(ct);

        return await BuildViewerAwareAsync(list, viewerUserId, ct);
    }

    public async Task<List<SeatSwapDto>> GetMineAsync(long userId, CancellationToken ct = default)
    {
        var list = await _db.SeatSwapRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .ToListAsync(ct);
        return await BuildDtosAsync(list, userId, includeResponses: true, ct);
    }

    public async Task<List<SeatSwapDto>> GetRespondedAsync(long userId, CancellationToken ct = default)
    {
        var myResponses = await _db.SeatSwapResponses
            .Where(x => x.UserId == userId)
            .Select(x => new { x.RequestId, x.Status })
            .ToListAsync(ct);
        var requestIdList = myResponses.Select(x => x.RequestId).Distinct().ToList();
        var myResponseMap = myResponses
            .GroupBy(x => x.RequestId)
            .ToDictionary(g => g.Key, g => g.First().Status);

        var list = await _db.SeatSwapRequests
            .Where(r => requestIdList.Contains(r.Id))
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        var dtos = await BuildDtosAsync(list, userId, includeResponses: false, ct);
        foreach (var d in dtos)
        {
            d.RespondedByMe = true;
            d.MyResponseStatus = myResponseMap.TryGetValue(d.Id, out var st) ? st.ToString() : null;
        }
        return dtos;
    }

    public async Task<SeatSwapDto> RespondAsync(long requestId, long userId, SeatSwapRespondRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var req = await _db.SeatSwapRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw AppException.NotFound("换座意向不存在");

        if (req.UserId == userId)
            throw AppException.BadRequest("cannot_respond_own", "不能响应自己的换座意向");
        if (req.Status != SeatSwapStatus.Open || req.ExpireAt <= now)
            throw AppException.BadRequest("swap_closed", "该换座意向已结束");

        var seat = await _db.Seats.Include(s => s.Zone!).ThenInclude(z => z.Floor)
            .FirstOrDefaultAsync(s => s.Id == request.SeatId, ct)
            ?? throw AppException.NotFound("座位不存在");
        if (seat.Zone!.Floor!.VenueId != req.VenueId)
            throw AppException.BadRequest("venue_mismatch", "只能和同场馆的座位交换");

        var existing = await _db.SeatSwapResponses
            .FirstOrDefaultAsync(x => x.RequestId == requestId && x.UserId == userId, ct);
        if (existing is null)
        {
            _db.SeatSwapResponses.Add(new SeatSwapResponse
            {
                RequestId = requestId,
                UserId = userId,
                SeatId = seat.Id,
                Status = SeatSwapResponseStatus.Pending,
                CreatedAt = now
            });
        }
        else
        {
            existing.SeatId = seat.Id;
            existing.Status = SeatSwapResponseStatus.Pending;
            existing.CreatedAt = now;
        }
        await _db.SaveChangesAsync(ct);

        await _notifications.SendAsync(req.UserId, NotificationType.System,
            "有人想和你换座", "有人愿意与你换座并标记了座位，去确认一下吧。", null, ct);

        return await GetDtoAsync(requestId, userId, includeResponses: true, ct) ?? throw AppException.NotFound();
    }

    public async Task<SeatSwapDto> AcceptAsync(long requestId, long userId, long responseId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var req = await _db.SeatSwapRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw AppException.NotFound("换座意向不存在");

        if (req.UserId != userId)
            throw AppException.Forbidden("只能处理自己发布的换座意向");
        if (req.Status != SeatSwapStatus.Open || req.ExpireAt <= now)
            throw AppException.BadRequest("swap_closed", "该换座意向已结束");

        var chosen = await _db.SeatSwapResponses
            .FirstOrDefaultAsync(x => x.Id == responseId && x.RequestId == requestId, ct)
            ?? throw AppException.NotFound("响应不存在");

        var others = await _db.SeatSwapResponses
            .Where(x => x.RequestId == requestId && x.Id != responseId)
            .ToListAsync(ct);

        chosen.Status = SeatSwapResponseStatus.Accepted;
        foreach (var o in others) o.Status = SeatSwapResponseStatus.Rejected;

        req.Status = SeatSwapStatus.Matched;
        req.MatchedAt = now;
        req.MatchedResponseId = responseId;
        await _db.SaveChangesAsync(ct);

        await _notifications.SendAsync(chosen.UserId, NotificationType.System,
            "对方已同意换座", "对方已同意与你交换座位，可以按双方座位进行线下物理交换了。", null, ct);

        return await GetDtoAsync(requestId, userId, includeResponses: true, ct) ?? throw AppException.NotFound();
    }

    public async Task CancelAsync(long requestId, long userId, CancellationToken ct = default)
    {
        var req = await _db.SeatSwapRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw AppException.NotFound("换座意向不存在");

        if (req.UserId != userId)
            throw AppException.Forbidden("只能取消自己发布的换座意向");
        if (req.Status != SeatSwapStatus.Open)
            throw AppException.BadRequest("swap_closed", "该换座意向已结束");

        req.Status = SeatSwapStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
    }

    // ---- 管理端 ----

    public async Task<List<SeatSwapDto>> AdminListAsync(string? status, CancellationToken ct = default)
    {
        var q = _db.SeatSwapRequests.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SeatSwapStatus>(status, true, out var st))
            q = q.Where(r => r.Status == st);

        var list = await q.OrderByDescending(r => r.CreatedAt).Take(200).ToListAsync(ct);
        return await BuildDtosAsync(list, 0, includeResponses: true, ct, responsesForAll: true);
    }

    public async Task AdminTakeDownAsync(long id, long operatorId, CancellationToken ct = default)
    {
        var req = await _db.SeatSwapRequests.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw AppException.NotFound("换座意向不存在");

        req.Status = SeatSwapStatus.Cancelled;
        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminUserId = operatorId,
            Action = "swap.take_down",
            EntityType = "SeatSwapRequest",
            EntityId = id.ToString(),
            Detail = "管理端下架换座意向",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    // ---- 内部辅助 ----

    private async Task ValidateWantLocationAsync(long venueId, long? floorId, long? areaId, long? zoneId, CancellationToken ct)
    {
        if (floorId.HasValue)
        {
            var ok = await _db.Floors.AnyAsync(f => f.Id == floorId.Value && f.VenueId == venueId, ct);
            if (!ok) throw AppException.BadRequest("floor_invalid", "楼层不存在或不属于该场馆");
        }
        if (areaId.HasValue)
        {
            var ok = await _db.Areas.AnyAsync(a => a.Id == areaId.Value && a.Floor!.VenueId == venueId, ct);
            if (!ok) throw AppException.BadRequest("area_invalid", "区域不存在或不属于该场馆");
        }
        if (zoneId.HasValue)
        {
            var ok = await _db.Zones.AnyAsync(z => z.Id == zoneId.Value && z.Floor!.VenueId == venueId, ct);
            if (!ok) throw AppException.BadRequest("zone_invalid", "区块不存在或不属于该场馆");
        }
    }

    private async Task<List<SeatSwapDto>> BuildViewerAwareAsync(List<SeatSwapRequest> list, long viewerUserId, CancellationToken ct)
    {
        var dtos = await BuildDtosAsync(list, viewerUserId, includeResponses: false, ct);
        if (list.Count == 0) return dtos;

        var idList = list.Select(r => r.Id).ToList();
        var myResponses = await _db.SeatSwapResponses
            .Where(x => x.UserId == viewerUserId && idList.Contains(x.RequestId))
            .Select(x => new { x.RequestId, x.Status })
            .ToListAsync(ct);
        var map = myResponses
            .GroupBy(x => x.RequestId)
            .ToDictionary(g => g.Key, g => g.First().Status);

        foreach (var d in dtos)
        {
            if (map.TryGetValue(d.Id, out var st))
            {
                d.RespondedByMe = true;
                d.MyResponseStatus = st.ToString();
            }
        }
        return dtos;
    }

    private async Task<SeatSwapDto?> GetDtoAsync(long id, long viewerUserId, bool includeResponses, CancellationToken ct)
    {
        var entity = await _db.SeatSwapRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return null;
        var dtos = await BuildDtosAsync(new List<SeatSwapRequest> { entity }, viewerUserId, includeResponses, ct);
        return dtos.FirstOrDefault();
    }

    private async Task<List<SeatSwapDto>> BuildDtosAsync(
        List<SeatSwapRequest> list, long viewerUserId, bool includeResponses, CancellationToken ct, bool responsesForAll = false)
    {
        if (list.Count == 0) return new List<SeatSwapDto>();

        var wantFloorIds = list.Select(r => r.WantFloorId).Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        var wantAreaIds = list.Select(r => r.WantAreaId).Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        var wantZoneIds = list.Select(r => r.WantZoneId).Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        var venueIds = list.Select(r => r.VenueId).Distinct().ToList();
        var userIds = list.Select(r => r.UserId).Distinct().ToList();

        var wantFloorNames = await _db.Floors.Where(f => wantFloorIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.Name, ct);
        var wantAreaNames = await _db.Areas.Where(a => wantAreaIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Name, ct);
        var wantZoneNames = await _db.Zones.Where(z => wantZoneIds.Contains(z.Id)).ToDictionaryAsync(z => z.Id, z => z.Name, ct);
        var venueNames = await _db.Venues.Where(v => venueIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v.Name, ct);
        var nicknames = await _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nickname ?? "", ct);

        // 座位信息（发布者座位 + 响应座位）
        var responseEntities = new List<SeatSwapResponse>();
        if (includeResponses)
        {
            var ids = list.Select(r => r.Id).ToList();
            responseEntities = await _db.SeatSwapResponses
                .Where(x => ids.Contains(x.RequestId))
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(ct);
        }

        var seatIds = list.Select(r => r.SeatId).Concat(responseEntities.Select(x => x.SeatId)).Distinct().ToList();
        var seatInfos = await LoadSeatInfosAsync(seatIds, ct);
        var responseUserIds = responseEntities.Select(x => x.UserId).Distinct().ToList();
        var responseNicknames = await _db.Users.Where(u => responseUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nickname ?? "", ct);

        string? WantName(Dictionary<long, string> map, long? id) => id.HasValue && map.TryGetValue(id.Value, out var v) ? v : null;
        SeatInfo? Seat(long id) => seatInfos.TryGetValue(id, out var v) ? v : null;

        var result = new List<SeatSwapDto>();
        foreach (var r in list)
        {
            var seat = Seat(r.SeatId);
            var dto = new SeatSwapDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserNickname = nicknames.TryGetValue(r.UserId, out var nn) ? nn : "",
                VenueId = r.VenueId,
                VenueName = venueNames.TryGetValue(r.VenueId, out var vn) ? vn : "",
                SeatId = r.SeatId,
                SeatCode = seat?.Code ?? "",
                FloorName = seat?.FloorName,
                AreaName = seat?.AreaName,
                ZoneName = seat?.ZoneName,
                WantFloorId = r.WantFloorId,
                WantFloorName = WantName(wantFloorNames, r.WantFloorId),
                WantAreaId = r.WantAreaId,
                WantAreaName = WantName(wantAreaNames, r.WantAreaId),
                WantZoneId = r.WantZoneId,
                WantZoneName = WantName(wantZoneNames, r.WantZoneId),
                Reasons = string.IsNullOrEmpty(r.Reasons)
                    ? new List<string>()
                    : r.Reasons.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt,
                ExpireAt = r.ExpireAt,
                IsMine = r.UserId == viewerUserId,
                MatchedResponseId = r.MatchedResponseId
            };

            if (includeResponses && (responsesForAll || r.UserId == viewerUserId))
            {
                dto.Responses = responseEntities
                    .Where(x => x.RequestId == r.Id)
                    .Select(x =>
                    {
                        var rs = Seat(x.SeatId);
                        return new SeatSwapResponseDto
                        {
                            Id = x.Id,
                            UserId = x.UserId,
                            UserNickname = responseNicknames.TryGetValue(x.UserId, out var rn) ? rn : "",
                            SeatId = x.SeatId,
                            SeatCode = rs?.Code ?? "",
                            FloorName = rs?.FloorName,
                            AreaName = rs?.AreaName,
                            ZoneName = rs?.ZoneName,
                            Status = x.Status.ToString(),
                            CreatedAt = x.CreatedAt,
                            IsMine = x.UserId == viewerUserId
                        };
                    })
                    .ToList();
            }

            result.Add(dto);
        }
        return result;
    }

    private async Task<Dictionary<long, SeatInfo>> LoadSeatInfosAsync(List<long> seatIds, CancellationToken ct)
    {
        var result = new Dictionary<long, SeatInfo>();
        if (seatIds.Count == 0) return result;

        var seats = await _db.Seats.Where(s => seatIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Code, s.ZoneId })
            .ToListAsync(ct);
        var zoneIds = seats.Select(s => s.ZoneId).Distinct().ToList();
        var zones = await _db.Zones.Where(z => zoneIds.Contains(z.Id))
            .Select(z => new { z.Id, z.Name, z.FloorId, z.AreaId })
            .ToListAsync(ct);
        var zoneMap = zones.ToDictionary(z => z.Id);
        var floorIds = zones.Select(z => z.FloorId).Distinct().ToList();
        var areaIds = zones.Where(z => z.AreaId.HasValue).Select(z => z.AreaId!.Value).Distinct().ToList();
        var floorNames = await _db.Floors.Where(f => floorIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.Name, ct);
        var areaNames = await _db.Areas.Where(a => areaIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Name, ct);

        foreach (var s in seats)
        {
            var info = new SeatInfo { Code = s.Code };
            if (zoneMap.TryGetValue(s.ZoneId, out var z))
            {
                info.ZoneName = z.Name;
                if (floorNames.TryGetValue(z.FloorId, out var fn)) info.FloorName = fn;
                if (z.AreaId.HasValue && areaNames.TryGetValue(z.AreaId.Value, out var an)) info.AreaName = an;
            }
            result[s.Id] = info;
        }
        return result;
    }

    private class SeatInfo
    {
        public string Code { get; set; } = string.Empty;
        public string? FloorName { get; set; }
        public string? AreaName { get; set; }
        public string? ZoneName { get; set; }
    }
}
