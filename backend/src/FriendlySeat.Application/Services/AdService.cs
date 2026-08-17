using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class AdService
{
    private readonly IAppDbContext _db;

    public AdService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AdDto>> GetAdsAsync(string placement, long? userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var ads = await _db.Advertisements
            .Where(a => a.Placement == placement
                && a.Status == AdStatus.Active
                && a.StartAt <= now && a.EndAt > now)
            .OrderByDescending(a => a.EndAt)
            .Take(5)
            .Select(a => new AdDto
            {
                Id = a.Id,
                Title = a.Title,
                ImageUrl = a.ImageUrl,
                TargetUrl = a.TargetUrl,
                Placement = a.Placement
            })
            .ToListAsync(ct);

        if (userId.HasValue)
        {
            foreach (var ad in ads)
            {
                _db.AdImpressions.Add(new AdImpression { AdId = ad.Id, UserId = userId.Value, Placement = placement, CreatedAt = now });
            }
            await _db.SaveChangesAsync(ct);
        }

        return ads;
    }

    public async Task ClickAsync(long adId, long userId, CancellationToken ct = default)
    {
        var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == adId, ct);
        if (ad is null || ad.Status != AdStatus.Active) return;

        _db.AdClicks.Add(new AdClick { AdId = adId, UserId = userId, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
    }
}
