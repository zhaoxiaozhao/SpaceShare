namespace FriendlySeat.Domain.Entities;

/// <summary>
/// 场馆交流帖：按图书馆/场馆维度组织的学习交流与信息共享（公开帖、无私信、无联系方式）。
/// 被举报后自动隐藏并进入后台审核；管理员可置顶/下架。
/// </summary>
public class VenuePost
{
    public long Id { get; set; }
    public long VenueId { get; set; }
    public long UserId { get; set; }

    /// <summary>分类 code（可配置，如 help/study/books/advice/lost/chat）</summary>
    public string Category { get; set; } = "chat";

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    /// <summary>封面图（微信云存储 fileID，可选）</summary>
    public string? CoverImage { get; set; }

    public CommentStatus Status { get; set; } = CommentStatus.Visible;

    /// <summary>管理端置顶</summary>
    public bool IsPinned { get; set; }

    public int LikeCount { get; set; }
    public int CommentCount { get; set; }

    /// <summary>浏览量（进入详情页累加，作者本人不计）</summary>
    public int ViewCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Venue? Venue { get; set; }
    public User? User { get; set; }
    public ICollection<VenuePostComment> Comments { get; set; } = new List<VenuePostComment>();
    public ICollection<VenuePostLike> Likes { get; set; } = new List<VenuePostLike>();
}
