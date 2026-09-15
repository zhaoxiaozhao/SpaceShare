namespace FriendlySeat.Domain.Entities;

/// <summary>
/// 书单分享：用户自选书籍 + 推荐语，生成不可枚举 token 的公开快照，供好友查看。
/// </summary>
public class BookListShare
{
    public long Id { get; set; }
    public long UserId { get; set; }

    /// <summary>访问 token（随机，防枚举）</summary>
    public string Token { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary>推荐语（可空）</summary>
    public string? Remark { get; set; }

    /// <summary>书单条目快照（JSON 数组）</summary>
    public string ItemsJson { get; set; } = "[]";

    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
