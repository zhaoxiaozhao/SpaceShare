using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class AdminVenueCreateRequest
{
    public long CityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Library";
    public string Address { get; set; } = string.Empty;
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public string? Description { get; set; }
    public string OpeningTime { get; set; } = "09:00";
    public string ClosingTime { get; set; } = "22:00";
}

public class AdminCityCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
}

public class AdminFloorRequest
{
    public long VenueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class AdminVenueUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Library";
    public string Address { get; set; } = string.Empty;
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public string? Description { get; set; }
    public string OpeningTime { get; set; } = "09:00";
    public string ClosingTime { get; set; } = "22:00";
}

public class AdminCityUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
}

public class AdminZoneRequest
{
    public long FloorId { get; set; }
    public long? AreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int GridRows { get; set; }
    public int GridCols { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public string LayoutMode { get; set; } = "grid";
    public int TableSeatCols { get; set; } = 2;
    public int TableSeatRows { get; set; } = 2;
    public double TableGapX { get; set; } = 1;
    public double TableGapY { get; set; } = 1;
    public int TablesX { get; set; } = 1;
    public int TablesY { get; set; } = 1;
    public double ArcRadius { get; set; } = 8;
    public double ArcRadiusStep { get; set; } = 1.5;
    public double ArcStartAngle { get; set; } = 180;
    public double ArcEndAngle { get; set; } = 360;
    public int ArcRows { get; set; } = 3;
    public int ArcSeatsPerRow { get; set; } = 8;
    public double ArcAxisB { get; set; } = 8;
    public double CurveAmplitude { get; set; } = 2;
    public double CurveWavelength { get; set; } = 6;
    public double CurvePhase { get; set; } = 0;
    public double CurveRowGap { get; set; } = 2;
    public double CurveAngle { get; set; } = 30;
    public double CurveSlantGap { get; set; } = 2;
    public string? PathPoints { get; set; }
}

public class AdminAreaRequest
{
    public long FloorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class AdminPoiRequest
{
    public long FloorId { get; set; }
    public string Type { get; set; } = "Other";
    public string? Name { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;
    public string? Direction { get; set; }
    public int Rotation { get; set; }
    public string? Text { get; set; }
}

public class AdminSeatRequest
{
    public long ZoneId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = "Normal";
    public bool Window { get; set; }
    public bool PowerSocket { get; set; }
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
}

public class AdminVenueManagementService
{
    private readonly IAppDbContext _db;
    private readonly IAuditService _audit;
    private readonly IRedisCache _cache;

    public AdminVenueManagementService(IAppDbContext db, IAuditService audit, IRedisCache cache)
    {
        _db = db;
        _audit = audit;
        _cache = cache;
    }

    // 管理端修改场馆结构（区域/区块/座位/标志物/楼层）后，清除用户端场馆详情缓存
    private Task InvalidateVenueAsync(long venueId, CancellationToken ct = default)
        => _cache.RemoveAsync(CacheKeys.Venue(venueId), ct);

    private async Task InvalidateVenueByFloorAsync(long floorId, CancellationToken ct = default)
    {
        var venueId = await _db.Floors.Where(f => f.Id == floorId).Select(f => f.VenueId).FirstOrDefaultAsync(ct);
        if (venueId > 0) await InvalidateVenueAsync(venueId, ct);
    }

    private async Task InvalidateVenueByZoneAsync(long zoneId, CancellationToken ct = default)
    {
        var floorId = await _db.Zones.Where(z => z.Id == zoneId).Select(z => z.FloorId).FirstOrDefaultAsync(ct);
        if (floorId > 0) await InvalidateVenueByFloorAsync(floorId, ct);
    }

    public async Task<List<CityDto>> GetCitiesAsync(CancellationToken ct = default)
    {
        return await _db.Cities
            .Select(c => new CityDto
            {
                Id = c.Id,
                Name = c.Name,
                Province = c.Province,
                CountryCode = c.CountryCode,
                Longitude = c.Longitude,
                Latitude = c.Latitude
            })
            .ToListAsync(ct);
    }

    public async Task<CityDto> CreateCityAsync(AdminCityCreateRequest request, long operatorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw AppException.BadRequest("name_required", "城市名称不能为空");

        var city = new City
        {
            Name = request.Name.Trim(),
            Province = request.Province ?? string.Empty,
            Longitude = request.Longitude,
            Latitude = request.Latitude,
            Status = EntityStatus.Active
        };
        _db.Cities.Add(city);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(operatorId, "city.create", "City", city.Id.ToString(), $"创建城市 {city.Name}", null, ct);

        return new CityDto { Id = city.Id, Name = city.Name, Province = city.Province, CountryCode = city.CountryCode, Longitude = city.Longitude, Latitude = city.Latitude };
    }

    public async Task<List<VenueListItemDto>> GetVenuesAsync(CancellationToken ct = default)
    {
        return await _db.Venues
            .Select(v => new VenueListItemDto
            {
                Id = v.Id,
                Name = v.Name,
                Type = v.Type.ToString(),
                Address = v.Address,
                Longitude = v.Longitude,
                Latitude = v.Latitude,
                OpeningTime = v.OpeningTime.ToString(@"hh\:mm"),
                ClosingTime = v.ClosingTime.ToString(@"hh\:mm"),
                Status = v.Status.ToString()
            })
            .ToListAsync(ct);
    }

    public async Task<VenueDto> CreateVenueAsync(AdminVenueCreateRequest request, long operatorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw AppException.BadRequest("name_required", "场馆名称不能为空");
        if (!Enum.TryParse<VenueType>(request.Type, true, out var type))
            type = VenueType.Library;

        var venue = new Venue
        {
            CityId = request.CityId,
            Name = request.Name.Trim(),
            Type = type,
            Address = request.Address ?? string.Empty,
            Longitude = request.Longitude,
            Latitude = request.Latitude,
            Description = request.Description,
            OpeningTime = TimeSpan.TryParse(request.OpeningTime, out var ot) ? ot : TimeSpan.FromHours(9),
            ClosingTime = TimeSpan.TryParse(request.ClosingTime, out var ct2) ? ct2 : TimeSpan.FromHours(22),
            Status = EntityStatus.Active
        };
        _db.Venues.Add(venue);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(operatorId, "venue.create", "Venue", venue.Id.ToString(), $"创建场馆 {venue.Name}", null, ct);

        return ToDto(venue);
    }

    public async Task<VenueDto> UpdateVenueAsync(long venueId, AdminVenueUpdateRequest request, long operatorId, CancellationToken ct = default)
    {
        var venue = await _db.Venues.FirstOrDefaultAsync(v => v.Id == venueId, ct)
            ?? throw AppException.NotFound("场馆不存在");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw AppException.BadRequest("name_required", "场馆名称不能为空");

        venue.Name = request.Name.Trim();
        if (Enum.TryParse<VenueType>(request.Type, true, out var type)) venue.Type = type;
        venue.Address = request.Address ?? string.Empty;
        venue.Longitude = request.Longitude;
        venue.Latitude = request.Latitude;
        venue.Description = request.Description;
        venue.OpeningTime = TimeSpan.TryParse(request.OpeningTime, out var ot) ? ot : venue.OpeningTime;
        venue.ClosingTime = TimeSpan.TryParse(request.ClosingTime, out var ct2) ? ct2 : venue.ClosingTime;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "venue.update", "Venue", venue.Id.ToString(), $"更新场馆 {venue.Name}", null, ct);
        await _cache.RemoveAsync($"venue:{venueId}", ct);

        return ToDto(venue);
    }

    /// <summary>场馆显示/隐藏（Hidden = 下架，用户端不可见）</summary>
    public async Task SetVenueStatusAsync(long venueId, bool visible, long operatorId, CancellationToken ct = default)
    {
        var venue = await _db.Venues.FirstOrDefaultAsync(v => v.Id == venueId, ct)
            ?? throw AppException.NotFound("场馆不存在");

        venue.Status = visible ? EntityStatus.Active : EntityStatus.Disabled;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "venue.status", "Venue", venueId.ToString(), $"设置场馆 {venue.Name} 为 {(visible ? "显示" : "隐藏")}", null, ct);
        await _cache.RemoveAsync($"venue:{venueId}", ct);
    }

