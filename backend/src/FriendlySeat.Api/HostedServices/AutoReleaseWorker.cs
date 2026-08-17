using FriendlySeat.Application.Services.Jobs;

namespace FriendlySeat.Api.HostedServices;

public class AutoReleaseWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AutoReleaseWorker> _logger;

    public AutoReleaseWorker(IServiceProvider services, ILogger<AutoReleaseWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("自动释放任务已启动（内嵌于 API）");
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

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
