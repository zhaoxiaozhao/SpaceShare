namespace FriendlySeat.Application.Dtos;

/// <summary>用户动态（社区新鲜事）</summary>
public class UserMomentDto
{
    public long Id { get; set; }
    public long? VenueId { get; set; }
    public string? VenueName { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public long OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerAvatar { get; set; }
    public bool IsOwner { get; set; }
}

public class MomentVisibilityRequest
{
    public bool IsPublic { get; set; }
}