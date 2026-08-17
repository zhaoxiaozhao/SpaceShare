using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class DonationService
{
    private readonly IAppDbContext _db;

    public DonationService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<DonationDto> CreateAsync(long userId, DonationCreateRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw AppException.BadRequest("amount_invalid", "支持金额必须大于0");

        var donation = new Donation
        {
            UserId = userId,
            Amount = request.Amount,
            PaymentChannel = request.PaymentChannel,
            Status = DonationStatus.Pending,
            IsPublic = request.IsPublic,
            CreatedAt = DateTime.UtcNow
        };
        _db.Donations.Add(donation);
        await _db.SaveChangesAsync(ct);

        return new DonationDto
        {
            Id = donation.Id,
            Amount = donation.Amount,
            Status = donation.Status.ToString(),
            CreatedAt = donation.CreatedAt
        };
    }

    public async Task<DonationSummaryDto> GetSummaryAsync(long userId, CancellationToken ct = default)
    {
        var my = await _db.Donations
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DonationDto
            {
                Id = d.Id,
                Amount = d.Amount,
                Status = d.Status.ToString(),
                CreatedAt = d.CreatedAt
            })
            .ToListAsync(ct);

        var totalAmount = await _db.Donations
            .Where(d => d.Status == DonationStatus.Paid)
            .SumAsync(d => (decimal?)d.Amount) ?? 0;
        var totalCount = await _db.Donations.CountAsync(d => d.Status == DonationStatus.Paid, ct);

        return new DonationSummaryDto
        {
            TotalAmount = totalAmount,
            TotalCount = totalCount,
            MonthCost = 200,
            MyDonations = my
        };
    }

    // 支付回调（MVP 阶段做标记，实际支付能力按微信审核结果落地）
    public async Task MarkPaidAsync(long donationId, string transactionId, CancellationToken ct = default)
    {
        var donation = await _db.Donations.FirstOrDefaultAsync(d => d.Id == donationId, ct)
            ?? throw AppException.NotFound("支持记录不存在");

        donation.Status = DonationStatus.Paid;
        donation.TransactionId = transactionId;
        donation.PaidAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
