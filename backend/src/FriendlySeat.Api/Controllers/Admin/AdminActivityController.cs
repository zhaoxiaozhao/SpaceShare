using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers.Admin;

public class AdminActivityController : AdminControllerBase
{
    private readonly ActivityService _activities;
    private readonly ICurrentAdmin _currentAdmin;

    public AdminActivityController(ActivityService activities, ICurrentAdmin currentAdmin)
    {
        _activities = activities;
        _currentAdmin = currentAdmin;
    }

    [HttpGet("activities")]
    public async Task<ActionResult<List<ActivityDto>>> List([FromQuery] string? status, CancellationToken ct)
        => Ok(await _activities.AdminListAsync(status, ct));

    /// <summary>审核：通过 / 驳回</summary>
    [HttpPost("activities/{id:long}/review")]
    public async Task<IActionResult> Review(long id, ActivityReviewRequest request, CancellationToken ct)
    {
        await _activities.AdminReviewAsync(id, request.Approve, request.Remark, ct);
        return Ok();
    }

    /// <summary>下架</summary>
    [HttpPost("activities/{id:long}/take-down")]
    public async Task<IActionResult> TakeDown(long id, CancellationToken ct)
    {
        await _activities.AdminTakeDownAsync(id, ct);
        return Ok();
    }
}
