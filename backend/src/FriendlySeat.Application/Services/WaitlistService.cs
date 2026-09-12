using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class WaitlistService
{
    private readonly IAppDbContext _db;
    private readonly ConfigService _config;
    private readonly RiskService _risk;
    private readonly ReservationService _reservationService;

    public WaitlistService(IAppDbContext db, ConfigService config, RiskService risk, ReservationService reservationService)
    {
        _db = db;
        _config = config;
        _risk = risk;
        _reservationService = reservationService;
    }

    public async Task<WaitlistDto> JoinAsync(long userId, long shareId, CancellationToken ct = default)
    {
        var share = await _db.SeatShares.FirstOrDefaultAsync(s => s.Id == shareId, ct)
            ?? throw AppException.NotFound("分享不存在");

        if (share.OwnerUserId == userId)
            throw AppException.BadRequest("cannot_wait_own", "不能候补自己分享的座位");

        // 信用与风控拦截：低信用/高风险/封禁用户不能加入候补
        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        if (user.Status == UserStatus.Banned)
            throw AppException.Forbidden("账号已被封禁，无法加入候补");
        if (user.CreditScore < 30)
            throw AppException.Forbidden("信用分过低，暂时无法加入候补");
        if (await _risk.IsRestrictedAsync(userId, ct))
            throw AppException.Forbidden("账号存在风险记录，暂时无法加入候补");

        if (share.Status != SeatShareStatus.Available && share.Status != SeatShareStatus.Reserved)
            throw AppException.BadRequest("share_not_waitable", "该分享已结束");

        var existing = await _db.ReservationWaitlists.FirstOrDefaultAsync(
            w => w.ShareId == shareId && w.UserId == userId && w.Status == WaitlistStatus.Waiting, ct);
        if (existing is not null)
        {
            throw AppException.Conflict("already_waiting", "你已在候补队列中");
        }

        var maxPosition = await _db.ReservationWaitlists
            .Where(w => w.ShareId == shareId && w.Status == WaitlistStatus.Waiting)
            .Select(w => (int?)w.Position)
            .MaxAsync(ct) ?? 0;

        var entry = new ReservationWaitlist
        {
            ShareId = shareId,
            UserId = userId,
            Position = maxPosition + 1,
            Status = WaitlistStatus.Waiting,
            CreatedAt = DateTime.UtcNow
        };
        _db.ReservationWaitlists.Add(entry);
        await _db.SaveChangesAsync(ct);

        return await GetDtoAsync(entry.Id, ct) ?? throw AppException.NotFound();
    }

    public async Task CancelAsync(long waitlistId, long userId, CancellationToken ct = default)
    {
        var entry = await _db.ReservationWaitlists.FirstOrDefaultAsync(w => w.Id == waitlistId, ct)
            ?? throw AppException.NotFound("候补不存在");

        if (entry.UserId != userId)
            throw AppException.Forbidden("只能取消自己的候补");

        if (entry.Status != WaitlistStatus.Waiting && entry.Status != WaitlistStatus.Notified)
            throw AppException.BadRequest("waitlist_not_active", "该候补已失效");

        entry.Status = WaitlistStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<WaitlistDto>> GetMyAsync(long userId, CancellationToken ct = default)
    {
        return await _db.ReservationWaitlists
            .Where(w => w.UserId == userId)
            .Include(w => w.Share!)
                .ThenInclude(s => s.Seat!)
                    .ThenInclude(s => s.Zone!)
                        .ThenInclude(z => z.Floor!)
                            .ThenInclude(f => f.Venue)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WaitlistDto
            {
                Id = w.Id,
                ShareId = w.ShareId,
                SeatId = w.Share!.SeatId,
                Position = w.Position,
                Status = w.Status.ToString(),
                CreatedAt = w.CreatedAt,
                SeatCode = w.Share!.Seat!.Code,
                VenueName = w.Share.Seat.Zone!.Floor!.Venue!.Name,
                StartAt = w.Share.StartAt,
                EndAt = w.Share.EndAt
            })
            .ToListAsync(ct);
    }

    public async Task<WaitlistPreferenceDto> CreatePreferenceAsync(long userId, WaitlistPreferenceRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var venue = await _db.Venues.FirstOrDefaultAsync(v => v.Id == request.VenueId, ct)
            ?? throw AppException.NotFound("场馆不存在");
        if (venue.Status != EntityStatus.Active)
            throw AppException.BadRequest("venue_inactive", "场馆未开放");

        Floor? floor = null;
        if (request.FloorId.HasValue)
        {
            floor = await _db.Floors.FirstOrDefaultAsync(f => f.Id == request.FloorId.Value && f.VenueId == request.VenueId, ct)
                ?? throw AppException.BadRequest("floor_invalid", "楼层不存在或不属于该场馆");
        }
        Area? area = null;
        if (request.AreaId.HasValue)
        {
            area = await _db.Areas.FirstOrDefaultAsync(a => a.Id == request.AreaId.Value && a.Floor!.VenueId == request.VenueId, ct)
                ?? throw AppException.BadRequest("area_invalid", "区域不存在或不属于该场馆");
            if (request.FloorId.HasValue && area.FloorId != request.FloorId.Value)
                throw AppException.BadRequest("area_floor_mismatch", "区域与楼层不匹配");
        }

        var validPrefs = new[] { "none", "window", "socket", "quiet" };
        if (!validPrefs.Contains(request.Preference))
            throw AppException.BadRequest("preference_invalid", "候补偏好无效");

        // 同一用户同一范围（场馆+楼层+区域+偏好）只允许一条进行中的候补
        var active = await _db.WaitlistPreferences
            .AnyAsync(p => p.UserId == userId
                && p.VenueId == request.VenueId
                && p.FloorId == request.FloorId
                && p.AreaId == request.AreaId
                && p.Preference == request.Preference
                && p.Status == WaitlistPreferenceStatus.Active, ct);
        if (active)
            throw AppException.Conflict("already_waiting", "你已提交过同类候补，等待自动预约即可");

        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        if (user.Status == UserStatus.Banned)
            throw AppException.Forbidden("账号已被封禁，无法提交候补");
        if (user.CreditScore < 30)
            throw AppException.Forbidden("信用分过低，暂时无法提交候补");
        if (await _risk.IsRestrictedAsync(userId, ct))
            throw AppException.Forbidden("账号存在风险记录，暂时无法提交候补");

        var pref = new WaitlistPreference
        {
            UserId = userId,
            VenueId = request.VenueId,
            FloorId = request.FloorId,
            AreaId = request.AreaId,
            Preference = request.Preference,
            Status = WaitlistPreferenceStatus.Active,
            CreatedAt = now
        };
        _db.WaitlistPreferences.Add(pref);
        await _db.SaveChangesAsync(ct);

        // 立即尝试：若当前已有匹配且可预约的分享，直接自动预约
        await TryBookPreferenceNowAsync(pref, ct);

        return await GetPreferenceDtoAsync(pref.Id, ct) ?? throw AppException.NotFound();
    }

    // 创建偏好后立即兜底：在可选范围内找一条正在可预约的分享，交给自动代约逻辑匹配
    private async Task TryBookPreferenceNowAsync(WaitlistPreference pref, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var candidateId = await _db.SeatShares
            .Where(s => s.Status == SeatShareStatus.Available
                && s.EndAt > now
                && s.Seat!.Zone!.Floor!.VenueId == pref.VenueId
                && (!pref.FloorId.HasValue || s.Seat.Zone.FloorId == pref.FloorId.Value)
                && (!pref.AreaId.HasValue || s.Seat.Zone.AreaId == pref.AreaId.Value))
            .OrderBy(s => s.CreatedAt)
            .Select(s => (long?)s.Id)
            .FirstOrDefaultAsync(ct);
        if (!candidateId.HasValue) return;

        await _reservationService.TryAutoBookPreferenceAsync(candidateId.Value, ct);
    }

    public async Task<List<WaitlistPreferenceDto>> GetMyPreferencesAsync(long userId, CancellationToken ct = default)
    {
        return await _db.WaitlistPreferences
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new WaitlistPreferenceDto
            {
                Id = p.Id,
                VenueId = p.VenueId,
                VenueName = p.Venue!.Name,
                FloorId = p.FloorId,
                FloorName = p.Floor != null ? p.Floor.Name : null,
                AreaId = p.AreaId,
                AreaName = p.Area != null ? p.Area.Name : null,
                Preference = p.Preference,
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt,
                BookedAt = p.BookedAt,
                ReservationId = p.ReservationId,
                BookedSeatId = p.Reservation != null && p.Reservation.Seat != null ? p.Reservation.Seat.Id : default,
                ReservationStartAt = p.Reservation != null ? p.Reservation.StartAt : default,
                ReservationEndAt = p.Reservation != null ? p.Reservation.EndAt : default,
                BookedSeatCode = p.Reservation != null && p.Reservation.Seat != null ? p.Reservation.Seat.Code : null
            })
            .ToListAsync(ct);
    }

    public async Task CancelPreferenceAsync(long preferenceId, long userId, CancellationToken ct = default)
    {
        var pref = await _db.WaitlistPreferences.FirstOrDefaultAsync(p => p.Id == preferenceId, ct)
            ?? throw AppException.NotFound("候补偏好不存在");

        if (pref.UserId != userId)
            throw AppException.Forbidden("只能取消自己的候补偏好");

        if (pref.Status != WaitlistPreferenceStatus.Active)
            throw AppException.BadRequest("not_active", "该候补偏好已失效");

        pref.Status = WaitlistPreferenceStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<WaitlistPreferenceDto?> GetPreferenceDtoAsync(long id, CancellationToken ct)
    {
        return await _db.WaitlistPreferences
            .Where(p => p.Id == id)
            .Select(p => new WaitlistPreferenceDto
            {
                Id = p.Id,
                VenueId = p.VenueId,
                VenueName = p.Venue!.Name,
                FloorId = p.FloorId,
                FloorName = p.Floor != null ? p.Floor.Name : null,
                AreaId = p.AreaId,
                AreaName = p.Area != null ? p.Area.Name : null,
                Preference = p.Preference,
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt,
                BookedAt = p.BookedAt,
                ReservationId = p.ReservationId,
                BookedSeatId = p.Reservation != null && p.Reservation.Seat != null ? p.Reservation.Seat.Id : default,
                ReservationStartAt = p.Reservation != null ? p.Reservation.StartAt : default,
                ReservationEndAt = p.Reservation != null ? p.Reservation.EndAt : default,
                BookedSeatCode = p.Reservation != null && p.Reservation.Seat != null ? p.Reservation.Seat.Code : null
            })
            .FirstOrDefaultAsync(ct);
    }

    private async Task<WaitlistDto?> GetDtoAsync(long id, CancellationToken ct)
    {
        return await _db.ReservationWaitlists
            .Where(w => w.Id == id)
            .Include(w => w.Share!)
                .ThenInclude(s => s.Seat!)
                    .ThenInclude(s => s.Zone!)
                        .ThenInclude(z => z.Floor!)
                            .ThenInclude(f => f.Venue)
            .Select(w => new WaitlistDto
            {
                Id = w.Id,
                ShareId = w.ShareId,
                SeatId = w.Share!.SeatId,
                Position = w.Position,
                Status = w.Status.ToString(),
                CreatedAt = w.CreatedAt,
                SeatCode = w.Share!.Seat!.Code,
                VenueName = w.Share.Seat.Zone!.Floor!.Venue!.Name,
                StartAt = w.Share.StartAt,
                EndAt = w.Share.EndAt
            })
            .FirstOrDefaultAsync(ct);
    }
}
