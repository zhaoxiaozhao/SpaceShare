
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers.Admin;

public class AdminStatsController : AdminControllerBase
{
    private readonly AdminStatsService _stats;

    public AdminStatsController(AdminStatsService stats)
    {
        _stats = stats;
    }

    [HttpGet("stats/overview")]
    public async Task<ActionResult<StatsOverviewDto>> Overview(CancellationToken ct)
        => Ok(await _stats.GetOverviewAsync(ct));

    [HttpGet("stats/trend")]
    public async Task<ActionResult<DailyTrendDto>> Trend(CancellationToken ct, [FromQuery] int days = 14)
        => Ok(await _stats.GetDailyTrendAsync(days, ct));
}
