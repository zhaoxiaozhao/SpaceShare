namespace FriendlySeat.Domain.Entities;

/// <summary>书单收藏：用户收藏他人公开分享的书单（纯计数，不做奖励/解锁）。</summary>
public class BookListShareFavorite
{
    public long Id { get; set; }
    public long ShareId { get; set; }
    public long UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public BookListShare? Share { get; set; }
    public User? User { get; set; }
}
