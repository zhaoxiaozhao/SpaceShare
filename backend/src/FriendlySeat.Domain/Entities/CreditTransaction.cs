namespace FriendlySeat.Domain.Entities;

public class CreditTransaction
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public int Change { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
