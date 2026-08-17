using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class VenueService
{
    private readonly IAppDbContext _db;
    private readonly IRedisCache _cache;

    public VenueService(IAppDbContext db, IRedisCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<List<CityDto>> GetCitiesAsync(CancellationToken ct = default)
    {
        return await _db.Cities
            .Where(c => c.Status == EntityStatus.Active)
            .OrderBy(c => c.Province).ThenBy(c => c.Name)
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

    public async Task<List<VenueListItemDto>> GetVenuesAsync(
        long? cityId, string? keyword, double? lat, double? lng, double? radiusKm, CancellationToken ct = default)
    {
        var query = _db.Venues
            .Where(v => v.Status == EntityStatus.Active);

        if (cityId.HasValue) query = query.Where(v => v.CityId == cityId.Value);
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(v => v.Name.Contains(keyword));

        var venues = await query.ToListAsync(ct);

        var now = DateTime.UtcNow;
        var result = new List<VenueListItemDto>();
        foreach (var v in venues)
        {
            var (seatCount, availableCount) = await GetVenueCountsAsync(v.Id, now, ct);

            var dto = new VenueListItemDto
            {
                Id = v.Id,
                Name = v.Name,
                Type = v.Type.ToString(),
                Address = v.Address,
                Longitude = v.Longitude,
                Latitude = v.Latitude,
                OpeningTime = v.OpeningTime.ToString(@"hh\:mm"),
                ClosingTime = v.ClosingTime.ToString(@"hh\:mm"),
                SeatCount = seatCount,
                AvailableCount = availableCount
            };

            if (lat.HasValue && lng.HasValue && v.Latitude.HasValue && v.Longitude.HasValue)
            {
                var d = HaversineKm(lat.Value, lng.Value, v.Latitude.Value, v.Longitude.Value);
                if (radiusKm.HasValue && d > radiusKm.Value) continue;
                dto.DistanceKm = Math.Round(d, 2);
            }

            result.Add(dto);
        }

        return result;
    }

    public async Task<VenueDetailDto?> GetVenueAsync(long id, CancellationToken ct = default)
    {
        var cacheKey = $"venue:{id}";
        var cached = await _cache.GetAsync<VenueDetailDto?>(cacheKey, ct);
        if (cached is not null) return cached;

        var venue = await _db.Venues
            .Include(v => v.Floors)
                .ThenInclude(f => f.Areas)
            .Include(v => v.Floors)
                .ThenInclude(f => f.Zones)
                    .ThenInclude(z => z.Seats)
            .Include(v => v.Floors)
                .ThenInclude(f => f.Pois)
            .FirstOrDefaultAsync(v => v.Id == id && v.Status == EntityStatus.Active, ct);

        if (venue is null) return null;

        var now = DateTime.UtcNow;
        var (seatCount, availableCount) = await GetVenueCountsAsync(venue.Id, now, ct);

        // 该场馆座位 → 有效分享/预约计数（用于地图状态显示）
        var venueSeatIds = venue.Floors.SelectMany(f => f.Zones).SelectMany(z => z.Seats).Select(s => s.Id).ToList();
        var shareCounts = await _db.SeatShares
            .Where(s => venueSeatIds.Contains(s.SeatId) && s.Status == SeatShareStatus.Available && s.EndAt > now)
            .GroupBy(s => s.SeatId)
            .Select(g => new { SeatId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SeatId, x => x.Count, ct);
        var reservedCounts = await _db.Reservations
            .Where(r => venueSeatIds.Contains(r.SeatId) && r.EndAt > now
                && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Arrived))
            .GroupBy(r => r.SeatId)
            .Select(g => new { SeatId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SeatId, x => x.Count, ct);

        var dto = new VenueDetailDto
        {
            Id = venue.Id,
            Name = venue.Name,
            Type = venue.Type.ToString(),
            Address = venue.Address,
            Longitude = venue.Longitude,
            Latitude = venue.Latitude,
            Description = venue.Description,
            OpeningTime = venue.OpeningTime.ToString(@"hh\:mm"),
            ClosingTime = venue.ClosingTime.ToString(@"hh\:mm"),
            SeatCount = seatCount,
            AvailableCount = availableCount,
            Floors = venue.Floors.OrderBy(f => f.SortOrder).Select(f => new FloorDto
            {
                Id = f.Id,
                Name = f.Name,
                SortOrder = f.SortOrder,
                MapImageUrl = f.MapImageUrl,
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
                Zones = f.Zones.OrderBy(z => z.SortOrder).Select(z => new ZoneDto
                {
                    Id = z.Id,
                    AreaId = z.AreaId,
                    Name = z.Name,
                    SortOrder = z.SortOrder,
                    MapImageUrl = z.MapImageUrl,
                    GridRows = z.GridRows,
                    GridCols = z.GridCols,
                    OffsetX = z.OffsetX,
                    OffsetY = z.OffsetY,
                    Seats = z.Seats.OrderBy(s => s.Code).Select(s => new SeatDto
                    {
                        Id = s.Id,
                        ZoneId = s.ZoneId,
                        Code = s.Code,
                        Type = s.Type.ToString(),
                        PositionX = s.PositionX,
                        PositionY = s.PositionY,
                        Window = s.Window,
                        PowerSocket = s.PowerSocket,
                        QuietLevel = s.QuietLevel,
                        LightLevel = s.LightLevel,
                        Status = s.Status.ToString(),
                        PhotoUrl = s.PhotoUrl,
                        Description = s.Description,
                        Verified = s.Verified,
                        CurrentShareCount = shareCounts.GetValueOrDefault(s.Id),
                        CurrentReservedCount = reservedCounts.GetValueOrDefault(s.Id)
                    }).ToList()
                }).ToList()
            }).ToList()
        };

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), ct);
        return dto;
    }

    public async Task<SeatDto?> GetSeatAsync(long id, CancellationToken ct = default)
    {
        var seat = await _db.Seats
            .Include(s => s.Zone!)
                .ThenInclude(z => z.Floor!)
                    .ThenInclude(f => f.Venue)
            .Include(s => s.Zone!)
                .ThenInclude(z => z.Area)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (seat is null) return null;

        var now = DateTime.UtcNow;
        var reservedCount = await _db.Reservations.CountAsync(
            r => r.SeatId == id && r.EndAt > now && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Arrived), ct);
        var shareCount = await _db.SeatShares.CountAsync(
            s => s.SeatId == id && s.EndAt > now && s.Status == SeatShareStatus.Available, ct);

        var venue = seat.Zone?.Floor?.Venue;
        var dto = new SeatDto
        {
            Id = seat.Id,
            ZoneId = seat.ZoneId,
            Code = seat.Code,
            Type = seat.Type.ToString(),
            PositionX = seat.PositionX,
            PositionY = seat.PositionY,
            Window = seat.Window,
            PowerSocket = seat.PowerSocket,
            QuietLevel = seat.QuietLevel,
            LightLevel = seat.LightLevel,
            Status = seat.Status.ToString(),
            PhotoUrl = seat.PhotoUrl,
            Description = seat.Description,
            Verified = seat.Verified,
            CurrentReservedCount = reservedCount,
            CurrentShareCount = shareCount,
            VenueName = venue?.Name ?? string.Empty,
            FloorName = seat.Zone?.Floor?.Name ?? string.Empty,
            AreaName = seat.Zone?.Area?.Name,
            ClosingTime = venue?.ClosingTime.ToString(@"hh\:mm") ?? "22:00"
        };

        // 展示编号（区块字母 + 座位序号）
        if (seat.Zone != null)
        {
            var letter = await GetZoneLetterAsync(seat.ZoneId, ct);
            var seatNo = seat.Code.Split('-').LastOrDefault() ?? seat.Code;
            dto.DisplayCode = $"{letter}区-{seatNo}";
        }
        return dto;
    }

    private async Task<char> GetZoneLetterAsync(long zoneId, CancellationToken ct)
    {
        var floor = await _db.Floors
            .Include(f => f.Areas)
            .Include(f => f.Zones)
            .FirstOrDefaultAsync(f => f.Zones.Any(z => z.Id == zoneId), ct);
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

        var idx = ordered.FindIndex(z => z.Id == zoneId);
        if (idx < 0) return 'A';
        return (char)('A' + idx);
    }

    private async Task<(int seatCount, int availableCount)> GetVenueCountsAsync(long venueId, DateTime now, CancellationToken ct)
    {
        var zoneIds = await _db.Zones
            .Where(z => z.Floor!.VenueId == venueId)
            .Select(z => z.Id)
            .ToListAsync(ct);

        if (zoneIds.Count == 0) return (0, 0);

        // 场馆下的所有座位 Id
        var seatIds = await _db.Seats
            .Where(s => zoneIds.Contains(s.ZoneId))
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (seatIds.Count == 0) return (0, 0);

        var seatCount = seatIds.Count;

        // 可预约 = 有可用分享的座位数
        var availableSeatIds = await _db.SeatShares
            .Where(s => seatIds.Contains(s.SeatId) && s.EndAt > now && s.Status == SeatShareStatus.Available)
            .Select(s => s.SeatId)
            .Distinct()
            .ToListAsync(ct);

        var availableCount = availableSeatIds.Count;
        return (seatCount, availableCount);
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
