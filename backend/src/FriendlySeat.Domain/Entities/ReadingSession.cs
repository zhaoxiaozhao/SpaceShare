namespace FriendlySeat.Domain.Entities;

public class ReadingSession
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long BookId { get; set; }

    /// <summary>阅读所在场馆（图书馆/自习室），可空</summary>
    public long? VenueId { get; set; }

    public string? VenueName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationMinutes { get; set; }
    public ReadingSessionStatus Status { get; set; } = ReadingSessionStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public ReadingBook? Book { get; set; }
    public Venue? Venue { get; set; }
}