namespace FriendlySeat.Application.Dtos;

public class ActivityCommentDto
{
    public long Id { get; set; }
    public long ActivityId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerAvatar { get; set; }

    /// <summary>是否当前查看者本人（用于显示删除）</summary>
    public bool IsOwner { get; set; }

    public string Status { get; set; } = "Visible";

    /// <summary>管理端展示：活动标题</summary>
    public string? ActivityTitle { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class CreateActivityCommentRequest
{
    public long ActivityId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class AdminCommentReviewRequest
{
    /// <summary>true=审核通过（恢复展示）；false=驳回（删除）</summary>
    public bool Approve { get; set; }
}
