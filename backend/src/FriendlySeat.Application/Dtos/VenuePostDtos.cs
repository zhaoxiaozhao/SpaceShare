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
    /// <summary>封面图（微信云存储 fileID）</summary>
    public string? CoverImage { get; set; }
    public long OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerAvatar { get; set; }
    public bool IsOwner { get; set; }
    public bool IsPinned { get; set; }
    public bool Liked { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int ViewCount { get; set; }
    public string Status { get; set; } = "Visible";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class VenuePostCommentDto
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public string Content { get; set; } = string.Empty;
    public long OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerAvatar { get; set; }
    public bool IsOwner { get; set; }
    public string Status { get; set; } = "Visible";
    public DateTime CreatedAt { get; set; }

    /// <summary>所属一级评论 Id（一级评论为 null，回复有值）</summary>
    public long? ParentCommentId { get; set; }

    /// <summary>被回复者 Id / 昵称（仅回复有值）</summary>
    public long? ReplyToUserId { get; set; }
    public string? ReplyToName { get; set; }

    /// <summary>评论配图（微信云存储 fileID）</summary>
    public string? ImageUrl { get; set; }

    /// <summary>点赞数 / 当前访问者是否已赞</summary>
    public int LikeCount { get; set; }
    public bool Liked { get; set; }

    /// <summary>回复列表（仅一级评论填充）</summary>
    public List<VenuePostCommentDto> Replies { get; set; } = new();
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

    /// <summary>封面图（微信云存储 fileID）</summary>
    public string? CoverImage { get; set; }

    /// <summary>封面图的临时可访问 https 地址，仅用于微信 imgSecCheck 校验</summary>
    public string? CoverImageUrl { get; set; }
}

public class UpdateVenuePostRequest
{
    public string Category { get; set; } = "chat";
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    /// <summary>封面图（微信云存储 fileID）</summary>
    public string? CoverImage { get; set; }

    /// <summary>封面图临时 https 地址，仅用于 imgSecCheck</summary>
    public string? CoverImageUrl { get; set; }
}

public class CreateVenuePostCommentRequest
{
    public string Content { get; set; } = string.Empty;

    /// <summary>回复的目标评论 Id（为空表示发表一级评论）</summary>
    public long? ParentCommentId { get; set; }

    /// <summary>评论配图（微信云存储 fileID，可选）</summary>
    public string? ImageUrl { get; set; }

    /// <summary>评论配图临时 https 地址，仅用于 imgSecCheck</summary>
    public string? ImageUrlTemp { get; set; }
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

public class AdminVenuePostHideRequest
{
    /// <summary>true=下架（保留数据）；false=恢复展示</summary>
    public bool Hidden { get; set; }
}
