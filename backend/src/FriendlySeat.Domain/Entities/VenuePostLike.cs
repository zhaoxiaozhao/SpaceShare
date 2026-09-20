namespace FriendlySeat.Domain.Entities;

/// <summary>场馆交流帖的点赞（每帖每人一条）。</summary>
public class VenuePostLike
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public long UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public VenuePost? Post { get; set; }
    public User? User { get; set; }
}
