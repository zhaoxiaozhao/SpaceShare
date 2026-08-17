namespace FriendlySeat.Domain.Entities;

public class UserContact
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public ContactType ContactType { get; set; }
    public string ContactValue { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
