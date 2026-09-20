using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers.Admin;

public class AdminActivityCommentController : AdminControllerBase
{
    private readonly ActivityCommentService _comments;
    private readonly ICurrentAdmin _currentAdmin;

    public AdminActivityCommentController(ActivityCommentService comments, ICurrentAdmin currentAdmin)
    {
        _comments = comments;
        _currentAdmin = currentAdmin;
    }

    [HttpGet("activity-comments")]
    public async Task<ActionResult<List<ActivityCommentDto>>> List([FromQuery] string? status, CancellationToken ct)
        => Ok(await _comments.AdminListAsync(status, ct));

    /// <summary>审核：通过=恢复展示；驳回=删除</summary>
    [HttpPost("activity-comments/{id:long}/review")]
    public async Task<IActionResult> Review(long id, AdminCommentReviewRequest request, CancellationToken ct)
    {
        await _comments.AdminReviewAsync(id, request.Approve, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }
}
