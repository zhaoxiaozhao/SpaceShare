namespace FriendlySeat.Domain.Entities;

/// <summary>
/// 活动留言：用户在某活动下留的一句话（≤50 字），用于给活动营造氛围、相互鼓励。
/// 被举报后自动隐藏（Hidden）并进入后台审核。
/// </summary>
public class ActivityComment
{
    public long Id { get; set; }
    public long ActivityId { get; set; }
    public long UserId { get; set; }

    /// <summary>留言内容（≤50 字）</summary>
    public string Content { get; set; } = string.Empty;

    public CommentStatus Status { get; set; } = CommentStatus.Visible;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Activity? Activity { get; set; }
    public User? User { get; set; }
}
