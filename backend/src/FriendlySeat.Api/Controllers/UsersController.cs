using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly UserProfileService _profiles;
    private readonly ICurrentUser _currentUser;

    public UsersController(UserProfileService profiles, ICurrentUser currentUser)
    {
        _profiles = profiles;
        _currentUser = currentUser;
    }

    /// <summary>访客主页：公开昵称/头像、对方公开的友邻画像、公开帖子（匿名可看）</summary>
    [HttpGet("{id:long}/profile")]
    [AllowAnonymous]
    public async Task<ActionResult<UserProfileDto>> Profile(long id, CancellationToken ct)
    {
        var dto = await _profiles.GetAsync(id, _currentUser.UserId, ct);
        if (dto is null) return NotFound();
        return Ok(dto);
    }
}
