using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("wechat/login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> WechatLogin([FromBody] WechatLoginRequest request, CancellationToken ct)
    {
        return Ok(await _auth.WechatLoginAsync(request, ct));
    }

    [HttpPost("refresh")]
    [Authorize]
    public IActionResult Refresh()
    {
        return Ok(new { message = "当前使用无状态 JWT，无需刷新；过期后请重新登录" });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // JWT 无状态，客户端删除本地 Token 即可
        return Ok();
    }
}
