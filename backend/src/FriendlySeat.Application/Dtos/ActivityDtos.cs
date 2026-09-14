namespace FriendlySeat.Application.Dtos;

public class ActivityCreateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "other";
    public long? VenueId { get; set; }
    public string? LocationText { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public DateTime? SignupDeadline { get; set; }
    public int Capacity { get; set; } = 20;
    public string Description { get; set; } = string.Empty;

    /// <summary>活动海报（微信云存储 fileID）</summary>
    public string? CoverImage { get; set; }
}

public class ActivityReviewRequest
{
    public bool Approve { get; set; }
    public string? Remark { get; set; }
}

public class ActivitySignupDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string UserNickname { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ActivityDto
{
    public long Id { get; set; }
    public long CreatorUserId { get; set; }
    public string CreatorNickname { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "other";
    public long? VenueId { get; set; }
    public string? VenueName { get; set; }
    public string? LocationText { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public DateTime? SignupDeadline { get; set; }
    public int Capacity { get; set; }
    public int SignupCount { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>活动海报（微信云存储 fileID）</summary>
    public string? CoverImage { get; set; }

    public string Status { get; set; } = string.Empty;
    public string? ReviewRemark { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsMine { get; set; }
    public bool IsSignedUp { get; set; }
    public bool IsFull { get; set; }
    public bool SignupOpen { get; set; }

    /// <summary>报名名单：仅创建者或管理端返回</summary>
    public List<ActivitySignupDto> Signups { get; set; } = new();
}
