using FriendlySeat.Application.Services.Jobs;
using Microsoft.Extensions.Configuration;

namespace FriendlySeat.Api.HostedServices;

public class AutoReleaseWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AutoReleaseWorker> _logger;
    private readonly TimeSpan _interval;

    public AutoReleaseWorker(IServiceProvider services, ILogger<AutoReleaseWorker> logger, IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        // 定时任务间隔（分钟），默认 5；可通过配置 Jobs:AutoReleaseIntervalMinutes 调整
        var minutes = configuration.GetValue<int?>("Jobs:AutoReleaseIntervalMinutes") ?? 5;
        _interval = TimeSpan.FromMinutes(Math.Max(1, minutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("自动释放任务已启动（内嵌于 API，间隔 {Minutes} 分钟）", _interval.TotalMinutes);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<IAutoReleaseJob>();
                await job.RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动释放任务执行失败");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
