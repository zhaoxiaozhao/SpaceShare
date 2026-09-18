
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

    /// <summary>学习/阅读 聚合概览（含近 N 日趋势）</summary>
    [HttpGet("stats/learning")]
    public async Task<ActionResult<LearningOverviewDto>> Learning(CancellationToken ct, [FromQuery] int days = 7)
        => Ok(await _stats.GetLearningOverviewAsync(days, ct));

    /// <summary>学习/阅读 用户维度列表</summary>
    [HttpGet("stats/learning/users")]
    public async Task<ActionResult<List<LearningUserDto>>> LearningUsers(CancellationToken ct, [FromQuery] int take = 100)
        => Ok(await _stats.GetLearningUsersAsync(take, ct));
}
