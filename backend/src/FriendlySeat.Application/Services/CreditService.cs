using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class CreditService
{
    private readonly IAppDbContext _db;
    private readonly ConfigService _config;

    public CreditService(IAppDbContext db, ConfigService config)
    {
        _db = db;
        _config = config;
    }

    public async Task<CreditSummaryDto> GetSummaryAsync(long userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);

        var transactions = await _db.CreditTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .Select(t => new CreditTransactionDto
            {
                Id = t.Id,
                Change = t.Change,
                Reason = t.Reason,
                ReferenceType = t.ReferenceType,
                ReferenceId = t.ReferenceId,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(ct);

        return new CreditSummaryDto
        {
            Score = user.CreditScore,
            Level = ConfigService.CreditLevel(user.CreditScore),
            Transactions = transactions
        };
    }

    public async Task<int> AdjustAsync(long userId, int change, string reason, string? referenceType = null, long? referenceId = null, CancellationToken ct = default)
    {
        var rules = await _config.GetCreditRulesAsync(ct);
        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);

        var newScore = Math.Clamp(user.CreditScore + change, 0, rules.MaxScore);

        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = userId,
            Change = change,
            Reason = reason,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            CreatedAt = DateTime.UtcNow
        });
        user.CreditScore = newScore;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return newScore;
    }

    public async Task<PublicContributionDto> GetContributionAsync(long userId, CancellationToken ct = default)
    {
        var row = await _db.PublicContributions.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (row is null) return new PublicContributionDto();

        return new PublicContributionDto
        {
            ShareCount = row.ShareCount,
            ShareHours = row.ShareHours,
            HelpedCount = row.HelpedCount,
            OnTimeCount = row.OnTimeCount
        };
    }

    public async Task TrackContributionAsync(long userId, string action, double hours = 0, CancellationToken ct = default)
    {
        var row = await _db.PublicContributions.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (row is null)
        {
            row = new PublicContribution { UserId = userId, UpdatedAt = DateTime.UtcNow };
            _db.PublicContributions.Add(row);
        }

        switch (action)
        {
            case "share_created":
                row.ShareCount++;
                row.ShareHours += hours;
                break;
            case "helped":
                row.HelpedCount++;
                break;
            case "on_time":
                row.OnTimeCount++;
                break;
        }
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
