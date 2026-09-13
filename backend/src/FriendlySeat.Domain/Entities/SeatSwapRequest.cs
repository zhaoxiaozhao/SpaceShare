namespace FriendlySeat.Domain.Entities;

/// <summary>
/// 换座意向：用户在座位详情页标记自己的座位并发布"我想换到哪"，其他用户自愿响应、提交自己的座位，
/// 发布者确认后双方线下物理交换。平台仅提供信息撮合，不涉及座位买卖/转让，以场馆规定为准。
/// </summary>
public class SeatSwapRequest
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long VenueId { get; set; }

    /// <summary>发布者当前座位</summary>
    public long SeatId { get; set; }

    /// <summary>期望换到的楼层（null = 不限）</summary>
    public long? WantFloorId { get; set; }

    /// <summary>期望换到的区域（null = 不限）</summary>
    public long? WantAreaId { get; set; }

    /// <summary>期望换到的区块（null = 不限）</summary>
    public long? WantZoneId { get; set; }

    /// <summary>客观因素标签（逗号分隔的 code：light/cold/hot/noise/together/window/socket/other）</summary>
    public string Reasons { get; set; } = string.Empty;

    public SeatSwapStatus Status { get; set; } = SeatSwapStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpireAt { get; set; }
    public DateTime? MatchedAt { get; set; }

    /// <summary>被选中的响应 Id</summary>
    public long? MatchedResponseId { get; set; }

    public User? User { get; set; }
    public Venue? Venue { get; set; }
    public Seat? Seat { get; set; }
    public ICollection<SeatSwapResponse> Responses { get; set; } = new List<SeatSwapResponse>();
}