    /// <summary>删除场馆（级联楼层/区域/区块/座位/预约/分享等，存在进行中预约时拒绝）</summary>
    public async Task DeleteVenueAsync(long venueId, long operatorId, CancellationToken ct = default)
    {
        var venue = await _db.Venues.FirstOrDefaultAsync(v => v.Id == venueId, ct)
            ?? throw AppException.NotFound("场馆不存在");

        var now = DateTime.UtcNow;
        var hasActiveReservation = await _db.Reservations
            .AnyAsync(r => r.EndAt > now && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Arrived || r.Status == ReservationStatus.Using)
                && _db.Zones.Any(z => z.Floor!.VenueId == venueId && z.Seats.Any(s => s.Id == r.SeatId)), ct);
        if (hasActiveReservation)
            throw AppException.Conflict("venue_has_active_reservation", "该场馆存在进行中的预约，无法删除");

        // 收集场馆下所有楼层/座位
        var floorIds = await _db.Floors.Where(f => f.VenueId == venueId).Select(f => f.Id).ToListAsync(ct);
        var seatIds = await _db.Seats.Where(s => floorIds.Contains(s.Zone!.FloorId)).Select(s => s.Id).ToListAsync(ct);

        // 级联清理（按外键依赖顺序）
        if (seatIds.Any())
        {
            await DeleteRelatedAsync(seatIds, ct);
            _db.Seats.RemoveRange(_db.Seats.Where(s => floorIds.Contains(s.Zone!.FloorId)));
        }
        _db.Zones.RemoveRange(_db.Zones.Where(z => floorIds.Contains(z.FloorId)));
        _db.Areas.RemoveRange(_db.Areas.Where(a => floorIds.Contains(a.FloorId)));
        _db.FloorPois.RemoveRange(_db.FloorPois.Where(p => floorIds.Contains(p.FloorId)));
        _db.Floors.RemoveRange(_db.Floors.Where(f => f.VenueId == venueId));
        _db.Venues.Remove(venue);

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "venue.delete", "Venue", venueId.ToString(), $"删除场馆 {venue.Name}", null, ct);
        await _cache.RemoveAsync($"venue:{venueId}", ct);
    }

    /// <summary>删除座位关联的预约/分享/会话/等候（外键依赖清理）</summary>
    private async Task DeleteRelatedAsync(List<long> seatIds, CancellationToken ct)
    {
        _db.Reservations.RemoveRange(_db.Reservations.Where(r => seatIds.Contains(r.SeatId)));
        _db.SeatShares.RemoveRange(_db.SeatShares.Where(s => seatIds.Contains(s.SeatId)));
        _db.SeatSessions.RemoveRange(_db.SeatSessions.Where(s => seatIds.Contains(s.SeatId)));
        _db.ReservationWaitlists.RemoveRange(_db.ReservationWaitlists.Where(w => w.Share != null && seatIds.Contains(w.Share.SeatId)));
    }

    public async Task UpdateFloorAsync(long floorId, AdminFloorRequest request, long operatorId, CancellationToken ct = default)
    {
        var floor = await _db.Floors.FirstOrDefaultAsync(f => f.Id == floorId, ct)
            ?? throw AppException.NotFound("楼层不存在");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw AppException.BadRequest("name_required", "楼层名称不能为空");

        floor.Name = request.Name.Trim();
        floor.SortOrder = request.SortOrder;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "floor.update", "Floor", floorId.ToString(), $"更新楼层 {floor.Name}", null, ct);

        var venueId = await _db.Floors.Where(f => f.Id == floorId).Select(f => f.VenueId).FirstOrDefaultAsync(ct);
        if (venueId > 0) await _cache.RemoveAsync($"venue:{venueId}", ct);
    }

    /// <summary>删除楼层（级联区域/区块/座位；存在座位时先确认）</summary>
    public async Task DeleteFloorAsync(long floorId, long operatorId, CancellationToken ct = default)
    {
        var floor = await _db.Floors
            .Include(f => f.Zones)
            .ThenInclude(z => z.Seats)
            .FirstOrDefaultAsync(f => f.Id == floorId, ct)
            ?? throw AppException.NotFound("楼层不存在");

        var hasSeats = floor.Zones.Any(z => z.Seats.Count > 0);
        if (hasSeats)
            throw AppException.Conflict("floor_has_seats", "该楼层下存在座位，请先删除座位或确认后再试");

        var venueId = floor.VenueId;
        _db.Areas.RemoveRange(_db.Areas.Where(a => a.FloorId == floorId));
        _db.FloorPois.RemoveRange(_db.FloorPois.Where(p => p.FloorId == floorId));
        _db.Zones.RemoveRange(floor.Zones);
        _db.Floors.Remove(floor);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "floor.delete", "Floor", floorId.ToString(), $"删除楼层 {floor.Name}", null, ct);
        await _cache.RemoveAsync($"venue:{venueId}", ct);
    }

    public async Task<CityDto> UpdateCityAsync(long cityId, AdminCityUpdateRequest request, long operatorId, CancellationToken ct = default)
    {
        var city = await _db.Cities.FirstOrDefaultAsync(c => c.Id == cityId, ct)
            ?? throw AppException.NotFound("城市不存在");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw AppException.BadRequest("name_required", "城市名称不能为空");

        city.Name = request.Name.Trim();
        city.Province = request.Province ?? string.Empty;
        city.Longitude = request.Longitude;
        city.Latitude = request.Latitude;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "city.update", "City", cityId.ToString(), $"更新城市 {city.Name}", null, ct);

        return new CityDto { Id = city.Id, Name = city.Name, Province = city.Province, CountryCode = city.CountryCode, Longitude = city.Longitude, Latitude = city.Latitude };
    }

    public async Task AddFloorAsync(AdminFloorRequest request, long operatorId, CancellationToken ct = default)
    {
        var venue = await _db.Venues.AnyAsync(v => v.Id == request.VenueId, ct);
        if (!venue) throw AppException.NotFound("场馆不存在");

        var floor = new Floor { VenueId = request.VenueId, Name = request.Name, SortOrder = request.SortOrder };
        _db.Floors.Add(floor);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "floor.create", "Floor", floor.Id.ToString(), $"创建楼层 {floor.Name}", null, ct);
        await InvalidateVenueAsync(request.VenueId, ct);
    }

    public async Task<long> AddAreaAsync(AdminAreaRequest request, long operatorId, CancellationToken ct = default)
    {
        var floor = await _db.Floors.AnyAsync(f => f.Id == request.FloorId, ct);
        if (!floor) throw AppException.NotFound("楼层不存在");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw AppException.BadRequest("area_name_required", "区域名称不能为空");

        var area = new Area
        {
            FloorId = request.FloorId,
            Name = request.Name.Trim(),
            SortOrder = request.SortOrder
        };
        _db.Areas.Add(area);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "area.create", "Area", area.Id.ToString(), $"创建空间区域 {area.Name}", null, ct);
        await InvalidateVenueByFloorAsync(request.FloorId, ct);
        return area.Id;
    }

    public async Task UpdateAreaAsync(long areaId, AdminAreaRequest request, long operatorId, CancellationToken ct = default)
    {
        var area = await _db.Areas.FirstOrDefaultAsync(a => a.Id == areaId, ct)
            ?? throw AppException.NotFound("空间区域不存在");
        if (!string.IsNullOrWhiteSpace(request.Name)) area.Name = request.Name.Trim();
        area.SortOrder = request.SortOrder;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "area.update", "Area", areaId.ToString(), $"更新空间区域 {area.Name}", null, ct);
        await InvalidateVenueByFloorAsync(area.FloorId, ct);
    }

    public async Task DeleteAreaAsync(long areaId, long operatorId, CancellationToken ct = default)
    {
        var area = await _db.Areas.FirstOrDefaultAsync(a => a.Id == areaId, ct)
            ?? throw AppException.NotFound("空间区域不存在");

        // 解除其下区块与区域的关联（区块保留，回到无区域分组）
        var zones = await _db.Zones.Where(z => z.AreaId == areaId).ToListAsync(ct);
        foreach (var z in zones) z.AreaId = null;

        _db.Areas.Remove(area);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "area.delete", "Area", areaId.ToString(), $"删除空间区域 {area.Name}", null, ct);
        await InvalidateVenueByFloorAsync(area.FloorId, ct);
    }

    public async Task<long> AddZoneAsync(AdminZoneRequest request, long operatorId, CancellationToken ct = default)
    {
        var floor = await _db.Floors.AnyAsync(f => f.Id == request.FloorId, ct);
        if (!floor) throw AppException.NotFound("楼层不存在");

        if (request.AreaId.HasValue)
        {
            var areaOk = await _db.Areas.AnyAsync(a => a.Id == request.AreaId.Value && a.FloorId == request.FloorId, ct);
            if (!areaOk) throw AppException.BadRequest("area_invalid", "空间区域不属于该楼层");
        }

        var zone = new Zone
        {
            FloorId = request.FloorId,
            AreaId = request.AreaId,
            Name = request.Name,
            SortOrder = request.SortOrder,
            GridRows = request.GridRows,
            GridCols = request.GridCols,
            OffsetX = request.OffsetX,
            OffsetY = request.OffsetY,
            LayoutMode = NormalizeLayoutMode(request.LayoutMode),
            TableSeatCols = Math.Max(1, request.TableSeatCols),
            TableSeatRows = Math.Max(1, request.TableSeatRows),
            TableGapX = Math.Max(0, request.TableGapX),
            TableGapY = Math.Max(0, request.TableGapY),
            TablesX = Math.Max(1, request.TablesX),
            TablesY = Math.Max(1, request.TablesY),
            ArcRadius = Math.Max(2, request.ArcRadius),
            ArcRadiusStep = Math.Max(0.5, request.ArcRadiusStep),
            ArcStartAngle = request.ArcStartAngle,
            ArcEndAngle = request.ArcEndAngle,
            ArcRows = Math.Max(1, request.ArcRows),
            ArcSeatsPerRow = Math.Max(1, request.ArcSeatsPerRow),
            ArcAxisB = Math.Max(2, request.ArcAxisB),
            CurveAmplitude = Math.Max(0.5, request.CurveAmplitude),
            CurveWavelength = Math.Max(2, request.CurveWavelength),
            CurvePhase = request.CurvePhase,
            CurveRowGap = Math.Max(0.5, request.CurveRowGap),
            CurveAngle = Math.Clamp(request.CurveAngle, -90, 90),
            CurveSlantGap = Math.Max(0.5, request.CurveSlantGap),
            PathPoints = string.IsNullOrWhiteSpace(request.PathPoints) ? null : request.PathPoints
        };
        _db.Zones.Add(zone);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "zone.create", "Zone", zone.Id.ToString(), $"创建座位区块 {zone.Name}", null, ct);
        await InvalidateVenueByFloorAsync(request.FloorId, ct);
        return zone.Id;
    }

    public async Task<long> AddSeatAsync(AdminSeatRequest request, long operatorId, CancellationToken ct = default)
    {
        var zone = await _db.Zones.AnyAsync(z => z.Id == request.ZoneId, ct);
        if (!zone) throw AppException.NotFound("区域不存在");

        if (!Enum.TryParse<SeatType>(request.Type, true, out var type))
            type = SeatType.Normal;

        // 设计阶段：编号允许重复，仅保证非空（空则自动生成）
        var code = request.Code?.Trim();
        if (string.IsNullOrEmpty(code))
        {
            code = await NextSeatCodeAsync(request.ZoneId, ct);
        }

        var seat = new Seat
        {
            ZoneId = request.ZoneId,
            Code = code,
            Type = type,
            Window = request.Window,
            PowerSocket = request.PowerSocket,
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            Status = SeatStatus.Available,
            Verified = true
        };
        _db.Seats.Add(seat);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "seat.create", "Seat", seat.Id.ToString(), $"创建座位 {seat.Code}", null, ct);
        await InvalidateVenueByZoneAsync(request.ZoneId, ct);
        return seat.Id;
    }

    private async Task<string> NextSeatCodeAsync(long zoneId, CancellationToken ct)
    {
        var zone = await _db.Zones.FirstAsync(z => z.Id == zoneId, ct);
        var zoneName = new string((zone.Name ?? "Z").Where(c => char.IsLetterOrDigit(c) || (c >= 0x4e00 && c <= 0x9fff)).ToArray());
        var prefix = string.IsNullOrEmpty(zoneName) ? "Z" : zoneName[..Math.Min(zoneName.Length, 6)];
        var maxSeq = await _db.Seats
            .Where(s => s.ZoneId == zoneId && s.Code.StartsWith(prefix + "-"))
            .Select(s => s.Code)
            .ToListAsync(ct);
        var maxN = 0;
        foreach (var c in maxSeq)
        {
            var tail = c[(prefix.Length + 1)..];
            if (int.TryParse(tail, out var n) && n > maxN) maxN = n;
        }
        return $"{prefix}-{(maxN + 1).ToString("D3")}";
    }

    public async Task SetSeatStatusAsync(long seatId, SeatStatus status, long operatorId, CancellationToken ct = default)
    {
        var seat = await _db.Seats.FirstOrDefaultAsync(s => s.Id == seatId, ct)
            ?? throw AppException.NotFound("座位不存在");
        seat.Status = status;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "seat.status", "Seat", seatId.ToString(), $"设置座位 {seat.Code} 状态为 {status}", null, ct);
        await InvalidateVenueByZoneAsync(seat.ZoneId, ct);
    }

    public async Task UpdateZoneAsync(long zoneId, AdminZoneRequest request, long operatorId, CancellationToken ct = default)
    {
        var zone = await _db.Zones.FirstOrDefaultAsync(z => z.Id == zoneId, ct)
            ?? throw AppException.NotFound("区域不存在");

        zone.Name = request.Name;
        zone.SortOrder = request.SortOrder;
        zone.GridRows = request.GridRows;
        zone.GridCols = request.GridCols;
        zone.OffsetX = request.OffsetX;
        zone.OffsetY = request.OffsetY;
        zone.LayoutMode = NormalizeLayoutMode(request.LayoutMode);
        zone.TableSeatCols = Math.Max(1, request.TableSeatCols);
        zone.TableSeatRows = Math.Max(1, request.TableSeatRows);
        zone.TableGapX = Math.Max(0, request.TableGapX);
        zone.TableGapY = Math.Max(0, request.TableGapY);
        zone.TablesX = Math.Max(1, request.TablesX);
        zone.TablesY = Math.Max(1, request.TablesY);
        zone.ArcRadius = Math.Max(2, request.ArcRadius);
        zone.ArcRadiusStep = Math.Max(0.5, request.ArcRadiusStep);
        zone.ArcStartAngle = request.ArcStartAngle;
        zone.ArcEndAngle = request.ArcEndAngle;
        zone.ArcRows = Math.Max(1, request.ArcRows);
        zone.ArcSeatsPerRow = Math.Max(1, request.ArcSeatsPerRow);
        zone.ArcAxisB = Math.Max(2, request.ArcAxisB);
        zone.CurveAmplitude = Math.Max(0.5, request.CurveAmplitude);
        zone.CurveWavelength = Math.Max(2, request.CurveWavelength);
        zone.CurvePhase = request.CurvePhase;
        zone.CurveRowGap = Math.Max(0.5, request.CurveRowGap);
        zone.CurveAngle = Math.Clamp(request.CurveAngle, -90, 90);
        zone.CurveSlantGap = Math.Max(0.5, request.CurveSlantGap);
        zone.PathPoints = string.IsNullOrWhiteSpace(request.PathPoints) ? null : request.PathPoints;
        if (request.AreaId.HasValue)
        {
            var areaOk = await _db.Areas.AnyAsync(a => a.Id == request.AreaId.Value && a.FloorId == zone.FloorId, ct);
            if (areaOk) zone.AreaId = request.AreaId;
        }
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "zone.update", "Zone", zoneId.ToString(), $"更新座位区块 {zone.Name} 网格 {zone.GridRows}x{zone.GridCols} 偏移({zone.OffsetX},{zone.OffsetY})", null, ct);
        await InvalidateVenueByFloorAsync(zone.FloorId, ct);
    }

    private static string NormalizeLayoutMode(string? mode) => mode switch
    {
        "table" => "table",
        "arc" => "arc",
        "ellipse" => "ellipse",
        "spiral" => "spiral",
        "sine" => "sine",
        "slant" => "slant",
        "curve" => "curve",
        _ => "grid"
    };

    public async Task DeleteZoneAsync(long zoneId, long operatorId, CancellationToken ct = default)
    {
        var zone = await _db.Zones
            .Include(z => z.Seats)
            .FirstOrDefaultAsync(z => z.Id == zoneId, ct)
            ?? throw AppException.NotFound("区域不存在");

        _db.Seats.RemoveRange(zone.Seats);
        _db.Zones.Remove(zone);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "zone.delete", "Zone", zoneId.ToString(), $"删除区域 {zone.Name} 及全部座位", null, ct);
        await InvalidateVenueByFloorAsync(zone.FloorId, ct);
    }

    public async Task DeleteSeatAsync(long seatId, long operatorId, CancellationToken ct = default)
    {
        var seat = await _db.Seats.FirstOrDefaultAsync(s => s.Id == seatId, ct)
            ?? throw AppException.NotFound("座位不存在");
        _db.Seats.Remove(seat);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "seat.delete", "Seat", seatId.ToString(), $"删除座位 {seat.Code}", null, ct);
        await InvalidateVenueByZoneAsync(seat.ZoneId, ct);
    }

    public async Task<PoiDto> AddPoiAsync(AdminPoiRequest request, long operatorId, CancellationToken ct = default)
    {
        var floor = await _db.Floors.AnyAsync(f => f.Id == request.FloorId, ct);
        if (!floor) throw AppException.NotFound("楼层不存在");
        if (!Enum.TryParse<PoiType>(request.Type, true, out var type))
            throw AppException.BadRequest("poi_type_invalid", "标志物类型无效");

        var poi = new FloorPoi
        {
            FloorId = request.FloorId,
            Type = type,
            Name = string.IsNullOrWhiteSpace(request.Name) ? type.ToString() : request.Name,
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            Width = request.Width <= 0 ? 1 : request.Width,
            Height = request.Height <= 0 ? 1 : request.Height,
            Direction = request.Direction,
            Rotation = request.Rotation,
            Text = request.Text,
            CreatedAt = DateTime.UtcNow
        };
        _db.FloorPois.Add(poi);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "poi.create", "FloorPoi", poi.Id.ToString(), $"新增标志物 {poi.Name} @({poi.PositionX},{poi.PositionY})", null, ct);
        await InvalidateVenueByFloorAsync(request.FloorId, ct);

        return ToPoiDto(poi);
    }

    public async Task<PoiDto> UpdatePoiAsync(long poiId, AdminPoiRequest request, long operatorId, CancellationToken ct = default)
    {
        var poi = await _db.FloorPois.FirstOrDefaultAsync(p => p.Id == poiId, ct)
            ?? throw AppException.NotFound("标志物不存在");
        if (Enum.TryParse<PoiType>(request.Type, true, out var type)) poi.Type = type;
        if (!string.IsNullOrWhiteSpace(request.Name)) poi.Name = request.Name;
        poi.PositionX = request.PositionX;
        poi.PositionY = request.PositionY;
        poi.Width = request.Width <= 0 ? 1 : request.Width;
        poi.Height = request.Height <= 0 ? 1 : request.Height;
        poi.Direction = request.Direction;
        poi.Rotation = request.Rotation;
        poi.Text = request.Text;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "poi.update", "FloorPoi", poiId.ToString(), $"更新标志物 {poi.Name} @({poi.PositionX},{poi.PositionY})", null, ct);
        await InvalidateVenueByFloorAsync(poi.FloorId, ct);

        return ToPoiDto(poi);
    }

    public async Task DeletePoiAsync(long poiId, long operatorId, CancellationToken ct = default)
    {
        var poi = await _db.FloorPois.FirstOrDefaultAsync(p => p.Id == poiId, ct)
            ?? throw AppException.NotFound("标志物不存在");
        _db.FloorPois.Remove(poi);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "poi.delete", "FloorPoi", poiId.ToString(), $"删除标志物 {poi.Name}", null, ct);
        await InvalidateVenueByFloorAsync(poi.FloorId, ct);
    }

    private static PoiDto ToPoiDto(FloorPoi p) => new()
    {
        Id = p.Id,
        Type = p.Type.ToString(),
        Name = p.Name,
        PositionX = p.PositionX,
        PositionY = p.PositionY,
        Width = p.Width,
        Height = p.Height,
        Direction = p.Direction,
        Rotation = p.Rotation,
        Text = p.Text
    };

    public async Task UpdateSeatAsync(long seatId, AdminSeatRequest request, long operatorId, CancellationToken ct = default)
    {
        var seat = await _db.Seats.FirstOrDefaultAsync(s => s.Id == seatId, ct)
            ?? throw AppException.NotFound("座位不存在");

        // 设计阶段：编号允许重复
        var code = string.IsNullOrWhiteSpace(request.Code) ? seat.Code : request.Code.Trim();
        seat.Code = code;
        if (Enum.TryParse<SeatType>(request.Type, true, out var type)) seat.Type = type;
        seat.Window = request.Window;
        seat.PowerSocket = request.PowerSocket;
        seat.PositionX = request.PositionX;
        seat.PositionY = request.PositionY;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "seat.update", "Seat", seatId.ToString(), $"更新座位 {seat.Code} 坐标({seat.PositionX},{seat.PositionY})", null, ct);
        await InvalidateVenueByZoneAsync(seat.ZoneId, ct);
    }

    public async Task<AdminVenueDetailDto> GetVenueDetailAsync(long venueId, CancellationToken ct = default)
    {
        var venue = await _db.Venues.FirstOrDefaultAsync(v => v.Id == venueId, ct)
            ?? throw AppException.NotFound("场馆不存在");

        var floors = await _db.Floors
            .Where(f => f.VenueId == venueId)
            .OrderBy(f => f.SortOrder)
            .Select(f => new AdminFloorDto
            {
                Id = f.Id,
                Name = f.Name,
                SortOrder = f.SortOrder,
                Areas = f.Areas.OrderBy(a => a.SortOrder).Select(a => new AreaDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    SortOrder = a.SortOrder
                }).ToList(),
                Pois = f.Pois.OrderBy(p => p.PositionY).ThenBy(p => p.PositionX).Select(p => new PoiDto
                {
                    Id = p.Id,
                    Type = p.Type.ToString(),
                    Name = p.Name,
                    PositionX = p.PositionX,
                    PositionY = p.PositionY,
                    Width = p.Width,
                    Height = p.Height,
                    Direction = p.Direction,
                    Rotation = p.Rotation,
                    Text = p.Text
                }).ToList(),
                Zones = f.Zones.OrderBy(z => z.SortOrder).Select(z => new AdminZoneDto
                {
                    Id = z.Id,
                    AreaId = z.AreaId,
                    Name = z.Name,
                    SortOrder = z.SortOrder,
                    GridRows = z.GridRows,
                    GridCols = z.GridCols,
                    OffsetX = z.OffsetX,
                    OffsetY = z.OffsetY,
                    LayoutMode = z.LayoutMode,
                    TableSeatCols = z.TableSeatCols,
                    TableSeatRows = z.TableSeatRows,
                    TableGapX = z.TableGapX,
                    TableGapY = z.TableGapY,
                    TablesX = z.TablesX,
                    TablesY = z.TablesY,
                    ArcRadius = z.ArcRadius,
                    ArcRadiusStep = z.ArcRadiusStep,
                    ArcStartAngle = z.ArcStartAngle,
                    ArcEndAngle = z.ArcEndAngle,
                    ArcRows = z.ArcRows,
                    ArcSeatsPerRow = z.ArcSeatsPerRow,
                    ArcAxisB = z.ArcAxisB,
                    CurveAmplitude = z.CurveAmplitude,
                    CurveWavelength = z.CurveWavelength,
                    CurvePhase = z.CurvePhase,
                    CurveRowGap = z.CurveRowGap,
                    CurveAngle = z.CurveAngle,
                    CurveSlantGap = z.CurveSlantGap,
                    PathPoints = z.PathPoints,
                    Seats = z.Seats.OrderBy(s => s.Code).Select(s => new AdminSeatDto
                    {
                        Id = s.Id,
                        Code = s.Code,
                        Type = s.Type.ToString(),
                        PositionX = s.PositionX,
                        PositionY = s.PositionY,
                        Window = s.Window,
                        PowerSocket = s.PowerSocket,
                        Status = s.Status.ToString()
                    }).ToList()
                }).ToList()
            })
            .ToListAsync(ct);

        return new AdminVenueDetailDto { Id = venue.Id, Name = venue.Name, Floors = floors };
    }

    private static VenueDto ToDto(Venue v) => new()
    {
        Id = v.Id,
        CityId = v.CityId,
        Name = v.Name,
        Type = v.Type.ToString(),
        Address = v.Address,
        Longitude = v.Longitude,
        Latitude = v.Latitude,
        Description = v.Description,
        OpeningTime = v.OpeningTime.ToString(@"hh\:mm"),
        ClosingTime = v.ClosingTime.ToString(@"hh\:mm"),
        Status = v.Status.ToString()
    };
}

