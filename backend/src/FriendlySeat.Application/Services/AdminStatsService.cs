using FriendlySeat.Application.Common;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class StatsOverviewDto
{
    public int UserCount { get; set; }
    public int TodayNewUsers { get; set; }
    public int VenueCount { get; set; }
    public int SeatCount { get; set; }
    public int TodayReservations { get; set; }
    public int ActiveReservations { get; set; }
    public double ArrivalRate { get; set; }
    public double NoShowRate { get; set; }
    public int PendingReports { get; set; }
    public int ActiveShares { get; set; }
    public decimal DonationTotal { get; set; }
    public int DonationCount { get; set; }
}

public class DailyTrendDto
{
    public List<string> Dates { get; set; } = new();
    public List<int> Reservations { get; set; } = new();
    public List<int> NewUsers { get; set; } = new();
}

public class AdminStatsService
{
    private readonly IAppDbContext _db;

    public AdminStatsService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<StatsOverviewDto> GetOverviewAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        var userCount = await _db.Users.CountAsync(ct);
        var todayNewUsers = await _db.Users.CountAsync(u => u.CreatedAt >= today, ct);
        var venueCount = await _db.Venues.CountAsync(ct);
        var seatCount = await _db.Seats.CountAsync(ct);

        var todayReservations = await _db.Reservations.CountAsync(r => r.ReservedAt >= today, ct);
        var activeReservations = await _db.Reservations.CountAsync(
            r => r.EndAt > now && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Arrived), ct);

        var arrived = await _db.Reservations.CountAsync(r => r.ReservedAt >= today && r.ArrivedAt.HasValue, ct);
        var noShow = await _db.Reservations.CountAsync(r => r.ReservedAt >= today && r.Status == ReservationStatus.NoShow, ct);
        var totalToday = await _db.Reservations.CountAsync(r => r.ReservedAt >= today, ct);

        var pendingReports = await _db.Reports.CountAsync(r => r.Status == ReportStatus.Pending, ct);
        var activeShares = await _db.SeatShares.CountAsync(s => s.Status == SeatShareStatus.Available && s.EndAt > now, ct);

        var donationTotal = await _db.Donations.Where(d => d.Status == DonationStatus.Paid).SumAsync(d => (decimal?)d.Amount, ct) ?? 0;
        var donationCount = await _db.Donations.CountAsync(d => d.Status == DonationStatus.Paid, ct);

        return new StatsOverviewDto
        {
            UserCount = userCount,
            TodayNewUsers = todayNewUsers,
            VenueCount = venueCount,
            SeatCount = seatCount,
            TodayReservations = todayReservations,
            ActiveReservations = activeReservations,
            ArrivalRate = totalToday == 0 ? 0 : Math.Round(arrived * 100.0 / totalToday, 1),
            NoShowRate = totalToday == 0 ? 0 : Math.Round(noShow * 100.0 / totalToday, 1),
            PendingReports = pendingReports,
            ActiveShares = activeShares,
            DonationTotal = donationTotal,
            DonationCount = donationCount
        };
    }

    public async Task<DailyTrendDto> GetDailyTrendAsync(int days, CancellationToken ct = default)
    {
        var result = new DailyTrendDto();
        var today = DateTime.UtcNow.Date;

        for (var i = days - 1; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var next = date.AddDays(1);
            result.Dates.Add(date.ToString("MM-dd"));
            result.Reservations.Add(await _db.Reservations.CountAsync(r => r.ReservedAt >= date && r.ReservedAt < next, ct));
            result.NewUsers.Add(await _db.Users.CountAsync(u => u.CreatedAt >= date && u.CreatedAt < next, ct));
        }

        return result;
    }
}
