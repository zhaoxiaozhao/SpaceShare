using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/activity-comments")]
[Authorize]
public class ActivityCommentsController : ControllerBase
{
    private readonly ActivityCommentService _comments;
    private readonly ICurrentUser _currentUser;

    public ActivityCommentsController(ActivityCommentService comments, ICurrentUser currentUser)
    {
        _comments = comments;
        _currentUser = currentUser;
    }

    /// <summary>某活动的留言列表（匿名可看）</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ActivityCommentDto>>> List([FromQuery] long activityId, CancellationToken ct)
        => Ok(await _comments.GetByActivityAsync(activityId, _currentUser.UserId, ct: ct));

    /// <summary>发表留言</summary>
    [HttpPost]
    public async Task<ActionResult<ActivityCommentDto>> Create([FromBody] CreateActivityCommentRequest request, CancellationToken ct)
        => Ok(await _comments.CreateAsync(_currentUser.UserId!.Value, request, ct));

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _comments.DeleteAsync(_currentUser.UserId!.Value, id, ct);
        return Ok();
    }
}
