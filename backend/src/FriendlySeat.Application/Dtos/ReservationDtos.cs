namespace FriendlySeat.Application.Dtos;

public class ReservationCreateRequest
{
    public long ShareId { get; set; }
}

public class ReservationDto
{
    public long Id { get; set; }
    public long SeatId { get; set; }
    public string SeatCode { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public long? ShareId { get; set; }
    public long UserId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReservedAt { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public string? OwnerNickname { get; set; }
}

public class ArrivalResultDto
{
    public long ReservationId { get; set; }
    public bool Confirmed { get; set; }
    public string? Message { get; set; }
}

public class MyReservationSummaryDto
{
    public List<ReservationDto> Upcoming { get; set; } = new();
    public List<ReservationDto> History { get; set; } = new();
    public List<SeatShareDto> MyShares { get; set; } = new();
}

public class WaitlistDto
{
    public long Id { get; set; }
    public long ShareId { get; set; }
    public int Position { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string SeatCode { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}
