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

    // 学习 / 阅读概览
    public int StudyTotalMinutes { get; set; }
    public int StudyUserCount { get; set; }
    public int StudyTodayMinutes { get; set; }
    public int ReadingTotalMinutes { get; set; }
    public int ReadingUserCount { get; set; }
    public int FinishedBookCount { get; set; }
}

public class LearningOverviewDto
{
    public int StudyTotalMinutes { get; set; }
    public int StudyUserCount { get; set; }
    public int StudyTodayMinutes { get; set; }
    public int ReadingTotalMinutes { get; set; }
    public int ReadingUserCount { get; set; }
    public int ReadingBookCount { get; set; }
    public int FinishedBookCount { get; set; }
    public List<string> Dates { get; set; } = new();
    public List<int> StudyMinutesTrend { get; set; } = new();
    public List<int> ReadingMinutesTrend { get; set; } = new();
}

public class LearningUserDto
{
    public long UserId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public int StudyMinutes { get; set; }
    public int ReadingMinutes { get; set; }
    public int TotalMinutes { get; set; }
    public int BookCount { get; set; }
    public int FinishedBookCount { get; set; }
    public DateTime? LastActiveAt { get; set; }
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

        // 学习 / 阅读（时间按北京时间日界）
        var cnTodayStartUtc = CnDayStartUtc(ToCn(now));

        var studyTotalMinutes = await _db.StudySessions
            .Where(s => s.Status == StudySessionStatus.Completed)
            .SumAsync(s => (int?)s.DurationMinutes, ct) ?? 0;
        var studyUserCount = await _db.StudySessions
            .Where(s => s.Status == StudySessionStatus.Completed)
            .Select(s => s.UserId).Distinct().CountAsync(ct);
        var studyTodayMinutes = await _db.StudySessions
            .Where(s => s.Status == StudySessionStatus.Completed && s.StartedAt >= cnTodayStartUtc)
            .SumAsync(s => (int?)s.DurationMinutes, ct) ?? 0;

