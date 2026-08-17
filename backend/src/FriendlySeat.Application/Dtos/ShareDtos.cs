namespace FriendlySeat.Application.Dtos;

public class ShareCreateRequest
{
    public long SeatId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Note { get; set; }
    public bool AllowContact { get; set; }
}

public class SeatShareDto
{
    public long Id { get; set; }
    public long SeatId { get; set; }
    public string SeatCode { get; set; } = string.Empty;

    /// <summary>展示编号：区块字母 + 座位序号，如 B区-002</summary>
    public string DisplayCode { get; set; } = string.Empty;

    public string VenueName { get; set; } = string.Empty;

    /// <summary>楼层（如 3F）</summary>
    public string FloorName { get; set; } = string.Empty;

    /// <summary>空间区域名（如 主空间）</summary>
    public string? AreaName { get; set; }

    public long OwnerUserId { get; set; }
    public string? OwnerNickname { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public bool AllowContact { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ShareDetailDto : SeatShareDto
{
    public int WaitlistCount { get; set; }
    public bool IsMine { get; set; }
    public bool IsReservable { get; set; }
}

public class ContactResultDto
{
    public string ContactType { get; set; } = string.Empty;
    public string ContactValue { get; set; } = string.Empty;
}
