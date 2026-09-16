using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers.Admin;

public class AdminSeatNoteController : AdminControllerBase
{
    private readonly SeatNoteService _notes;

    public AdminSeatNoteController(SeatNoteService notes)
    {
        _notes = notes;
    }

    [HttpGet("seat-notes")]
    public async Task<ActionResult<List<SeatNoteDto>>> List([FromQuery] string? status, CancellationToken ct)
        => Ok(await _notes.AdminListAsync(status, ct));

    /// <summary>审核：通过=恢复展示；驳回=删除</summary>
    [HttpPost("seat-notes/{id:long}/review")]
    public async Task<IActionResult> Review(long id, AdminSeatNoteReviewRequest request, CancellationToken ct)
    {
        await _notes.AdminReviewAsync(id, request.Approve, ct);
        return Ok();
    }
}
