namespace FriendlySeat.Application.Dtos;

public class ReportCreateRequest
{
    public string TargetType { get; set; } = string.Empty;
    public long? TargetId { get; set; }
    public long? TargetUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? EvidenceUrl { get; set; }
}

public class ReportDto
{
    public long Id { get; set; }
    public long ReporterUserId { get; set; }
    public string? ReporterNickname { get; set; }
    public long? TargetUserId { get; set; }
    public string? TargetUserNickname { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public long? TargetId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? EvidenceUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreditTransactionDto
{
    public long Id { get; set; }
    public int Change { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreditSummaryDto
{
    public int Score { get; set; }
    public string Level { get; set; } = string.Empty;
    public List<CreditTransactionDto> Transactions { get; set; } = new();
}

public class DonationCreateRequest
{
    public decimal Amount { get; set; }
    public string PaymentChannel { get; set; } = "wechat";
    public bool IsPublic { get; set; }
}

public class DonationDto
{
    public long Id { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class DonationSummaryDto
{
    public decimal TotalAmount { get; set; }
    public int TotalCount { get; set; }
    public decimal MonthCost { get; set; }
    public List<DonationDto> MyDonations { get; set; } = new();
}

public class AdDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? TargetUrl { get; set; }
    public string Placement { get; set; } = string.Empty;
}

public class NotificationDto
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PagedResult<T>
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<T> Items { get; set; } = new();
}
