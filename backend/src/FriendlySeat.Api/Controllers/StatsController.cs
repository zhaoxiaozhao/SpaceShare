using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/stats")]
public class StatsController : ControllerBase
{
    private readonly PublicStatsService _stats;

    public StatsController(PublicStatsService stats)
    {
        _stats = stats;
    }

    /// <summary>实时在线（正在学习/阅读）人数（匿名可看，仅聚合数字）</summary>
    [HttpGet("live")]
    [AllowAnonymous]
    public async Task<ActionResult<LiveStatsDto>> Live(CancellationToken ct)
        => Ok(await _stats.GetLiveAsync(ct));
}
