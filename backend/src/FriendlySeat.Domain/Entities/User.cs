namespace FriendlySeat.Domain.Entities;

public class User
{
    public long Id { get; set; }
    public string OpenId { get; set; } = string.Empty;
    public string? UnionId { get; set; }
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public int CreditScore { get; set; } = 100;
    public int RiskScore { get; set; }
    /// <summary>是否公开我的动态（自动生成的社区动态，默认开启，可在个人中心关闭）</summary>
    public bool MomentsPublic { get; set; } = true;

    /// <summary>是否公开我的读书笔记/摘抄（自动生成交流帖，默认关闭，需用户主动开启）</summary>
    public bool ReadNotePublic { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<UserContact> Contacts { get; set; } = new List<UserContact>();
}
