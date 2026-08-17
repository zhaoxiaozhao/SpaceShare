using FriendlySeat.Application.Common;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FriendlySeat.Application.Services;

public record ReservationRuleConfig(
    int MinMinutes = 30,
    int MaxMinutes = 240,
    int MaxAdvanceHours = 24,
    int MaxActiveReservations = 1,
    int DailyReservationLimit = 5,
    int ArrivalGraceMinutes = 30,
    int ArrivalWarningMinutes = 15,
    int WaitlistWindowMinutes = 10);

public record CreditRuleConfig(
    int ArrivalBonus = 1,
    int CompletionBonus = 1,
    int NoShowPenalty = -5,
    int FakeSeatPenalty = -10,
    int MaliciousHoldPenalty = -10,
    int TransactionPenalty = -20,
    int MaliciousReportPenalty = -10,
    int MaxScore = 100);

public class ConfigService
{
    private readonly IAppDbContext _db;
    private readonly IRedisCache _cache;
    private readonly ILogger<ConfigService> _logger;

    public ConfigService(IAppDbContext db, IRedisCache cache, ILogger<ConfigService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetValueAsync(ConfigCategory category, string key, CancellationToken ct = default)
    {
        var cacheKey = $"config:{category}:{key}";
        var cached = await _cache.GetAsync<string?>(cacheKey, ct);
        if (cached is not null) return cached;

        var row = await _db.SystemConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Category == category && c.ConfigKey == key, ct);

        var value = row?.Value;
        if (value is not null)
        {
            await _cache.SetAsync(cacheKey, value, TimeSpan.FromMinutes(5), ct);
        }
        return value;
    }

    public async Task<int> GetIntAsync(ConfigCategory category, string key, int defaultValue, CancellationToken ct = default)
    {
        var v = await GetValueAsync(category, key, ct);
        return int.TryParse(v, out var parsed) ? parsed : defaultValue;
    }

    public async Task<ReservationRuleConfig> GetReservationRulesAsync(CancellationToken ct = default)
    {
        var c = await _db.SystemConfigs
            .Where(x => x.Category == ConfigCategory.ReservationRules)
            .AsNoTracking()
            .ToDictionaryAsync(x => x.ConfigKey, x => x.Value, ct);

        int I(string k, int d) => int.TryParse(c.GetValueOrDefault(k), out var v) ? v : d;

        return new ReservationRuleConfig(
            MinMinutes: I("min_minutes", 30),
            MaxMinutes: I("max_minutes", 240),
            MaxAdvanceHours: I("max_advance_hours", 24),
            MaxActiveReservations: I("max_active_reservations", 1),
            DailyReservationLimit: I("daily_reservation_limit", 5),
            ArrivalGraceMinutes: I("arrival_grace_minutes", 30),
            ArrivalWarningMinutes: I("arrival_warning_minutes", 15),
            WaitlistWindowMinutes: I("waitlist_window_minutes", 10));
    }

    public async Task<CreditRuleConfig> GetCreditRulesAsync(CancellationToken ct = default)
    {
        var c = await _db.SystemConfigs
            .Where(x => x.Category == ConfigCategory.CreditRules)
            .AsNoTracking()
            .ToDictionaryAsync(x => x.ConfigKey, x => x.Value, ct);

        int I(string k, int d) => int.TryParse(c.GetValueOrDefault(k), out var v) ? v : d;

        return new CreditRuleConfig(
            ArrivalBonus: I("arrival_bonus", 1),
            CompletionBonus: I("completion_bonus", 1),
            NoShowPenalty: I("no_show_penalty", -5),
            FakeSeatPenalty: I("fake_seat_penalty", -10),
            MaliciousHoldPenalty: I("malicious_hold_penalty", -10),
            TransactionPenalty: I("transaction_penalty", -20),
            MaliciousReportPenalty: I("malicious_report_penalty", -10),
            MaxScore: I("max_score", 100));
    }

    public async Task SetValueAsync(ConfigCategory category, string key, string? value, CancellationToken ct = default)
    {
        var row = await _db.SystemConfigs
            .FirstOrDefaultAsync(c => c.Category == category && c.ConfigKey == key, ct);

        if (row is null)
        {
            _db.SystemConfigs.Add(new SystemConfig
            {
                Category = category,
                ConfigKey = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            row.Value = value;
            row.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"config:{category}:{key}", ct);
    }

    public static string CreditLevel(int score) => score switch
    {
        >= 90 => "优秀",
        >= 70 => "正常",
        >= 50 => "观察",
        >= 30 => "限制",
        _ => "高风险"
    };

    public static string RiskLevel(int score) => score switch
    {
        <= 30 => "正常",
        <= 60 => "观察",
        <= 80 => "限制",
        _ => "高风险"
    };
}
