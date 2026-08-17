namespace FriendlySeat.Domain.Entities;

public class ReservationWaitlist
{
    public long Id { get; set; }
    public long ShareId { get; set; }
    public long UserId { get; set; }
    public int Position { get; set; }
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;
    public DateTime? NotifiedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SeatShare? Share { get; set; }
    public User? User { get; set; }
}
