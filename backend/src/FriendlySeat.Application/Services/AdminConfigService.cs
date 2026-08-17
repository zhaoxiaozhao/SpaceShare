using FriendlySeat.Application.Common;
using FriendlySeat.Application.Services;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class ConfigItemDto
{
    public long Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
}

public class AdminConfigService
{
    private readonly IAppDbContext _db;
    private readonly ConfigService _configService;
    private readonly IAuditService _audit;

    public AdminConfigService(IAppDbContext db, ConfigService configService, IAuditService audit)
    {
        _db = db;
        _configService = configService;
        _audit = audit;
    }

    public async Task<List<ConfigItemDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.SystemConfigs
            .OrderBy(c => c.Category).ThenBy(c => c.ConfigKey)
            .Select(c => new ConfigItemDto
            {
                Id = c.Id,
                Category = c.Category.ToString(),
                Key = c.ConfigKey,
                Value = c.Value,
                Description = c.Description
            })
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(long id, string? value, long operatorId, CancellationToken ct = default)
    {
        var config = await _db.SystemConfigs.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw AppException.NotFound("配置不存在");

        var old = config.Value;
        config.Value = value;
        config.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        if (Enum.TryParse<ConfigCategory>(config.Category.ToString(), out var category))
        {
            await _configService.SetValueAsync(category, config.ConfigKey, value, ct);
        }

        await _audit.LogAsync(operatorId, "config.update", "SystemConfig", id.ToString(), $"更新配置 {config.ConfigKey}: {old} → {value}", null, ct);
    }
}