public class VenueDto
{
    public long Id { get; set; }
    public long CityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public string? Description { get; set; }
    public string OpeningTime { get; set; } = string.Empty;
    public string ClosingTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class AdminVenueDetailDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<AdminFloorDto> Floors { get; set; } = new();
}

public class AdminFloorDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<AreaDto> Areas { get; set; } = new();
    public List<PoiDto> Pois { get; set; } = new();
    public List<AdminZoneDto> Zones { get; set; } = new();
}

public class AdminZoneDto
{
    public long Id { get; set; }
    public long? AreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int GridRows { get; set; }
    public int GridCols { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public string LayoutMode { get; set; } = "grid";
    public int TableSeatCols { get; set; } = 2;
    public int TableSeatRows { get; set; } = 2;
    public double TableGapX { get; set; } = 1;
    public double TableGapY { get; set; } = 1;
    public int TablesX { get; set; } = 1;
    public int TablesY { get; set; } = 1;
    public double ArcRadius { get; set; } = 8;
    public double ArcRadiusStep { get; set; } = 1.5;
    public double ArcStartAngle { get; set; } = 180;
    public double ArcEndAngle { get; set; } = 360;
    public int ArcRows { get; set; } = 3;
    public int ArcSeatsPerRow { get; set; } = 8;
    public double ArcAxisB { get; set; } = 8;
    public double CurveAmplitude { get; set; } = 2;
    public double CurveWavelength { get; set; } = 6;
    public double CurvePhase { get; set; } = 0;
    public double CurveRowGap { get; set; } = 2;
    public double CurveAngle { get; set; } = 30;
    public double CurveSlantGap { get; set; } = 2;
    public string? PathPoints { get; set; }
    public List<AdminSeatDto> Seats { get; set; } = new();
}

public class AdminSeatDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public bool Window { get; set; }
    public bool PowerSocket { get; set; }
    public string Status { get; set; } = string.Empty;
}