        var readingTotalMinutes = await _db.ReadingSessions
            .Where(s => s.Status == ReadingSessionStatus.Completed)
            .SumAsync(s => (int?)s.DurationMinutes, ct) ?? 0;
        var readingUserCount = await _db.ReadingSessions
            .Where(s => s.Status == ReadingSessionStatus.Completed)
            .Select(s => s.UserId).Distinct().CountAsync(ct);
        var finishedBookCount = await _db.ReadingBooks.CountAsync(b => b.Status == BookStatus.Finished, ct);

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
            DonationCount = donationCount,
            StudyTotalMinutes = studyTotalMinutes,
            StudyUserCount = studyUserCount,
            StudyTodayMinutes = studyTodayMinutes,
            ReadingTotalMinutes = readingTotalMinutes,
            ReadingUserCount = readingUserCount,
            FinishedBookCount = finishedBookCount
        };
    }

    /// <summary>学习 / 阅读聚合概览（含近 N 日时长趋势，按北京时间日界）</summary>
    public async Task<LearningOverviewDto> GetLearningOverviewAsync(int days = 7, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, 30);
        var now = DateTime.UtcNow;

        var result = new LearningOverviewDto
        {
            StudyTotalMinutes = await _db.StudySessions
                .Where(s => s.Status == StudySessionStatus.Completed)
                .SumAsync(s => (int?)s.DurationMinutes, ct) ?? 0,
            StudyUserCount = await _db.StudySessions
                .Where(s => s.Status == StudySessionStatus.Completed)
                .Select(s => s.UserId).Distinct().CountAsync(ct),
            StudyTodayMinutes = await _db.StudySessions
                .Where(s => s.Status == StudySessionStatus.Completed && s.StartedAt >= CnDayStartUtc(ToCn(now)))
                .SumAsync(s => (int?)s.DurationMinutes, ct) ?? 0,
            ReadingTotalMinutes = await _db.ReadingSessions
                .Where(s => s.Status == ReadingSessionStatus.Completed)
                .SumAsync(s => (int?)s.DurationMinutes, ct) ?? 0,
            ReadingUserCount = await _db.ReadingSessions
                .Where(s => s.Status == ReadingSessionStatus.Completed)
                .Select(s => s.UserId).Distinct().CountAsync(ct),
            ReadingBookCount = await _db.ReadingBooks.CountAsync(ct),
            FinishedBookCount = await _db.ReadingBooks.CountAsync(b => b.Status == BookStatus.Finished, ct)
        };

        // 近 N 日趋势：按北京时间归组
        var cnTodayStart = ToCn(now).Date;
        var startCnDate = cnTodayStart.AddDays(-(days - 1));
        var startUtc = CnDayStartUtc(startCnDate);

        var studySessions = await _db.StudySessions
            .Where(s => s.Status == StudySessionStatus.Completed && s.StartedAt >= startUtc)
            .Select(s => new { s.StartedAt, s.DurationMinutes })
            .ToListAsync(ct);
        var readingSessions = await _db.ReadingSessions
            .Where(s => s.Status == ReadingSessionStatus.Completed && s.StartedAt >= startUtc)
            .Select(s => new { s.StartedAt, s.DurationMinutes })
            .ToListAsync(ct);

        for (var i = 0; i < days; i++)
        {
            var d = startCnDate.AddDays(i);
            result.Dates.Add(d.ToString("MM-dd"));
            result.StudyMinutesTrend.Add(studySessions
                .Where(s => ToCn(s.StartedAt).Date == d)
                .Sum(s => s.DurationMinutes));
            result.ReadingMinutesTrend.Add(readingSessions
                .Where(s => ToCn(s.StartedAt).Date == d)
                .Sum(s => s.DurationMinutes));
        }

        return result;
    }

    /// <summary>学习/阅读用户维度列表（按总时长倒序）</summary>
    public async Task<List<LearningUserDto>> GetLearningUsersAsync(int take = 100, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);

        var studyAgg = await _db.StudySessions
            .Where(s => s.Status == StudySessionStatus.Completed)
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, Minutes = g.Sum(x => x.DurationMinutes), Last = g.Max(x => x.StartedAt) })
            .ToListAsync(ct);
        var readingAgg = await _db.ReadingSessions
            .Where(s => s.Status == ReadingSessionStatus.Completed)
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, Minutes = g.Sum(x => x.DurationMinutes), Last = g.Max(x => x.StartedAt) })
            .ToListAsync(ct);
        var bookAgg = await _db.ReadingBooks
            .GroupBy(b => b.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count(), Finished = g.Count(x => x.Status == BookStatus.Finished) })
            .ToListAsync(ct);

        var ids = studyAgg.Select(x => x.UserId)
            .Concat(readingAgg.Select(x => x.UserId))
            .Concat(bookAgg.Select(x => x.UserId))
            .Distinct()
            .ToList();

        var nicknames = await _db.Users.Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Nickname })
            .ToDictionaryAsync(u => u.Id, u => u.Nickname, ct);

        var list = ids.Select(id =>
        {
            var s = studyAgg.FirstOrDefault(x => x.UserId == id);
            var r = readingAgg.FirstOrDefault(x => x.UserId == id);
            var b = bookAgg.FirstOrDefault(x => x.UserId == id);
            var study = s?.Minutes ?? 0;
            var reading = r?.Minutes ?? 0;
            var lastDates = new[] { s?.Last, r?.Last }.Where(x => x.HasValue).Select(x => x!.Value).ToList();
            return new LearningUserDto
            {
                UserId = id,
                Nickname = nicknames.TryGetValue(id, out var nick) && !string.IsNullOrWhiteSpace(nick) ? nick! : $"用户#{id}",
                StudyMinutes = study,
                ReadingMinutes = reading,
                TotalMinutes = study + reading,
                BookCount = b?.Count ?? 0,
                FinishedBookCount = b?.Finished ?? 0,
                LastActiveAt = lastDates.Count > 0 ? lastDates.Max() : null
            };
        })
        .OrderByDescending(x => x.TotalMinutes)
        .Take(take)
        .ToList();

        return list;
    }

    private static readonly TimeZoneInfo ChinaTz = TimeZoneInfo.CreateCustomTimeZone(
        "China Standard Time", TimeSpan.FromHours(8), "China Standard Time", "China Standard Time");

    private static DateTime ToCn(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, ChinaTz);
    private static DateTime CnDayStartUtc(DateTime cnDate) => TimeZoneInfo.ConvertTimeToUtc(cnDate.Date, ChinaTz);

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
