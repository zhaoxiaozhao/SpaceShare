using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/venue-posts")]
[Authorize]
public class VenuePostsController : ControllerBase
{
    private readonly VenuePostService _posts;
    private readonly ICurrentUser _currentUser;

    public VenuePostsController(VenuePostService posts, ICurrentUser currentUser)
    {
        _posts = posts;
        _currentUser = currentUser;
    }

    /// <summary>场馆交流帖列表（匿名可看，支持 beforeId 游标分页）</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<VenuePostDto>>> List(
        [FromQuery] long venueId, [FromQuery] string? category, [FromQuery] string? sort,
        [FromQuery] int take, [FromQuery] long? beforeId, CancellationToken ct)
        => Ok(await _posts.GetListAsync(venueId, category, sort, _currentUser.UserId, take <= 0 ? 30 : take, beforeId, ct));

    /// <summary>我发布的帖子（含被隐藏的）</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<VenuePostDto>>> Mine(CancellationToken ct)
        => Ok(await _posts.GetMineAsync(_currentUser.UserId!.Value, ct));

    /// <summary>帖子详情 + 评论（匿名可看）</summary>
    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<VenuePostDetailDto>> Detail(long id, [FromQuery] bool countView, CancellationToken ct)
    {
        var dto = await _posts.GetDetailAsync(id, _currentUser.UserId, countView, ct);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<VenuePostDto>> Create([FromBody] CreateVenuePostRequest request, CancellationToken ct)
        => Ok(await _posts.CreateAsync(_currentUser.UserId!.Value, request, ct));

    /// <summary>编辑帖子（仅作者）</summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<VenuePostDto>> Update(long id, [FromBody] UpdateVenuePostRequest request, CancellationToken ct)
        => Ok(await _posts.UpdateAsync(_currentUser.UserId!.Value, id, request, ct));

    [HttpPost("{id:long}/like")]
    public async Task<ActionResult<VenuePostDto>> Like(long id, CancellationToken ct)
        => Ok(await _posts.ToggleLikeAsync(_currentUser.UserId!.Value, id, ct));

    [HttpPost("{id:long}/comments")]
    public async Task<ActionResult<VenuePostCommentDto>> Comment(long id, [FromBody] CreateVenuePostCommentRequest request, CancellationToken ct)
        => Ok(await _posts.AddCommentAsync(_currentUser.UserId!.Value, id, request, ct));

    /// <summary>评论点赞</summary>
    [HttpPost("comments/{id:long}/like")]
    public async Task<ActionResult<VenuePostCommentDto>> CommentLike(long id, CancellationToken ct)
        => Ok(await _posts.ToggleCommentLikeAsync(_currentUser.UserId!.Value, id, ct));

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _posts.DeletePostAsync(_currentUser.UserId!.Value, id, ct);
        return Ok();
    }

    [HttpDelete("comments/{id:long}")]
    public async Task<IActionResult> DeleteComment(long id, CancellationToken ct)
    {
        await _posts.DeleteCommentAsync(_currentUser.UserId!.Value, id, ct);
        return Ok();
    }
}
