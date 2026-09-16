using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/seat-notes")]
[Authorize]
public class SeatNotesController : ControllerBase
{
    private readonly SeatNoteService _notes;
    private readonly ICurrentUser _currentUser;

    public SeatNotesController(SeatNoteService notes, ICurrentUser currentUser)
    {
        _notes = notes;
        _currentUser = currentUser;
    }

    /// <summary>某座位的便签列表（匿名可看）</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<SeatNoteDto>>> List([FromQuery] long seatId, CancellationToken ct)
        => Ok(await _notes.GetBySeatAsync(seatId, _currentUser.UserId, ct: ct));

    /// <summary>发布/更新自己的便签（每人每座位一条）</summary>
    [HttpPost]
    public async Task<ActionResult<SeatNoteDto>> Create([FromBody] CreateSeatNoteRequest request, CancellationToken ct)
        => Ok(await _notes.CreateOrUpdateAsync(_currentUser.UserId!.Value, request, ct));

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _notes.DeleteAsync(_currentUser.UserId!.Value, id, ct);
        return Ok();
    }
}
