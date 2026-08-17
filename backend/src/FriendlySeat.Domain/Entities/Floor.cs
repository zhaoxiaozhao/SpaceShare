namespace FriendlySeat.Domain.Entities;

public class Floor
{
    public long Id { get; set; }
    public long VenueId { get; set; }
    public long? BuildingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? MapImageUrl { get; set; }

    public Venue? Venue { get; set; }
    public Building? Building { get; set; }
    public ICollection<Zone> Zones { get; set; } = new List<Zone>();
    public ICollection<FloorPoi> Pois { get; set; } = new List<FloorPoi>();
    public ICollection<Area> Areas { get; set; } = new List<Area>();
}
