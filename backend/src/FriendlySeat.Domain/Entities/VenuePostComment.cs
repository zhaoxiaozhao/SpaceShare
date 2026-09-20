namespace FriendlySeat.Domain.Entities;

/// <summary>场馆交流帖的评论（一级评论）。被举报后自动隐藏并进入后台审核。</summary>
public class VenuePostComment
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public long UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public CommentStatus Status { get; set; } = CommentStatus.Visible;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public VenuePost? Post { get; set; }
    public User? User { get; set; }
}
