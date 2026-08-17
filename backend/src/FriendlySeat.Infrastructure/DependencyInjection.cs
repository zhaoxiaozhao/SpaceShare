using FriendlySeat.Application.Common;
using FriendlySeat.Application.Services;
using FriendlySeat.Application.Services.Jobs;
using FriendlySeat.Infrastructure.Persistence;
using FriendlySeat.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FriendlySeat.Infrastructure;

public enum DbProvider
{
    Postgres,
    MySql
}

public class DatabaseOptions
{
    public DbProvider Provider { get; set; } = DbProvider.Postgres;
}

public static class DependencyInjection
{
    public static IServiceCollection AddFriendlySeatInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 数据库（支持 PostgreSQL / MySQL 切换）
        var dbOptions = configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=friendlyseat;Username=postgres;Password=postgres";

        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
        services.AddDbContext<FriendlySeatDbContext>(options =>
        {
            if (dbOptions.Provider == DbProvider.MySql)
            {
                options.UseMySQL(connectionString);
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        // Redis（支持禁用，用于本地无 Redis 环境降级）
        var redisOptions = configuration.GetSection("Redis").Get<RedisOptions>() ?? new RedisOptions();
        if (redisOptions.Enabled)
        {
            try
            {
                var multiplexer = ConnectionMultiplexer.Connect(redisOptions.ConnectionString);
                services.AddSingleton<IConnectionMultiplexer>(multiplexer);
                services.AddStackExchangeRedisCache(o =>
                {
                    o.Configuration = redisOptions.ConnectionString;
                    o.InstanceName = "FriendlySeat:";
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Redis 连接失败，降级为内存缓存: {ex.Message}");
                services.AddDistributedMemoryCache();
                services.AddSingleton<IConnectionMultiplexer?>(_ => null);
                redisOptions.Enabled = false;
                services.Configure<RedisOptions>(o => o.Enabled = false);
            }
        }
        else
        {
            services.AddDistributedMemoryCache();
            services.AddSingleton<IConnectionMultiplexer?>(_ => null);
        }

        services.Configure<RedisOptions>(configuration.GetSection("Redis"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<WechatOptions>(configuration.GetSection("Wechat"));

        // 基础设施服务
        services.AddSingleton<IDistributedLock, DistributedLockService>();
        services.AddScoped<IRedisCache, RedisCacheService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IWechatService, WechatService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddScoped<ICurrentAdmin, CurrentAdminService>();
        services.AddHttpContextAccessor();
        services.AddHttpClient<IWechatService, WechatService>();

        return services;
    }

    public static IServiceCollection AddFriendlySeatApplication(this IServiceCollection services)
    {
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<FriendlySeatDbContext>());

        services.AddScoped<ConfigService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<VenueService>();
        services.AddScoped<ShareService>();
        services.AddScoped<ReservationService>();
        services.AddScoped<WaitlistService>();
        services.AddScoped<CreditService>();
        services.AddScoped<ReportService>();
        services.AddScoped<DonationService>();
        services.AddScoped<AdService>();
        services.AddScoped<RiskService>();
        services.AddScoped<SeatSessionService>();

        // 管理端服务（整合进单体 API）
        services.AddScoped<AdminManageService>();
        services.AddScoped<AdminUserManagementService>();
        services.AddScoped<AdminVenueManagementService>();
        services.AddScoped<AdminReportService>();
        services.AddScoped<AdminConfigService>();
        services.AddScoped<AdminStatsService>();

        // 定时任务
        services.AddScoped<IAutoReleaseJob, AutoReleaseJob>();

        return services;
    }
}
