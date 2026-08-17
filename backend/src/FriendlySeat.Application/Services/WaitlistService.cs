using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class WaitlistService
{
    private readonly IAppDbContext _db;
    private readonly ConfigService _config;

    public WaitlistService(IAppDbContext db, ConfigService config)
    {
        _db = db;
        _config = config;
    }

    public async Task<WaitlistDto> JoinAsync(long userId, long shareId, CancellationToken ct = default)
    {
        var share = await _db.SeatShares.FirstOrDefaultAsync(s => s.Id == shareId, ct)
            ?? throw AppException.NotFound("分享不存在");

        if (share.OwnerUserId == userId)
            throw AppException.BadRequest("cannot_wait_own", "不能候补自己分享的座位");

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
