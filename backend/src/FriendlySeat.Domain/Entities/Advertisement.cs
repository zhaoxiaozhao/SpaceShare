namespace FriendlySeat.Domain.Entities;

public class Advertiser
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
}

public class Advertisement
{
    public long Id { get; set; }
    public long AdvertiserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? TargetUrl { get; set; }
    public string Placement { get; set; } = "home_feed";
    public long? CityId { get; set; }
    public long? VenueId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public AdStatus Status { get; set; } = AdStatus.Active;

    public Advertiser? Advertiser { get; set; }
}

public class AdImpression
{
    public long Id { get; set; }
    public long AdId { get; set; }
    public long UserId { get; set; }
    public string Placement { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AdClick
{
    public long Id { get; set; }
    public long AdId { get; set; }
    public long UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
