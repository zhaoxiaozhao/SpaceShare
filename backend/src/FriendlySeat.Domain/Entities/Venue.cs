namespace FriendlySeat.Domain.Entities;

public class Venue
{
    public long Id { get; set; }
    public long CityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public VenueType Type { get; set; } = VenueType.Library;
    public string Address { get; set; } = string.Empty;
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public string? Description { get; set; }
    // TimeSpan 兼容 PostgreSQL time 与 MySQL TIME（MySql provider 对 TimeOnly 支持不完善）
    public TimeSpan OpeningTime { get; set; } = TimeSpan.FromHours(9);
    public TimeSpan ClosingTime { get; set; } = TimeSpan.FromHours(22);
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public City? City { get; set; }
    public ICollection<Building> Buildings { get; set; } = new List<Building>();
    public ICollection<Floor> Floors { get; set; } = new List<Floor>();
}
