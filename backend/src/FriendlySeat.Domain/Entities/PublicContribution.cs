namespace FriendlySeat.Domain.Entities;

public class PublicContribution
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public int ShareCount { get; set; }
    public double ShareHours { get; set; }
    public int HelpedCount { get; set; }
    public int OnTimeCount { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
