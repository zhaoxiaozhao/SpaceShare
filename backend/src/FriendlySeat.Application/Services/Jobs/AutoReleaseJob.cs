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

        // 1. 处理到座超时：reserved 且超过宽限 → no_show
        //    宽限从「预约开始时间」与「预约生效时间」中较晚者起算，避免临近分享结束的预约刚生成就被误判爽约
        var overdueReservations = await _db.Reservations
            .Include(r => r.Seat)
            .Where(r => r.Status == ReservationStatus.Reserved && (
                (r.ReservedAt <= r.StartAt && r.StartAt.AddMinutes(rules.ArrivalGraceMinutes) < now) ||
                (r.ReservedAt > r.StartAt && r.ReservedAt.AddMinutes(rules.ArrivalGraceMinutes) < now)))
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

        // 1.5 到座提醒：宽限剩余 ≤10 分钟仍未确认的预约，提醒用户尽快确认（每单只提醒一次）
        var remindDeadline = now.AddMinutes(10);
        var toRemind = await _db.Reservations
            .Where(r => r.Status == ReservationStatus.Reserved && (
                (r.ReservedAt <= r.StartAt && r.StartAt.AddMinutes(rules.ArrivalGraceMinutes) < remindDeadline) ||
                (r.ReservedAt > r.StartAt && r.ReservedAt.AddMinutes(rules.ArrivalGraceMinutes) < remindDeadline)))
            .ToListAsync(ct);

        foreach (var reservation in toRemind)
        {
            // 防重：该预约已发过到座提醒则跳过（Data 存预约Id）
            var dataTag = $"arrival_reminder:{reservation.Id}";
            var alreadyReminded = await _db.Notifications
                .AnyAsync(n => n.UserId == reservation.UserId && n.Data == dataTag, ct);
            if (alreadyReminded) continue;

            var minutesLeft = reservation.ReservedAt > reservation.StartAt
                ? (int)Math.Max(0, (reservation.ReservedAt.AddMinutes(rules.ArrivalGraceMinutes) - now).TotalMinutes)
                : (int)Math.Max(0, (reservation.StartAt.AddMinutes(rules.ArrivalGraceMinutes) - now).TotalMinutes);
            await _notifications.SendAsync(reservation.UserId, NotificationType.ArrivalRequired,
                "到座提醒", $"你预约的座位将在 {minutesLeft} 分钟后超时释放，如已到座请尽快在小程序确认。", dataTag, ct);
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

        // 4. 过期候补：标记过期后，通知同分享下一位候补（候补链条）
        var expiredWaitlists = await _db.ReservationWaitlists
            .Where(w => (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Notified) && w.ExpiredAt.HasValue && w.ExpiredAt < now)
            .ToListAsync(ct);
        var shareIdsToRecheck = new List<long>();
        foreach (var w in expiredWaitlists)
        {
            w.Status = WaitlistStatus.Expired;
            shareIdsToRecheck.Add(w.ShareId);
        }
        if (expiredWaitlists.Count > 0) await _db.SaveChangesAsync(ct);

        // 对每条过期候补对应的分享，通知当前队首候补
        foreach (var shareId in shareIdsToRecheck.Distinct())
        {
            await NotifyNextWaitlistAsync(shareId, ct);
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

        // 6. 风险晋升检查：风险分达到阈值自动升级处罚（可配置）
        await AutoBanHighRiskUsersAsync(now, ct);
    }

    // 风险分 ≥ 阈值自动封禁（重复爽约/频繁取消等累积导致），封禁并通知用户
    private async Task AutoBanHighRiskUsersAsync(DateTime now, CancellationToken ct)
    {
        var banThreshold = await _config.GetIntAsync(ConfigCategory.RiskRules, "auto_ban_threshold", 80, ct);
        if (banThreshold <= 0) return; // 配置为 0 或负数表示关闭自动封禁

        var highRiskUsers = await _db.Users
            .Where(u => u.Status == UserStatus.Active && u.RiskScore >= banThreshold)
            .ToListAsync(ct);

        foreach (var user in highRiskUsers)
        {
            user.Status = UserStatus.Banned;
            user.UpdatedAt = now;
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("风险分达到 {Threshold}，自动封禁用户 {UserId}", banThreshold, user.Id);
            await _notifications.SendAsync(user.Id, NotificationType.CreditChanged,
                "账号已被封禁", "你的账号因多次违规被系统封禁，如有疑问请联系管理员申诉。", null, ct);
        }
    }

    // 候补链条：通知分享当前队首候补（若有空位）
    private async Task NotifyNextWaitlistAsync(long shareId, CancellationToken ct)
    {
        var rules = await _config.GetReservationRulesAsync(ct);
        var next = await _db.ReservationWaitlists
            .Where(w => w.ShareId == shareId && w.Status == WaitlistStatus.Waiting)
            .OrderBy(w => w.Position)
            .FirstOrDefaultAsync(ct);
        if (next is null) return;

        var share = await _db.SeatShares.Include(s => s.Seat).FirstOrDefaultAsync(s => s.Id == shareId, ct);
        if (share is null || share.Status != SeatShareStatus.Available) return;

        next.Status = WaitlistStatus.Notified;
        next.NotifiedAt = DateTime.UtcNow;
        next.ExpiredAt = DateTime.UtcNow.AddMinutes(rules.WaitlistWindowMinutes);
        await _db.SaveChangesAsync(ct);

        await _notifications.SendAsync(next.UserId, NotificationType.WaitlistAvailable,
            "候补成功", $"「{share.Seat?.Code}」有空位了，请在{rules.WaitlistWindowMinutes}分钟内预约", null, ct);
    }
}
