using FriendlySeat.Infrastructure;
using FriendlySeat.Infrastructure.Persistence;
using FriendlySeat.Worker.Jobs;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/worker-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.AddSerilog();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=friendlyseat;Username=postgres;Password=postgres";
builder.Services.AddDbContext<FriendlySeatDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddFriendlySeatInfrastructure(builder.Configuration);
builder.Services.AddFriendlySeatApplication();
builder.Services.AddScoped<IAutoReleaseJob, AutoReleaseJob>();

builder.Services.AddHostedService<AutoReleaseWorker>();

var host = builder.Build();

// 初始化数据库
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FriendlySeatDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbSeeder.SeedAsync(db, logger);
}

host.Run();

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
        _logger.LogInformation("自动释放 Worker 启动");
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
