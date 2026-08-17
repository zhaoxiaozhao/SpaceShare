namespace FriendlySeat.Domain.Entities;

public class Report
{
    public long Id { get; set; }
    public long ReporterUserId { get; set; }
    public long? TargetUserId { get; set; }
    public ReportTargetType TargetType { get; set; }
    public long? TargetId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? EvidenceUrl { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public long? HandledBy { get; set; }
    public DateTime? HandledAt { get; set; }
    public string? HandleNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? ReporterUser { get; set; }
    public User? TargetUser { get; set; }
}
