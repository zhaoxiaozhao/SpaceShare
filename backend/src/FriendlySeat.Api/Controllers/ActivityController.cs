using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class ActivityController : ControllerBase
{
    private readonly ActivityService _activities;
    private readonly ICurrentUser _currentUser;

    public ActivityController(ActivityService activities, ICurrentUser currentUser)
    {
        _activities = activities;
        _currentUser = currentUser;
    }

    /// <summary>活动列表（已发布、未结束）</summary>
    [HttpGet("activities")]
    public async Task<ActionResult<List<ActivityDto>>> List([FromQuery] string? category, CancellationToken ct)
    {
        return Ok(await _activities.GetListAsync(_currentUser.UserId!.Value, category, ct));
    }

    /// <summary>我发布的活动</summary>
    [HttpGet("activities/mine")]
    public async Task<ActionResult<List<ActivityDto>>> Mine(CancellationToken ct)
    {
        return Ok(await _activities.GetMineAsync(_currentUser.UserId!.Value, ct));
    }

    /// <summary>我报名的活动</summary>
    [HttpGet("activities/joined")]
    public async Task<ActionResult<List<ActivityDto>>> Joined(CancellationToken ct)
    {
        return Ok(await _activities.GetJoinedAsync(_currentUser.UserId!.Value, ct));
    }

    /// <summary>活动详情</summary>
    [HttpGet("activities/{id:long}")]
    public async Task<ActionResult<ActivityDto>> Detail(long id, CancellationToken ct)
    {
        var dto = await _activities.GetDetailAsync(id, _currentUser.UserId!.Value, ct);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    /// <summary>发布活动</summary>
    [HttpPost("activities")]
    public async Task<ActionResult<ActivityDto>> Create(ActivityCreateRequest request, CancellationToken ct)
    {
        return Ok(await _activities.CreateAsync(_currentUser.UserId!.Value, request, ct));
    }

    /// <summary>编辑活动</summary>
    [HttpPut("activities/{id:long}")]
    public async Task<ActionResult<ActivityDto>> Update(long id, ActivityCreateRequest request, CancellationToken ct)
    {
        return Ok(await _activities.UpdateAsync(id, _currentUser.UserId!.Value, request, ct));
    }

    /// <summary>取消活动</summary>
    [HttpPost("activities/{id:long}/cancel")]
    public async Task<ActionResult> Cancel(long id, CancellationToken ct)
    {
        await _activities.CancelAsync(id, _currentUser.UserId!.Value, ct);
        return Ok();
    }

    /// <summary>报名</summary>
    [HttpPost("activities/{id:long}/signup")]
    public async Task<ActionResult> Signup(long id, CancellationToken ct)
    {
        await _activities.SignupAsync(id, _currentUser.UserId!.Value, ct);
        return Ok();
    }

    /// <summary>取消报名</summary>
    [HttpDelete("activities/{id:long}/signup")]
    public async Task<ActionResult> CancelSignup(long id, CancellationToken ct)
    {
        await _activities.CancelSignupAsync(id, _currentUser.UserId!.Value, ct);
        return Ok();
    }
}
