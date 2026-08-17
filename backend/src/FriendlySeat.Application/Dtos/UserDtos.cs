namespace FriendlySeat.Application.Dtos;

public class UserDto
{
    public long Id { get; set; }
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public int CreditScore { get; set; }
    public string CreditLevel { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UserProfileUpdateRequest
{
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
}

public class UserContactDto
{
    public long Id { get; set; }
    public string ContactType { get; set; } = string.Empty;
    public string ContactValue { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
}

public class UpsertContactRequest
{
    public string ContactType { get; set; } = "WechatId";
    public string ContactValue { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
}

public class PublicContributionDto
{
    public int ShareCount { get; set; }
    public double ShareHours { get; set; }
    public int HelpedCount { get; set; }
    public int OnTimeCount { get; set; }
}
