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

    /// <summary>到座核销码（6 位数字，分享者出示给预约者核销到座）</summary>
    public string CheckInCode { get; set; } = string.Empty;

    /// <summary>
    /// 候补预留：座位释放时预留给队首候补用户（该用户 10 分钟内优先预约），
    /// 其他用户不可预约；预留期间 share.Status 为 Reserved。
    /// </summary>
    public long? HoldForUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }

    public Seat? Seat { get; set; }
    public User? OwnerUser { get; set; }
    public SeatSession? SourceSession { get; set; }
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<ReservationWaitlist> Waitlists { get; set; } = new List<ReservationWaitlist>();
}
