using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

/// <summary>用户动态（社区新鲜事）：交流板「动态」标签的数据源</summary>
[ApiController]
[Route("api/v1/moments")]
[Authorize]
public class MomentsController : ControllerBase
{
    private readonly MomentService _moments;
    private readonly ICurrentUser _currentUser;

    public MomentsController(MomentService moments, ICurrentUser currentUser)
    {
        _moments = moments;
        _currentUser = currentUser;
    }

    /// <summary>动态列表（匿名可看，支持 beforeId 游标分页；venueId=0 按全部场馆）</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<UserMomentDto>>> List(
        [FromQuery] long? venueId, [FromQuery] int take, [FromQuery] long? beforeId, CancellationToken ct)
        => Ok(await _moments.GetListAsync(venueId, _currentUser.UserId, take <= 0 ? 20 : take, beforeId, ct));

    /// <summary>删除自己的动态</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var ok = await _moments.DeleteAsync(_currentUser.UserId!.Value, id, ct);
        return ok ? Ok() : NotFound();
    }
}