namespace FriendlySeat.Domain.Entities;

/// <summary>活动报名记录</summary>
public class ActivitySignup
{
    public long Id { get; set; }
    public long ActivityId { get; set; }
    public long UserId { get; set; }

    public ActivitySignupStatus Status { get; set; } = ActivitySignupStatus.Joined;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Activity? Activity { get; set; }
    public User? User { get; set; }
}
