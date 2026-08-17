namespace FriendlySeat.Domain.Entities;

public class City
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "CN";
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public ICollection<Venue> Venues { get; set; } = new List<Venue>();
}
