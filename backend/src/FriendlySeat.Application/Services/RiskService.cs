using FriendlySeat.Application.Common;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class RiskService
{
    private readonly IAppDbContext _db;

    public RiskService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task RecordEventAsync(long userId, string eventType, int riskScore, string? metadata = null, CancellationToken ct = default)
    {
        _db.RiskEvents.Add(new RiskEvent
        {
            UserId = userId,
            EventType = eventType,
            RiskScore = riskScore,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow
        });

        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        user.RiskScore = Math.Clamp(user.RiskScore + riskScore, 0, 100);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> IsRestrictedAsync(long userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        return user.RiskScore > 60 || user.Status == UserStatus.Banned;
    }
}
