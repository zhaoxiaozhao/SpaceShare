namespace FriendlySeat.Domain.Entities;

public class Building
{
    public long Id { get; set; }
    public long VenueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Venue? Venue { get; set; }
    public ICollection<Floor> Floors { get; set; } = new List<Floor>();
}
