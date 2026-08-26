using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly UserService _users;
    private readonly ICurrentUser _currentUser;

    public MeController(UserService users, ICurrentUser currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<UserDto>> GetProfile(CancellationToken ct)
    {
        return Ok(await _users.GetProfileAsync(_currentUser.UserId!.Value, ct));
    }

    [HttpPost("update-profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UserProfileUpdateRequest request, CancellationToken ct)
    {
        return Ok(await _users.UpdateProfileAsync(_currentUser.UserId!.Value, request, ct));
    }

    /// <summary>注销账号（匿名化个人信息并禁用账号）</summary>
    [HttpPost("delete-account")]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        await _users.DeleteAccountAsync(_currentUser.UserId!.Value, ct);
        return Ok();
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications([FromQuery] bool? unread, CancellationToken ct)
    {
        return Ok(await _users.GetNotificationsAsync(_currentUser.UserId!.Value, unread, ct));
    }

    [HttpPost("notifications/read")]
    public async Task<IActionResult> MarkNotificationsRead(CancellationToken ct)
    {
        await _users.MarkNotificationsReadAsync(_currentUser.UserId!.Value, ct);
        return Ok();
    }

    [HttpGet("notifications/unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken ct)
    {
        return Ok(await _users.GetUnreadCountAsync(_currentUser.UserId!.Value, ct));
    }

    /// <summary>小程序订阅消息模板列表（key → 模板ID），用于 wx.requestSubscribeMessage 订阅</summary>
    [HttpGet("subscribe-templates")]
    public async Task<ActionResult<Dictionary<string, string>>> GetSubscribeTemplates(CancellationToken ct)
    {
        return Ok(await _users.GetSubscribeTemplatesAsync(ct));
    }
}
