namespace FriendlySeat.Domain.Entities;

/// <summary>评论点赞（每人每条评论最多一次）</summary>
public class VenuePostCommentLike
{
    public long Id { get; set; }
    public long CommentId { get; set; }
    public long UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public VenuePostComment? Comment { get; set; }
    public User? User { get; set; }
}
