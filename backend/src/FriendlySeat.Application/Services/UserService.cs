using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class UserService
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ConfigService _config;

    public UserService(IAppDbContext db, INotificationService notifications, ConfigService config)
    {
        _db = db;
        _notifications = notifications;
        _config = config;
    }

    /// <summary>返回小程序端可用于请求订阅的消息模板（key→模板ID），未配置的模板不返回</summary>
    public async Task<Dictionary<string, string>> GetSubscribeTemplatesAsync(CancellationToken ct = default)
    {
        var keys = new[] { "reservation_created", "reservation_starting", "arrival_required", "waitlist_available" };
        var result = new Dictionary<string, string>();
        foreach (var key in keys)
        {
            var id = await _config.GetValueAsync(ConfigCategory.NotificationTemplates, key, ct);
            if (!string.IsNullOrEmpty(id)) result[key] = id;
        }
        return result;
    }

    public async Task<UserDto> GetProfileAsync(long userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        return AuthService.ToDto(user);
    }

    public async Task<UserDto> UpdateProfileAsync(long userId, UserProfileUpdateRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        if (!string.IsNullOrWhiteSpace(request.Nickname)) user.Nickname = request.Nickname.Trim();
        if (!string.IsNullOrWhiteSpace(request.AvatarUrl)) user.AvatarUrl = request.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return AuthService.ToDto(user);
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync(long userId, bool? unread, CancellationToken ct = default)
    {
        var query = _db.Notifications.Where(n => n.UserId == userId);
        if (unread == true) query = query.Where(n => !n.IsRead);

        return await query.OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Title = n.Title,
                Content = n.Content,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task MarkNotificationsReadAsync(long userId, CancellationToken ct = default)
    {
        var items = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        foreach (var item in items) item.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnreadCountAsync(long userId, CancellationToken ct = default)
    {
        await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default)
    {
        return await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    /// <summary>注销账号：匿名化个人信息并禁用账号（预约等业务记录按合规要求脱敏保留）</summary>
    public async Task DeleteAccountAsync(long userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw AppException.NotFound("账号不存在");

        user.Nickname = null;
        user.AvatarUrl = null;
        user.UnionId = null;
        user.OpenId = "deleted_" + user.Id; // 匿名化标识，防止复用原 openid 重建身份关联
        user.Status = UserStatus.Banned;
        user.UpdatedAt = DateTime.UtcNow;

        // 删除用户联系方式等个人敏感信息（如有）
        var contacts = await _db.UserContacts.Where(c => c.UserId == userId).ToListAsync(ct);
        if (contacts.Count > 0)
        {
            _db.UserContacts.RemoveRange(contacts);
        }

        await _db.SaveChangesAsync(ct);
    }
}
