namespace FriendlySeat.Domain.Entities;

/// <summary>
/// 场馆交流帖的评论（一级评论 + 对其的回复，最多两层）。
/// 回复的 ParentCommentId 始终指向所属的一级评论；ReplyToUserId 记录被回复者。
/// 被举报后自动隐藏并进入后台审核。
/// </summary>
public class VenuePostComment
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public long UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public CommentStatus Status { get; set; } = CommentStatus.Visible;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>评论配图（微信云存储 fileID，可选，最多 1 张）</summary>
    public string? ImageUrl { get; set; }

    /// <summary>点赞数</summary>
    public int LikeCount { get; set; }

    /// <summary>所属一级评论 Id（一级评论为 null；回复时指向一级评论）</summary>
    public long? ParentCommentId { get; set; }

    /// <summary>被回复者 Id（仅回复有值，用于展示「回复 @昵称」）</summary>
    public long? ReplyToUserId { get; set; }

    public VenuePost? Post { get; set; }
    public User? User { get; set; }
}
