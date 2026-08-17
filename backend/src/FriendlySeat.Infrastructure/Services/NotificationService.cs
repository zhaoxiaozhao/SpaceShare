using FriendlySeat.Application.Common;
using FriendlySeat.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FriendlySeat.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IAppDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IAppDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendAsync(long userId, NotificationType type, string title, string? content = null, string? data = null, CancellationToken ct = default)
    {
        try
        {
            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Content = content,
                Data = data,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
            // MVP 阶段先入库，后续接微信订阅消息推送
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送通知失败 userId={UserId} type={Type}", userId, type);
        }
    }
}
