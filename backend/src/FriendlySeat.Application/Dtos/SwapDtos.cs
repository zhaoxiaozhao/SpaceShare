namespace FriendlySeat.Application.Dtos;

public class SeatSwapCreateRequest
{
    public long VenueId { get; set; }

    public long? FloorId { get; set; }
    public long? AreaId { get; set; }
    public long? ZoneId { get; set; }

    public long? WantFloorId { get; set; }
    public long? WantAreaId { get; set; }
    public long? WantZoneId { get; set; }

    /// <summary>客观因素标签 code：light/cold/hot/noise/together/window/socket/other</summary>
    public List<string> Reasons { get; set; } = new();

    /// <summary>有效期（分钟）：30 / 60 / 120</summary>
    public int DurationMinutes { get; set; } = 60;
}

public class SeatSwapRespondRequest
{
    public long? FloorId { get; set; }
    public long? AreaId { get; set; }
    public long? ZoneId { get; set; }
}

public class SeatSwapResponseDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string UserNickname { get; set; } = string.Empty;
    public long? FloorId { get; set; }
    public string? FloorName { get; set; }
    public long? AreaId { get; set; }
    public string? AreaName { get; set; }
    public long? ZoneId { get; set; }
    public string? ZoneName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsMine { get; set; }
}

public class SeatSwapDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string UserNickname { get; set; } = string.Empty;
    public long VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;

    public long? FloorId { get; set; }
    public string? FloorName { get; set; }
    public long? AreaId { get; set; }
    public string? AreaName { get; set; }
    public long? ZoneId { get; set; }
    public string? ZoneName { get; set; }

    public long? WantFloorId { get; set; }
    public string? WantFloorName { get; set; }
    public long? WantAreaId { get; set; }
    public string? WantAreaName { get; set; }
    public long? WantZoneId { get; set; }
    public string? WantZoneName { get; set; }

    public List<string> Reasons { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpireAt { get; set; }
    public bool IsMine { get; set; }

    /// <summary>是否已响应（广场列表里标识当前用户是否已响应）</summary>
    public bool RespondedByMe { get; set; }

    /// <summary>我对该意向的响应状态（Pending/Accepted/Rejected），未响应为 null</summary>
    public string? MyResponseStatus { get; set; }

    /// <summary>被选中的响应 Id（发布者视图）</summary>
    public long? MatchedResponseId { get; set; }

    /// <summary>响应列表：仅发布者自己的请求会返回</summary>
    public List<SeatSwapResponseDto> Responses { get; set; } = new();
}
