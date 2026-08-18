namespace FriendlySeat.Domain.Entities;

public class StudySession
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public StudyType Type { get; set; } = StudyType.Other;
    public long? VenueId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationMinutes { get; set; }
    public string? Note { get; set; }
    public StudySessionStatus Status { get; set; } = StudySessionStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
