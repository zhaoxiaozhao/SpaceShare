namespace FriendlySeat.Domain.Entities;

public class RiskEvent
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
