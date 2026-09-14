namespace FriendlySeat.Domain.Entities;

/// <summary>
/// 线下活动：用户可在小程序发布，需经管理端审核后展示；名额制报名。
/// </summary>
public class Activity
{
    public long Id { get; set; }
    public long CreatorUserId { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>分类 code：reading/lecture/exhibition/study/other</summary>
    public string Category { get; set; } = "other";

    /// <summary>关联场馆（可空）</summary>
    public long? VenueId { get; set; }

    /// <summary>具体地点（可空，如 "3F-A区"）</summary>
    public string? LocationText { get; set; }

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }

    /// <summary>报名截止时间（null = 活动开始前均可）</summary>
    public DateTime? SignupDeadline { get; set; }

    /// <summary>报名名额</summary>
    public int Capacity { get; set; }

    public string Description { get; set; } = string.Empty;

    public ActivityStatus Status { get; set; } = ActivityStatus.PendingReview;

    /// <summary>审核备注（驳回原因等）</summary>
    public string? ReviewRemark { get; set; }

    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? Creator { get; set; }
    public Venue? Venue { get; set; }
    public ICollection<ActivitySignup> Signups { get; set; } = new List<ActivitySignup>();
}
