using FriendlySeat.Application.Common;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/sessions")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly SeatSessionService _sessions;
    private readonly ICurrentUser _currentUser;

    public SessionsController(SeatSessionService sessions, ICurrentUser currentUser)
    {
        _sessions = sessions;
        _currentUser = currentUser;
    }

    [HttpPost("check-in")]
    public async Task<ActionResult<CheckInResult>> CheckIn([FromBody] CheckInRequest request, CancellationToken ct)
    {
        return Ok(await _sessions.CheckInAsync(_currentUser.UserId!.Value, request, ct));
    }

    [HttpGet("my")]
    public async Task<ActionResult<CheckInResult?>> GetMy(CancellationToken ct)
    {
        var session = await _sessions.GetMySessionAsync(_currentUser.UserId!.Value, ct);
        if (session is null) return Ok(null);
        return Ok(session);
    }

    [HttpPost("end")]
    public async Task<IActionResult> End(CancellationToken ct)
    {
        await _sessions.EndSessionAsync(_currentUser.UserId!.Value, ct);
        return Ok();
    }
}
