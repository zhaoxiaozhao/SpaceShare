namespace FriendlySeat.Domain.Entities;

public class StudyGoal
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public GoalPeriod Period { get; set; } = GoalPeriod.Daily;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TargetMinutes { get; set; }
    public int AchievedMinutes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
