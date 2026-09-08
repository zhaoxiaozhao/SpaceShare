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
    private readonly INotificationService _notification;

    public AdminUserManagementService(IAppDbContext db, IAuditService audit, INotificationService notification)
    {
        _db = db;
        _audit = audit;
        _notification = notification;
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

    /// <summary>
    /// 给指定用户生成一组演示通知（供提审 / 演示站内通知功能）。
    /// 仅写入站内通知，不触发微信订阅消息推送，避免打扰真实用户。
    /// </summary>
    public async Task SendDemoNotificationsAsync(long id, long operatorId, CancellationToken ct = default)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("用户不存在");

        var demos = new[]
        {
            (NotificationType.ReservationCreated, "预约成功", "您已成功预约座位 A-008，请按时到座。"),
            (NotificationType.ReservationStarting, "预约即将开始", "您的预约即将在 15 分钟后开始，请提前到馆。"),
            (NotificationType.ArrivalRequired, "到座提醒", "请前往座位扫码签到确认到座。"),
            (NotificationType.ReservationExpired, "预约已过期", "您的预约已超时未到座，已自动释放。"),
            (NotificationType.ReservationCancelled, "预约已取消", "您的预约已取消，座位已释放。"),
            (NotificationType.WaitlistAvailable, "候补成功", "候补的座位已有空位，请尽快预约。"),
            (NotificationType.CreditChanged, "信用分变动", "本次履约良好，信用分 +1。"),
            (NotificationType.ReportResult, "举报处理结果", "您提交的举报已处理，感谢反馈。"),
            (NotificationType.System, "系统通知", "欢迎使用友邻座，祝您学习愉快。")
        };

        foreach (var (type, title, content) in demos)
        {
            await _notification.SendAsync(id, type, title, content, null, ct);
        }
        await _audit.LogAsync(operatorId, "user.demo_notifications", "User", id.ToString(), "生成演示通知", null, ct);
    }
}
