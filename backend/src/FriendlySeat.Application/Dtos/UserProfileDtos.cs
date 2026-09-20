namespace FriendlySeat.Application.Dtos;

/// <summary>
/// 访客视角的用户公开资料：仅包含昵称/头像等已公开信息、用户主动公开的友邻画像与公开帖子。
/// 不包含信用分、学习/阅读统计等个人数据。
/// </summary>
public class UserProfileDto
{
    public long Id { get; set; }
    public string Nickname { get; set; } = "友邻";
    public string? AvatarUrl { get; set; }
    public DateTime JoinedAt { get; set; }

    /// <summary>是否为当前访问者本人</summary>
    public bool IsSelf { get; set; }

    /// <summary>公开帖子数量</summary>
    public int PostCount { get; set; }

    /// <summary>对方是否公开了友邻画像</summary>
    public bool PersonaPublic { get; set; }

    /// <summary>友邻画像（仅在对方公开时返回）</summary>
    public PersonaProfileDto? Persona { get; set; }

    /// <summary>公开帖子（最近若干条）</summary>
    public List<VenuePostDto> Posts { get; set; } = new();
}
