using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class AdminReportService
{
    private readonly IAppDbContext _db;
    private readonly IAuditService _audit;
    private readonly ConfigService _config;
    private readonly INotificationService _notifications;

    public AdminReportService(IAppDbContext db, IAuditService audit, ConfigService config, INotificationService notifications)
    {
        _db = db;
        _audit = audit;
        _config = config;
        _notifications = notifications;
    }

    public async Task<List<ReportDto>> GetReportsAsync(string? status, CancellationToken ct = default)
    {
        var query = _db.Reports.AsQueryable();
        if (Enum.TryParse<ReportStatus>(status, true, out var parsed))
        {
            query = query.Where(r => r.Status == parsed);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(200)
            .Select(r => new ReportDto
            {
                Id = r.Id,
                ReporterUserId = r.ReporterUserId,
                ReporterNickname = r.ReporterUser!.Nickname,
                TargetUserId = r.TargetUserId,
                TargetUserNickname = r.TargetUser != null ? r.TargetUser.Nickname : null,
                TargetType = r.TargetType.ToString(),
                TargetId = r.TargetId,
                Reason = r.Reason,
                Description = r.Description,
                EvidenceUrl = r.EvidenceUrl,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task HandleAsync(long reportId, ReportStatus status, string? note, long operatorId, CancellationToken ct = default)
    {
        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw AppException.NotFound("举报不存在");

        report.Status = status;
        report.HandledBy = operatorId;
        report.HandledAt = DateTime.UtcNow;
        report.HandleNote = note;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(operatorId, "report.handle", "Report", reportId.ToString(), $"处理举报，状态={status}，备注={note}", null, ct);

        // 联动处罚（分值走配置）
        if (report.TargetUserId.HasValue)
        {
            var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == report.TargetUserId.Value, ct);
            if (target is not null)
            {
                var creditRules = await _config.GetCreditRulesAsync(ct);

                if (status == ReportStatus.CreditDeducted)
                {
                    var penalty = report.Reason.Contains("交易") ? creditRules.TransactionPenalty
                        : report.Reason.Contains("虚假") || report.Reason.Contains("不存在") ? creditRules.FakeSeatPenalty
                        : report.Reason.Contains("占座") ? creditRules.MaliciousHoldPenalty
                        : -10;
                    target.CreditScore = Math.Max(0, target.CreditScore + penalty);
                    target.UpdatedAt = DateTime.UtcNow;
                    _db.CreditTransactions.Add(new CreditTransaction
                    {
                        UserId = target.Id,
                        Change = penalty,
                        Reason = $"举报处罚：{report.Reason}",
                        ReferenceType = "report",
                        ReferenceId = report.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else if (status == ReportStatus.Banned)
                {
                    target.Status = UserStatus.Banned;
                    target.UpdatedAt = DateTime.UtcNow;
                }
                else if (status == ReportStatus.AccountRestricted)
                {
                    target.RiskScore = Math.Min(100, target.RiskScore + 20);
                    target.UpdatedAt = DateTime.UtcNow;
                    _db.RiskEvents.Add(new RiskEvent
                    {
                        UserId = target.Id,
                        EventType = "abuse_report",
                        RiskScore = 20,
                        Metadata = $"举报处罚：{report.Reason}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await _db.SaveChangesAsync(ct);

                // 通知被举报人处理结果
                var message = status switch
                {
                    ReportStatus.Ignored => "你的举报已被忽略，感谢反馈。",
                    ReportStatus.Warned => "你被举报的行为已收到警告，请遵守平台规则。",
                    ReportStatus.CreditDeducted => "你因被举报的行为被扣除信用分，请注意守约。",
                    ReportStatus.Banned => "你因严重违规已被封禁账号。",
                    ReportStatus.AccountRestricted => "你因多次违规被限制部分功能。",
                    _ => "你的举报已处理。"
                };
                await _notifications.SendAsync(report.TargetUserId.Value, NotificationType.ReportResult,
                    "举报处理结果", message, null, ct);
            }
        }
    }

    public async Task<List<AdminReservationDto>> GetReservationsAsync(string? status, CancellationToken ct = default)
    {
        var query = _db.Reservations.Include(r => r.Seat).AsQueryable();
        if (Enum.TryParse<ReservationStatus>(status, true, out var parsed))
        {
            query = query.Where(r => r.Status == parsed);
        }

        return await query
            .OrderByDescending(r => r.ReservedAt)
            .Take(200)
            .Select(r => new AdminReservationDto
            {
                Id = r.Id,
                SeatId = r.SeatId,
                SeatCode = r.Seat!.Code,
                UserId = r.UserId,
                StartAt = r.StartAt,
                EndAt = r.EndAt,
                Status = r.Status.ToString(),
                ReservedAt = r.ReservedAt,
                ArrivedAt = r.ArrivedAt,
                CancelledAt = r.CancelledAt,
                CompletedAt = r.CompletedAt,
                ExpiredAt = r.ExpiredAt
            })
            .ToListAsync(ct);
    }

    public async Task ForceCancelAsync(long reservationId, string? reason, long operatorId, CancellationToken ct = default)
    {
        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId, ct)
            ?? throw AppException.NotFound("预约不存在");

        if (reservation.Status == ReservationStatus.Reserved)
        {
            reservation.Status = ReservationStatus.Cancelled;
            reservation.CancelledAt = DateTime.UtcNow;

            if (reservation.ShareId.HasValue)
            {
                var share = await _db.SeatShares.FirstOrDefaultAsync(s => s.Id == reservation.ShareId.Value, ct);
                if (share is not null && share.Status == SeatShareStatus.Reserved)
                {
                    share.Status = SeatShareStatus.Available;
                }
            }
        }
        else if (reservation.Status == ReservationStatus.Arrived || reservation.Status == ReservationStatus.Using)
        {
            reservation.Status = ReservationStatus.Completed;
            reservation.CompletedAt = DateTime.UtcNow;

            var seat = await _db.Seats.FirstAsync(s => s.Id == reservation.SeatId, ct);
            seat.Status = SeatStatus.Available;
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "reservation.force_cancel", "Reservation", reservationId.ToString(), $"强制取消预约，原因={reason}", null, ct);
    }

    public async Task<List<AdminAuditLogDto>> GetAuditLogsAsync(CancellationToken ct = default)
    {
        return await _db.AdminAuditLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(200)
            .Select(l => new AdminAuditLogDto
            {
                Id = l.Id,
                AdminUserId = l.AdminUserId,
                Action = l.Action,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Detail = l.Detail,
                IpAddress = l.IpAddress,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync(ct);
    }
}

public class AdminReservationDto
{
    public long Id { get; set; }
    public long SeatId { get; set; }
    public string SeatCode { get; set; } = string.Empty;
    public long UserId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReservedAt { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
}

public class AdminAuditLogDto
{
    public long Id { get; set; }
    public long AdminUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
