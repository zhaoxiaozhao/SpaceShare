using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly ICurrentAdmin _currentAdmin;

    public AdminAuthController(AuthService auth, ICurrentAdmin currentAdmin)
    {
        _auth = auth;
        _currentAdmin = currentAdmin;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AdminAuthResult>> Login([FromBody] AdminLoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        return Ok(await _auth.AdminLoginAsync(request, ip, ct));
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new { AdminId = _currentAdmin.AdminId, Role = _currentAdmin.Role?.ToString() });
    }
}
