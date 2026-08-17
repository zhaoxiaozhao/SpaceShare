using FriendlySeat.Domain.Entities;

namespace FriendlySeat.Application.Common;

public interface ICurrentUser
{
    long? UserId { get; }
    bool IsAuthenticated { get; }
}

public interface ICurrentAdmin
{
    long? AdminId { get; }
    AdminRole? Role { get; }
    bool IsAuthenticated { get; }
}

public interface IDateTime
{
    DateTime UtcNow { get; }
}

public interface IRedisCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}

public interface IDistributedLock
{
    Task<IDistributedLockHandle?> AcquireAsync(string key, TimeSpan timeout, CancellationToken ct = default);
}

public interface IDistributedLockHandle : IAsyncDisposable
{
    bool IsAcquired { get; }
}

public interface IWechatService
{
    Task<WechatSessionResult> Code2SessionAsync(string code, CancellationToken ct = default);
}

public record WechatSessionResult(string OpenId, string? UnionId);

public interface ITokenService
{
    string CreateUserToken(User user);
    string CreateAdminToken(AdminUser admin);
}

public interface IAuditService
{
    Task LogAsync(long adminId, string action, string? entityType = null, string? entityId = null, string? detail = null, string? ip = null, CancellationToken ct = default);
}

public interface INotificationService
{
    Task SendAsync(long userId, NotificationType type, string title, string? content = null, string? data = null, CancellationToken ct = default);
}
