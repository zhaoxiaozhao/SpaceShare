namespace FriendlySeat.Domain.Entities;

public class Donation
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentChannel { get; set; } = "wechat";
    public string? TransactionId { get; set; }
    public DonationStatus Status { get; set; } = DonationStatus.Pending;
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    public User? User { get; set; }
}
