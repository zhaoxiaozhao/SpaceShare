using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers.Admin;

public class AdminSeatNoteController : AdminControllerBase
{
    private readonly SeatNoteService _notes;
    private readonly ICurrentAdmin _currentAdmin;

    public AdminSeatNoteController(SeatNoteService notes, ICurrentAdmin currentAdmin)
    {
        _notes = notes;
        _currentAdmin = currentAdmin;
    }

    [HttpGet("seat-notes")]
    public async Task<ActionResult<List<SeatNoteDto>>> List([FromQuery] string? status, CancellationToken ct)
        => Ok(await _notes.AdminListAsync(status, ct));

    /// <summary>审核：通过=恢复展示；驳回=删除</summary>
    [HttpPost("seat-notes/{id:long}/review")]
    public async Task<IActionResult> Review(long id, AdminSeatNoteReviewRequest request, CancellationToken ct)
    {
        await _notes.AdminReviewAsync(id, request.Approve, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }
}
