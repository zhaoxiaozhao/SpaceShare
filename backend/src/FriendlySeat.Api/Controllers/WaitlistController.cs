using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class WaitlistController : ControllerBase
{
    private readonly WaitlistService _waitlist;
    private readonly ICurrentUser _currentUser;

    public WaitlistController(WaitlistService waitlist, ICurrentUser currentUser)
    {
        _waitlist = waitlist;
        _currentUser = currentUser;
    }

    [HttpPost("shares/{id:long}/waitlist")]
    public async Task<ActionResult<WaitlistDto>> Join(long id, CancellationToken ct)
    {
        return Ok(await _waitlist.JoinAsync(_currentUser.UserId!.Value, id, ct));
    }

    [HttpGet("waitlist/my")]
    public async Task<ActionResult<List<WaitlistDto>>> GetMy(CancellationToken ct)
    {
        return Ok(await _waitlist.GetMyAsync(_currentUser.UserId!.Value, ct));
    }

    [HttpDelete("waitlist/{id:long}")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        await _waitlist.CancelAsync(id, _currentUser.UserId!.Value, ct);
        return Ok();
    }

    // ===== 范围候补偏好（提交后系统自动盯+自动代约） =====
    [HttpPost("waitlist/preferences")]
    public async Task<ActionResult<WaitlistPreferenceDto>> CreatePreference(WaitlistPreferenceRequest request, CancellationToken ct)
    {
        return Ok(await _waitlist.CreatePreferenceAsync(_currentUser.UserId!.Value, request, ct));
    }

    [HttpGet("waitlist/preferences/my")]
    public async Task<ActionResult<List<WaitlistPreferenceDto>>> GetMyPreferences(CancellationToken ct)
    {
        return Ok(await _waitlist.GetMyPreferencesAsync(_currentUser.UserId!.Value, ct));
    }

    [HttpDelete("waitlist/preferences/{id:long}")]
    public async Task<IActionResult> CancelPreference(long id, CancellationToken ct)
    {
        await _waitlist.CancelPreferenceAsync(id, _currentUser.UserId!.Value, ct);
        return Ok();
    }
}
