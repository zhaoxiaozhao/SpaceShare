namespace FriendlySeat.Domain.Entities;

public class ReadingNote
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long BookId { get; set; }
    public ReadingNoteType Type { get; set; } = ReadingNoteType.Note;
    public string Content { get; set; } = string.Empty;
    public string? Position { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public ReadingBook? Book { get; set; }
}