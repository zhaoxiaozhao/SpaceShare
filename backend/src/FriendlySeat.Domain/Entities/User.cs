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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<UserContact> Contacts { get; set; } = new List<UserContact>();
}
