using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class SwapController : ControllerBase
{
    private readonly SwapService _swap;
    private readonly ICurrentUser _currentUser;

    public SwapController(SwapService swap, ICurrentUser currentUser)
    {
        _swap = swap;
        _currentUser = currentUser;
    }

    /// <summary>发布换座意向</summary>
    [HttpPost("swaps")]
    public async Task<ActionResult<SeatSwapDto>> Create(SeatSwapCreateRequest request, CancellationToken ct)
    {
        return Ok(await _swap.CreateAsync(_currentUser.UserId!.Value, request, ct));
    }

    /// <summary>某场馆的换座广场（进行中的意向）</summary>
    [HttpGet("swaps")]
    public async Task<ActionResult<List<SeatSwapDto>>> GetOpen([FromQuery] long venueId, CancellationToken ct)
    {
        return Ok(await _swap.GetOpenAsync(venueId, _currentUser.UserId!.Value, ct));
    }

    /// <summary>最近换座意向（跨场馆，首页展示）</summary>
    [HttpGet("swaps/recent")]
    public async Task<ActionResult<List<SeatSwapDto>>> GetRecent([FromQuery] int take, CancellationToken ct)
    {
        return Ok(await _swap.GetRecentAsync(_currentUser.UserId!.Value, take <= 0 ? 20 : take, ct));
    }

    /// <summary>某座位当前进行中的换座意向（座位详情页判断按钮）</summary>
    [HttpGet("swaps/seat/{seatId:long}")]
    public async Task<ActionResult<SeatSwapDto?>> GetBySeat(long seatId, CancellationToken ct)
    {
        return Ok(await _swap.GetBySeatAsync(seatId, _currentUser.UserId!.Value, ct));
    }

    /// <summary>我发布的换座意向（含响应列表）</summary>
    [HttpGet("swaps/mine")]
    public async Task<ActionResult<List<SeatSwapDto>>> GetMine(CancellationToken ct)
    {
        return Ok(await _swap.GetMineAsync(_currentUser.UserId!.Value, ct));
    }

    /// <summary>我响应过的换座意向</summary>
    [HttpGet("swaps/responded")]
    public async Task<ActionResult<List<SeatSwapDto>>> GetResponded(CancellationToken ct)
    {
        return Ok(await _swap.GetRespondedAsync(_currentUser.UserId!.Value, ct));
    }

    /// <summary>响应换座（提交自己的位置）</summary>
    [HttpPost("swaps/{id:long}/respond")]
    public async Task<ActionResult<SeatSwapDto>> Respond(long id, SeatSwapRespondRequest request, CancellationToken ct)
    {
        return Ok(await _swap.RespondAsync(id, _currentUser.UserId!.Value, request, ct));
    }

    /// <summary>发布者确认某条响应</summary>
    [HttpPost("swaps/{id:long}/accept/{responseId:long}")]
    public async Task<ActionResult<SeatSwapDto>> Accept(long id, long responseId, CancellationToken ct)
    {
        return Ok(await _swap.AcceptAsync(id, _currentUser.UserId!.Value, responseId, ct));
    }

    /// <summary>取消我发布的换座意向</summary>
    [HttpPost("swaps/{id:long}/cancel")]
    public async Task<ActionResult> Cancel(long id, CancellationToken ct)
    {
        await _swap.CancelAsync(id, _currentUser.UserId!.Value, ct);
        return Ok();
    }
}
