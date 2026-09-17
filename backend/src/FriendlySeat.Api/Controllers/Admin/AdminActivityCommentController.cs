using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers.Admin;

public class AdminActivityCommentController : AdminControllerBase
{
    private readonly ActivityCommentService _comments;

    public AdminActivityCommentController(ActivityCommentService comments)
    {
        _comments = comments;
    }

    [HttpGet("activity-comments")]
    public async Task<ActionResult<List<ActivityCommentDto>>> List([FromQuery] string? status, CancellationToken ct)
        => Ok(await _comments.AdminListAsync(status, ct));

    /// <summary>审核：通过=恢复展示；驳回=删除</summary>
    [HttpPost("activity-comments/{id:long}/review")]
    public async Task<IActionResult> Review(long id, AdminCommentReviewRequest request, CancellationToken ct)
    {
        await _comments.AdminReviewAsync(id, request.Approve, ct);
        return Ok();
    }
}
