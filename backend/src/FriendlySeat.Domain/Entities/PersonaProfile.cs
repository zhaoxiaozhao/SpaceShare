namespace FriendlySeat.Domain.Entities;

/// <summary>
/// 友邻画像：基于「作息/社交/学习方式/计划性/兴趣」五个维度的偏好侧写（非心理测评）。
/// 每人一条，默认私密（IsPublic=false），用户可主动公开。
/// </summary>
public class PersonaProfile
{
    public long Id { get; set; }
    public long UserId { get; set; }

    /// <summary>社交能量：1 独行 ↔ 4 结伴</summary>
    public int Social { get; set; }

    /// <summary>作息节律：1 晨型 ↔ 4 夜型</summary>
    public int Rhythm { get; set; }

    /// <summary>学习方式：1 沉浸 ↔ 4 交流</summary>
    public int Style { get; set; }

    /// <summary>计划性：1 随性 ↔ 4 计划</summary>
    public int Plan { get; set; }

    /// <summary>兴趣取向：1 人文 ↔ 4 实用</summary>
    public int Interest { get; set; }

    /// <summary>类型 code（见 PersonaCatalog.Types）</summary>
    public string TypeCode { get; set; } = string.Empty;

    /// <summary>是否公开（默认私密）</summary>
    public bool IsPublic { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
