using FriendlySeat.Application.Common;
using FriendlySeat.Application.Services;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FriendlySeat.Application.Services.Jobs;

public interface IAutoReleaseJob
{
    Task RunAsync(CancellationToken ct = default);
}

public class AutoReleaseJob : IAutoReleaseJob
{
    private readonly IAppDbContext _db;
    private readonly ConfigService _config;
    private readonly CreditService _credit;
    private readonly INotificationService _notifications;
    private readonly ILogger<AutoReleaseJob> _logger;

    private readonly RiskService _risk;

    public AutoReleaseJob(
        IAppDbContext db,
        ConfigService config,
        CreditService credit,
        INotificationService notifications,
        RiskService risk,
        ILogger<AutoReleaseJob> logger)
    {
        _db = db;
        _config = config;
        _credit = credit;
        _notifications = notifications;
        _risk = risk;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var rules = await _config.GetReservationRulesAsync(ct);
        var creditRules = await _config.GetCreditRulesAsync(ct);
        var now = DateTime.UtcNow;
        var grace = TimeSpan.FromMinutes(rules.ArrivalGraceMinutes);

        // 1. 处理到座超时：reserved 且超过 start + grace → no_show
        var overdueReservations = await _db.Reservations
            .Include(r => r.Seat)
            .Where(r => r.Status == ReservationStatus.Reserved && r.StartAt.AddMinutes(rules.ArrivalGraceMinutes) < now)
            .ToListAsync(ct);

        foreach (var reservation in overdueReservations)
        {
            _logger.LogInformation("自动释放超时预约 {ReservationId}", reservation.Id);

            reservation.Status = ReservationStatus.NoShow;
            reservation.ExpiredAt = now;

            if (reservation.ShareId.HasValue)
            {
                var share = await _db.SeatShares.FirstOrDefaultAsync(s => s.Id == reservation.ShareId.Value, ct);
                if (share is not null && share.Status == SeatShareStatus.Reserved)
                {
                    share.Status = SeatShareStatus.Available;
                }
            }

            // 扣信用
            await _credit.AdjustAsync(reservation.UserId, creditRules.NoShowPenalty, "爽约未到", "reservation", reservation.Id, ct);

            // 风控：当天爽约 >= 阈值 记录风险
            var noShowThreshold = await _config.GetIntAsync(ConfigCategory.RiskRules, "no_show_threshold", 2, ct);
            var todayNoShows = await _db.Reservations.CountAsync(
                r => r.UserId == reservation.UserId && r.Status == ReservationStatus.NoShow && r.ExpiredAt >= now.Date, ct);
            if (todayNoShows >= noShowThreshold)
            {
                await _risk.RecordEventAsync(reservation.UserId, "no_show", 20,
                    $"当天爽约{todayNoShows}次", ct);
            }

            await _notifications.SendAsync(reservation.UserId, NotificationType.ReservationExpired,
                "预约已超时释放", "你未在到座时间内确认到达，预约已自动释放。", null, ct);
        }

        // 2. 处理到座后未结束但超时：arrived 且超过 end → completed
        var overArrived = await _db.Reservations
            .Where(r => r.Status == ReservationStatus.Arrived && r.EndAt < now)
            .ToListAsync(ct);

        foreach (var reservation in overArrived)
        {
            reservation.Status = ReservationStatus.Completed;
            reservation.CompletedAt = now;

            var session = await _db.SeatSessions.FirstOrDefaultAsync(
                s => s.SeatId == reservation.SeatId && s.UserId == reservation.UserId && s.Status == SeatSessionStatus.Active, ct);
            if (session is not null)
            {
                session.Status = SeatSessionStatus.Completed;
                session.ActualEndAt = now;
            }

            var seat = await _db.Seats.FirstAsync(s => s.Id == reservation.SeatId, ct);
            seat.Status = SeatStatus.Available;
        }

        // 3. 过期 share 标记为 expired
        var expiredShares = await _db.SeatShares
            .Where(s => (s.Status == SeatShareStatus.Available || s.Status == SeatShareStatus.Reserved) && s.EndAt < now)
            .ToListAsync(ct);

        foreach (var share in expiredShares)
        {
            share.Status = SeatShareStatus.Expired;
        }

        // 4. 过期候补
        var expiredWaitlists = await _db.ReservationWaitlists
            .Where(w => (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Notified) && w.ExpiredAt.HasValue && w.ExpiredAt < now)
            .ToListAsync(ct);
        foreach (var w in expiredWaitlists)
        {
            w.Status = WaitlistStatus.Expired;
        }

        // 5. 清理过期使用会话
        var staleSessions = await _db.SeatSessions
            .Where(s => s.Status == SeatSessionStatus.Active && s.ExpectedEndAt.HasValue && s.ExpectedEndAt.Value.AddHours(2) < now)
            .ToListAsync(ct);
        foreach (var session in staleSessions)
        {
            session.Status = SeatSessionStatus.Completed;
            session.ActualEndAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }
}
