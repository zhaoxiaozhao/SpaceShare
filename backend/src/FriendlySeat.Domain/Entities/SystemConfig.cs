namespace FriendlySeat.Domain.Entities;

public class SystemConfig
{
    public long Id { get; set; }
    public ConfigCategory Category { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
