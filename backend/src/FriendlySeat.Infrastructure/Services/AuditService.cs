using FriendlySeat.Application.Common;
using FriendlySeat.Domain.Entities;

namespace FriendlySeat.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAppDbContext _db;

    public AuditService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(long adminId, string action, string? entityType = null, string? entityId = null, string? detail = null, string? ip = null, CancellationToken ct = default)
    {
        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminUserId = adminId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail,
            IpAddress = ip,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
