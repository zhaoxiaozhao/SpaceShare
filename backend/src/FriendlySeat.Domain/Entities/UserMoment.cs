namespace FriendlySeat.Domain.Entities;

/// <summary>
/// 用户动态（社区新鲜事）：由用户行为自动生成，展示在交流板「动态」标签页。
/// 生成前经过敏感词拦截，用户可在个人中心一键关闭公开，也可删除自己的动态。
/// </summary>
public class UserMoment
{
    public long Id { get; set; }
    public long UserId { get; set; }

    /// <summary>所属场馆（「正在阅读」等场景可空）</summary>
    public long? VenueId { get; set; }

    /// <summary>场馆名（冗余存储，便于展示，场馆改名无需回刷）</summary>
    public string? VenueName { get; set; }

    /// <summary>动态类型：seat_share / swap / seat_note / reading / check_in</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>展示文案（不含昵称与场馆名，昵称/场馆由前端按 DTO 拼接）</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>封面图链接（未来分享带图时预留，暂置空）</summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 源对象引用（用于去重与级联删除，如 seat_note:123 / swap:456 / share:789 / book:21）
    /// </summary>
    public string? TargetKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}