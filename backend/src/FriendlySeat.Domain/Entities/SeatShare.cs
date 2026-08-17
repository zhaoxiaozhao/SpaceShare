namespace FriendlySeat.Domain.Entities;

public class SeatShare
{
    public long Id { get; set; }
    public long SeatId { get; set; }
    public long OwnerUserId { get; set; }
    public long? SourceSessionId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public SeatShareStatus Status { get; set; } = SeatShareStatus.Available;
    public string? Note { get; set; }
    public bool AllowContact { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }

    public Seat? Seat { get; set; }
    public User? OwnerUser { get; set; }
    public SeatSession? SourceSession { get; set; }
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<ReservationWaitlist> Waitlists { get; set; } = new List<ReservationWaitlist>();
}
