namespace FriendlySeat.Domain.Entities;

public class ReadingBook
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? CoverUrl { get; set; }

    /// <summary>常阅读的场馆（图书馆/自习室），可空</summary>
    public long? VenueId { get; set; }

    public string? VenueName { get; set; }
    public BookStatus Status { get; set; } = BookStatus.WantToRead;
    public int CurrentProgress { get; set; }
    public int? TotalPages { get; set; }
    public string? LastPosition { get; set; }
    public int TotalMinutes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Venue? Venue { get; set; }
}