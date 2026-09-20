namespace FriendlySeat.Application.Dtos;

public class VenuePostDto
{
    public long Id { get; set; }
    public long VenueId { get; set; }
    public string? VenueName { get; set; }
    public string Category { get; set; } = string.Empty;
    public string CategoryLabel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerAvatar { get; set; }
    public bool IsOwner { get; set; }
    public bool IsPinned { get; set; }
    public bool Liked { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public string Status { get; set; } = "Visible";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class VenuePostCommentDto
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerAvatar { get; set; }
    public bool IsOwner { get; set; }
    public string Status { get; set; } = "Visible";
    public DateTime CreatedAt { get; set; }
}

public class VenuePostDetailDto
{
    public VenuePostDto Post { get; set; } = new();
    public List<VenuePostCommentDto> Comments { get; set; } = new();
}

public class CreateVenuePostRequest
{
    public long VenueId { get; set; }
    public string Category { get; set; } = "chat";
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class CreateVenuePostCommentRequest
{
    public string Content { get; set; } = string.Empty;
}

public class AdminVenuePostReviewRequest
{
    /// <summary>true=通过（恢复展示）；false=驳回（删除）</summary>
    public bool Approve { get; set; }
}

public class AdminVenuePostPinRequest
{
    public bool Pinned { get; set; }
}
