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

    public AdminVenueManagementService(IAppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
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
                ClosingTime = v.ClosingTime.ToString(@"hh\:mm")
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

    public async Task AddFloorAsync(AdminFloorRequest request, long operatorId, CancellationToken ct = default)
    {
        var venue = await _db.Venues.AnyAsync(v => v.Id == request.VenueId, ct);
        if (!venue) throw AppException.NotFound("场馆不存在");

        var floor = new Floor { VenueId = request.VenueId, Name = request.Name, SortOrder = request.SortOrder };
        _db.Floors.Add(floor);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "floor.create", "Floor", floor.Id.ToString(), $"创建楼层 {floor.Name}", null, ct);
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
            OffsetY = request.OffsetY
        };
        _db.Zones.Add(zone);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "zone.create", "Zone", zone.Id.ToString(), $"创建座位区块 {zone.Name}", null, ct);
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
        if (request.AreaId.HasValue)
        {
            var areaOk = await _db.Areas.AnyAsync(a => a.Id == request.AreaId.Value && a.FloorId == zone.FloorId, ct);
            if (areaOk) zone.AreaId = request.AreaId;
        }
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "zone.update", "Zone", zoneId.ToString(), $"更新座位区块 {zone.Name} 网格 {zone.GridRows}x{zone.GridCols} 偏移({zone.OffsetX},{zone.OffsetY})", null, ct);
    }

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
    }

    public async Task DeleteSeatAsync(long seatId, long operatorId, CancellationToken ct = default)
    {
        var seat = await _db.Seats.FirstOrDefaultAsync(s => s.Id == seatId, ct)
            ?? throw AppException.NotFound("座位不存在");
        _db.Seats.Remove(seat);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "seat.delete", "Seat", seatId.ToString(), $"删除座位 {seat.Code}", null, ct);
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

        return ToPoiDto(poi);
    }

    public async Task DeletePoiAsync(long poiId, long operatorId, CancellationToken ct = default)
    {
        var poi = await _db.FloorPois.FirstOrDefaultAsync(p => p.Id == poiId, ct)
            ?? throw AppException.NotFound("标志物不存在");
        _db.FloorPois.Remove(poi);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "poi.delete", "FloorPoi", poiId.ToString(), $"删除标志物 {poi.Name}", null, ct);
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
