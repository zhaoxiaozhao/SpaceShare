namespace FriendlySeat.Domain.Entities;

/// <summary>换座意向的响应：用户表示愿意和发布者交换，并标记自己的座位供对方确认。</summary>
public class SeatSwapResponse
{
    public long Id { get; set; }
    public long RequestId { get; set; }
    public long UserId { get; set; }

    /// <summary>响应者当前座位</summary>
    public long SeatId { get; set; }

    public SeatSwapResponseStatus Status { get; set; } = SeatSwapResponseStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SeatSwapRequest? Request { get; set; }
    public User? User { get; set; }
    public Seat? Seat { get; set; }
}
