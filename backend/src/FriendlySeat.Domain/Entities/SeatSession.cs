namespace FriendlySeat.Domain.Entities;

public class SeatSession
{
    public long Id { get; set; }
    public long SeatId { get; set; }
    public long UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ExpectedEndAt { get; set; }
    public DateTime? ActualEndAt { get; set; }
    public DateTime? ArrivalAt { get; set; }
    public SeatSessionStatus Status { get; set; } = SeatSessionStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Seat? Seat { get; set; }
    public User? User { get; set; }
    public ICollection<SeatShare> Shares { get; set; } = new List<SeatShare>();
}
