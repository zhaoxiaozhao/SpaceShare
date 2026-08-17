using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class CheckInRequest
{
    public long SeatId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class CheckInResult
{
    public long SessionId { get; set; }
    public long SeatId { get; set; }
    public string SeatCode { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;

    /// <summary>场馆闭馆时间（HH:mm），用于分享页计算可选离开时长</summary>
    public string ClosingTime { get; set; } = "22:00";

    public DateTime StartedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SeatSessionService
{
    private readonly IAppDbContext _db;
    private readonly IRedisCache _cache;
    private readonly INotificationService _notifications;

    public SeatSessionService(IAppDbContext db, IRedisCache cache, INotificationService notifications)
    {
        _db = db;
        _cache = cache;
        _notifications = notifications;
    }

    /// <summary>
    /// 到场签到：用户确认自己在某个座位使用中，从而获得分享资格（真实使用原则）。
    /// </summary>
    public async Task<CheckInResult> CheckInAsync(long userId, CheckInRequest request, CancellationToken ct = default)
    {
        var seat = await _db.Seats
            .Include(s => s.Zone!)
                .ThenInclude(z => z.Floor!)
                    .ThenInclude(f => f.Venue)
            .FirstOrDefaultAsync(s => s.Id == request.SeatId, ct)
            ?? throw AppException.NotFound("座位不存在");

        // 场馆允许范围校验（粗粒度）
        var venue = seat.Zone!.Floor!.Venue!;
        if (request.Latitude.HasValue && request.Longitude.HasValue
            && venue.Latitude.HasValue && venue.Longitude.HasValue)
        {
            var dist = HaversineKm(request.Latitude.Value, request.Longitude.Value, venue.Latitude.Value, venue.Longitude.Value);
            if (dist > 5.0)
                throw AppException.BadRequest("arrive_out_of_range", "你不在场馆附近，无法签到");
        }

        var now = DateTime.UtcNow;

        // 一个用户同一时间只能有一个 active session
        var existing = await _db.SeatSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SeatSessionStatus.Active, ct);
        if (existing is not null)
        {
            if (existing.SeatId == request.SeatId)
            {
                return new CheckInResult
                {
                    SessionId = existing.Id,
                    SeatId = existing.SeatId,
                    SeatCode = seat.Code,
                    VenueName = venue.Name,
                    StartedAt = existing.StartedAt,
                    Message = "你已在使用该座位"
                };
            }
            throw AppException.BadRequest("already_using", "你当前正在使用另一个座位，请先结束当前座位");
        }

        // 座位当前不能被预约占用
        var conflicting = await _db.Reservations.AnyAsync(
            r => r.SeatId == request.SeatId && r.EndAt > now
                && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Arrived), ct);
        if (conflicting)
            throw AppException.Conflict("seat_reserved", "该座位当前已被预约占用");

        var session = new SeatSession
        {
            SeatId = request.SeatId,
            UserId = userId,
            StartedAt = now,
            ArrivalAt = now,
            Status = SeatSessionStatus.Active,
            CreatedAt = now
        };
        _db.SeatSessions.Add(session);
        seat.Status = SeatStatus.Occupied;
        await _db.SaveChangesAsync(ct);

        await InvalidateVenueCacheAsync(request.SeatId, ct);

        return new CheckInResult
        {
            SessionId = session.Id,
            SeatId = seat.Id,
            SeatCode = seat.Code,
            VenueName = venue.Name,
            ClosingTime = venue.ClosingTime.ToString(@"hh\:mm"),
            StartedAt = session.StartedAt,
            Message = "签到成功，欢迎到座"
        };
    }

    public async Task<CheckInResult?> GetMySessionAsync(long userId, CancellationToken ct = default)
    {
        var session = await _db.SeatSessions
            .Include(s => s.Seat!)
                .ThenInclude(s => s.Zone!)
                    .ThenInclude(z => z.Floor!)
                        .ThenInclude(f => f.Venue)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SeatSessionStatus.Active, ct);

        if (session is null) return null;

        return new CheckInResult
        {
            SessionId = session.Id,
            SeatId = session.SeatId,
            SeatCode = session.Seat!.Code,
            VenueName = session.Seat.Zone!.Floor!.Venue!.Name,
            ClosingTime = session.Seat.Zone!.Floor!.Venue!.ClosingTime.ToString(@"hh\:mm"),
            StartedAt = session.StartedAt,
            Message = "使用中"
        };
    }

    public async Task EndSessionAsync(long userId, CancellationToken ct = default)
    {
        var session = await _db.SeatSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SeatSessionStatus.Active, ct)
            ?? throw AppException.NotFound("当前没有使用中的座位");

        session.Status = SeatSessionStatus.Completed;
        session.ActualEndAt = DateTime.UtcNow;

        // 有未结束的分享则标记为过期
        var activeShares = await _db.SeatShares
            .Where(s => s.SourceSessionId == session.Id
                && (s.Status == SeatShareStatus.Available || s.Status == SeatShareStatus.Reserved))
            .ToListAsync(ct);
        foreach (var share in activeShares)
        {
            share.Status = SeatShareStatus.Expired;
        }

        var seat = await _db.Seats.FirstAsync(s => s.Id == session.SeatId, ct);
        seat.Status = SeatStatus.Available;

        await _db.SaveChangesAsync(ct);
        await InvalidateVenueCacheAsync(session.SeatId, ct);
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
