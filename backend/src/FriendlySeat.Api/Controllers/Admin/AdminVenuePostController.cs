using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers.Admin;

public class AdminVenuePostController : AdminControllerBase
{
    private readonly VenuePostService _posts;
    private readonly ICurrentAdmin _currentAdmin;

    public AdminVenuePostController(VenuePostService posts, ICurrentAdmin currentAdmin)
    {
        _posts = posts;
        _currentAdmin = currentAdmin;
    }

    [HttpGet("venue-posts")]
    public async Task<ActionResult<List<VenuePostDto>>> List([FromQuery] string? status, CancellationToken ct)
        => Ok(await _posts.AdminListAsync(status, ct));

    /// <summary>审核：通过=恢复展示；驳回=删除</summary>
    [HttpPost("venue-posts/{id:long}/review")]
    public async Task<IActionResult> Review(long id, AdminVenuePostReviewRequest request, CancellationToken ct)
    {
        await _posts.AdminReviewAsync(id, request.Approve, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    /// <summary>置顶 / 取消置顶</summary>
    [HttpPost("venue-posts/{id:long}/pin")]
    public async Task<IActionResult> Pin(long id, AdminVenuePostPinRequest request, CancellationToken ct)
    {
        await _posts.AdminPinAsync(id, request.Pinned, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpGet("venue-post-comments")]
    public async Task<ActionResult<List<VenuePostCommentDto>>> CommentList([FromQuery] string? status, CancellationToken ct)
        => Ok(await _posts.AdminCommentListAsync(status, ct));

    [HttpPost("venue-post-comments/{id:long}/review")]
    public async Task<IActionResult> CommentReview(long id, AdminVenuePostReviewRequest request, CancellationToken ct)
    {
        await _posts.AdminCommentReviewAsync(id, request.Approve, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }
}
