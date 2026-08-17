namespace FriendlySeat.Domain.Entities;

public class Reservation
{
    public long Id { get; set; }
    public long SeatId { get; set; }
    public long? ShareId { get; set; }
    public long UserId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Reserved;
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ArrivedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Seat? Seat { get; set; }
    public SeatShare? Share { get; set; }
    public User? User { get; set; }
}
