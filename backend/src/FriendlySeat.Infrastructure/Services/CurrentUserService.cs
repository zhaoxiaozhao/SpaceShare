using FriendlySeat.Application.Common;
using FriendlySeat.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace FriendlySeat.Infrastructure.Services;

public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public long? UserId
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirst("uid")?.Value;
            return long.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => UserId.HasValue;
}

public class CurrentAdminService : ICurrentAdmin
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentAdminService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public long? AdminId
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirst("aid")?.Value;
            return long.TryParse(value, out var id) ? id : null;
        }
    }

    public AdminRole? Role
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirst("role")?.Value;
            return Enum.TryParse<AdminRole>(value, out var role) ? role : null;
        }
    }

    public bool IsAuthenticated => AdminId.HasValue;
}
