using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class AdminUserListDto
{
    public long Id { get; set; }
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public int CreditScore { get; set; }
    public int RiskScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int ReservationCount { get; set; }
}

public class AdminUserDetailDto : AdminUserListDto
{
    public string OpenId { get; set; } = string.Empty;
    public List<CreditTransactionDto> CreditTransactions { get; set; } = new();
    public List<RiskEventDto> RiskEvents { get; set; } = new();
}

public class RiskEventDto
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminUserManagementService
{
    private readonly IAppDbContext _db;
    private readonly IAuditService _audit;

    public AdminUserManagementService(IAppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<List<AdminUserListDto>> GetUsersAsync(string? keyword, CancellationToken ct = default)
    {
        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(u => u.Nickname!.Contains(keyword));
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).Take(200).ToListAsync(ct);

        var result = new List<AdminUserListDto>();
        foreach (var u in users)
        {
            var count = await _db.Reservations.CountAsync(r => r.UserId == u.Id, ct);
            result.Add(new AdminUserListDto
            {
                Id = u.Id,
                Nickname = u.Nickname,
                AvatarUrl = u.AvatarUrl,
                CreditScore = u.CreditScore,
                RiskScore = u.RiskScore,
                Status = u.Status.ToString(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                ReservationCount = count
            });
        }
        return result;
    }

    public async Task<AdminUserDetailDto?> GetUserAsync(long id, CancellationToken ct = default)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u is null) return null;

        var creditTransactions = await _db.CreditTransactions
            .Where(t => t.UserId == id)
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

        var riskEvents = await _db.RiskEvents
            .Where(e => e.UserId == id)
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
            .Select(e => new RiskEventDto
            {
                Id = e.Id,
                EventType = e.EventType,
                RiskScore = e.RiskScore,
                Metadata = e.Metadata,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(ct);

        var count = await _db.Reservations.CountAsync(r => r.UserId == id, ct);

        return new AdminUserDetailDto
        {
            Id = u.Id,
            OpenId = u.OpenId,
            Nickname = u.Nickname,
            AvatarUrl = u.AvatarUrl,
            CreditScore = u.CreditScore,
            RiskScore = u.RiskScore,
            Status = u.Status.ToString(),
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt,
            ReservationCount = count,
            CreditTransactions = creditTransactions,
            RiskEvents = riskEvents
        };
    }

    public async Task SetStatusAsync(long id, UserStatus status, long operatorId, CancellationToken ct = default)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("用户不存在");
        u.Status = status;
        u.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(operatorId, "user.status", "User", id.ToString(), $"设置用户 {id} 状态为 {status}", null, ct);
    }

    public async Task AdjustCreditAsync(long id, int change, string reason, long operatorId, CancellationToken ct = default)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("用户不存在");
        u.CreditScore = Math.Clamp(u.CreditScore + change, 0, 100);
        u.UpdatedAt = DateTime.UtcNow;
        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = id,
            Change = change,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(operatorId, "user.credit", "User", id.ToString(), $"调整用户 {id} 信用 {change}: {reason}", null, ct);
    }

    public async Task AdjustRiskAsync(long id, int change, string reason, long operatorId, CancellationToken ct = default)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("用户不存在");
        u.RiskScore = Math.Clamp(u.RiskScore + change, 0, 100);
        u.UpdatedAt = DateTime.UtcNow;
        _db.RiskEvents.Add(new RiskEvent
        {
            UserId = id,
            EventType = "manual_adjust",
            RiskScore = change,
            Metadata = reason,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(operatorId, "user.risk", "User", id.ToString(), $"调整用户 {id} 风险 {change}: {reason}", null, ct);
    }
}
