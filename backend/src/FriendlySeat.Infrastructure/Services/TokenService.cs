using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FriendlySeat.Application.Common;
using FriendlySeat.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FriendlySeat.Infrastructure.Services;

public class JwtOptions
{
    public string Issuer { get; set; } = "friendly-seat";
    public string Audience { get; set; } = "friendly-seat";
    public string Key { get; set; } = "friendly-seat-dev-secret-key-change-me-in-production-0123456789";
    public int UserTokenDays { get; set; } = 7;
    public int AdminTokenHours { get; set; } = 12;
}

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string CreateUserToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("uid", user.Id.ToString()),
            new("typ", "user"),
            new("openid", user.OpenId)
        };

        return CreateToken(claims, TimeSpan.FromDays(_options.UserTokenDays));
    }

    public string CreateAdminToken(AdminUser admin)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
            new("aid", admin.Id.ToString()),
            new("typ", "admin"),
            new("role", admin.Role.ToString()),
            new("username", admin.Username)
        };

        return CreateToken(claims, TimeSpan.FromHours(_options.AdminTokenHours));
    }

    private string CreateToken(IEnumerable<Claim> claims, TimeSpan lifetime)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
