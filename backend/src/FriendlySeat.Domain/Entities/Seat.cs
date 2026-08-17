namespace FriendlySeat.Domain.Entities;

public class Seat
{
    public long Id { get; set; }
    public long ZoneId { get; set; }
    public string Code { get; set; } = string.Empty;
    public SeatType Type { get; set; } = SeatType.Normal;
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public bool Window { get; set; }
    public bool PowerSocket { get; set; }
    public int? QuietLevel { get; set; }
    public int? LightLevel { get; set; }
    public SeatStatus Status { get; set; } = SeatStatus.Available;
    public string? PhotoUrl { get; set; }
    public string? Description { get; set; }
    public bool Verified { get; set; }

    public Zone? Zone { get; set; }
    public ICollection<SeatSession> Sessions { get; set; } = new List<SeatSession>();
    public ICollection<SeatShare> Shares { get; set; } = new List<SeatShare>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
