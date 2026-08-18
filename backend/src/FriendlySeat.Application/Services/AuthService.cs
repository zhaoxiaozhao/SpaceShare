using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class AuthService
{
    private readonly IAppDbContext _db;
    private readonly IWechatService _wechat;
    private readonly ITokenService _tokens;
    private readonly ConfigService _config;

    public AuthService(IAppDbContext db, IWechatService wechat, ITokenService tokens, ConfigService config)
    {
        _db = db;
        _wechat = wechat;
        _tokens = tokens;
        _config = config;
    }

    public async Task<AuthResult> WechatLoginAsync(WechatLoginRequest request, CancellationToken ct = default)
    {
        var session = await _wechat.Code2SessionAsync(request.Code, ct);
        if (string.IsNullOrEmpty(session.OpenId))
        {
            throw AppException.BadRequest("wechat_code_invalid", "微信登录凭证无效");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.OpenId == session.OpenId, ct);
        var now = DateTime.UtcNow;

        if (user is null)
        {
            user = new User
            {
                OpenId = session.OpenId,
                UnionId = session.UnionId,
                Nickname = string.IsNullOrWhiteSpace(request.Nickname) ? "友邻座友邻" : request.Nickname.Trim(),
                AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl,
                Status = UserStatus.Active,
                CreditScore = 100,
                RiskScore = 0,
                CreatedAt = now,
                UpdatedAt = now,
                LastLoginAt = now
            };
            _db.Users.Add(user);
        }
        else
        {
            if (user.Status == UserStatus.Banned)
            {
                throw AppException.Forbidden("账号已被封禁，如有疑问请联系管理员");
            }
            user.LastLoginAt = now;
            user.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
        return await BuildAuthResultAsync(user, ct);
    }

    private async Task<AuthResult> BuildAuthResultAsync(User user, CancellationToken ct)
    {
        var token = _tokens.CreateUserToken(user);
        return new AuthResult
        {
            Token = token,
            ExpiresIn = 7 * 24 * 3600,
            User = ToDto(user)
        };
    }

    public static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Nickname = user.Nickname,
        AvatarUrl = user.AvatarUrl,
        CreditScore = user.CreditScore,
        CreditLevel = ConfigService.CreditLevel(user.CreditScore),
        RiskScore = user.RiskScore,
        Status = user.Status.ToString(),
        CreatedAt = user.CreatedAt
    };

    public async Task<AdminAuthResult> AdminLoginAsync(AdminLoginRequest request, string? ip, CancellationToken ct = default)
    {
        var admin = await _db.AdminUsers.FirstOrDefaultAsync(a => a.Username == request.Username, ct);
        if (admin is null || admin.Status != EntityStatus.Active)
        {
            throw AppException.Unauthorized("用户名或密码错误");
        }

        var ok = BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash);
        if (!ok)
        {
            throw AppException.Unauthorized("用户名或密码错误");
        }

        admin.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new AdminAuthResult
        {
            Token = _tokens.CreateAdminToken(admin),
            ExpiresIn = 12 * 3600,
            Admin = new AdminDto
            {
                Id = admin.Id,
                Username = admin.Username,
                DisplayName = admin.DisplayName,
                Role = admin.Role.ToString()
            }
        };
    }
}
