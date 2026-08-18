namespace FriendlySeat.Domain.Entities;

public class StudyAchievement
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
