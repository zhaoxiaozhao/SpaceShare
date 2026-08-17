
using FriendlySeat.Application.Services;
using FriendlySeat.Application.Common;
using FriendlySeat.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers.Admin;

public class AdminUsersController : AdminControllerBase
{
    private readonly AdminManageService _manage;
    private readonly AdminUserManagementService _users;
    private readonly ICurrentAdmin _currentAdmin;

    public AdminUsersController(AdminManageService manage, AdminUserManagementService users, ICurrentAdmin currentAdmin)
    {
        _manage = manage;
        _users = users;
        _currentAdmin = currentAdmin;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminUserDto>>> GetAdmins(CancellationToken ct)
        => Ok(await _manage.GetUsersAsync(ct));

    [HttpPost]
    public async Task<ActionResult<AdminUserDto>> CreateAdmin([FromBody] AdminUserCreateRequest request, CancellationToken ct)
        => Ok(await _manage.CreateUserAsync(request, _currentAdmin.AdminId!.Value, ct));

    [HttpPost("{id:long}/status")]
    public async Task<IActionResult> SetAdminStatus(long id, [FromQuery] string status, CancellationToken ct)
    {
        if (!Enum.TryParse<EntityStatus>(status, true, out var parsed))
            return BadRequest(new { message = "状态无效" });
        await _manage.SetStatusAsync(id, parsed, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpPost("{id:long}/password")]
    public async Task<IActionResult> ResetPassword(long id, [FromBody] dynamic body, CancellationToken ct)
    {
        string? pwd = body?.newPassword;
        await _manage.ResetPasswordAsync(id, pwd ?? string.Empty, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<AdminUserListDto>>> GetAllUsers([FromQuery] string? keyword, CancellationToken ct)
        => Ok(await _users.GetUsersAsync(keyword, ct));

    [HttpGet("detail/{id:long}")]
    public async Task<ActionResult<AdminUserDetailDto>> GetUser(long id, CancellationToken ct)
    {
        var user = await _users.GetUserAsync(id, ct);
        if (user is null) return NotFound();
        return Ok(user);
    }

    [HttpPost("detail/{id:long}/status")]
    public async Task<IActionResult> SetUserStatus(long id, [FromQuery] string status, CancellationToken ct)
    {
        if (!Enum.TryParse<UserStatus>(status, true, out var parsed))
            return BadRequest(new { message = "状态无效" });
        await _users.SetStatusAsync(id, parsed, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpPost("detail/{id:long}/credit")]
    public async Task<IActionResult> AdjustCredit(long id, [FromQuery] int change, [FromQuery] string? reason, CancellationToken ct)
    {
        await _users.AdjustCreditAsync(id, change, reason ?? "管理员调整", _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpPost("detail/{id:long}/risk")]
    public async Task<IActionResult> AdjustRisk(long id, [FromQuery] int change, [FromQuery] string? reason, CancellationToken ct)
    {
        await _users.AdjustRiskAsync(id, change, reason ?? "管理员调整", _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }
}
