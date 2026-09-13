namespace FriendlySeat.Domain.Entities;

/// <summary>换座意向的响应：用户表示愿意和发布者交换，并提交自己的座位位置供对方确认。</summary>
public class SeatSwapResponse
{
    public long Id { get; set; }
    public long RequestId { get; set; }
    public long UserId { get; set; }

    public long? FloorId { get; set; }
    public long? AreaId { get; set; }
    public long? ZoneId { get; set; }

    public SeatSwapResponseStatus Status { get; set; } = SeatSwapResponseStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SeatSwapRequest? Request { get; set; }
    public User? User { get; set; }
}
