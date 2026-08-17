using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class ReportService
{
    private readonly IAppDbContext _db;

    public ReportService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<ReportDto> CreateAsync(long reporterId, ReportCreateRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ReportTargetType>(request.TargetType, true, out var targetType))
            throw AppException.BadRequest("target_type_invalid", "举报对象类型无效");

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw AppException.BadRequest("reason_required", "请填写举报原因");

        // 自动定位被举报人：Share → 分享者；Reservation → 预约者
        long? targetUserId = request.TargetUserId;
        if (!targetUserId.HasValue && request.TargetId.HasValue)
        {
            if (targetType == ReportTargetType.Share)
            {
                targetUserId = await _db.SeatShares
                    .Where(s => s.Id == request.TargetId.Value)
                    .Select(s => (long?)s.OwnerUserId)
                    .FirstOrDefaultAsync(ct);
            }
            else if (targetType == ReportTargetType.Reservation)
            {
                targetUserId = await _db.Reservations
                    .Where(r => r.Id == request.TargetId.Value)
                    .Select(r => (long?)r.UserId)
                    .FirstOrDefaultAsync(ct);
            }
        }

        var report = new Report
        {
            ReporterUserId = reporterId,
            TargetUserId = targetUserId,
            TargetType = targetType,
            TargetId = request.TargetId,
            Reason = request.Reason,
            Description = request.Description,
            EvidenceUrl = request.EvidenceUrl,
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _db.Reports.Add(report);
        await _db.SaveChangesAsync(ct);

        // 保存后重新查询，带出举报人/被举报人昵称
        return await _db.Reports
            .Where(r => r.Id == report.Id)
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
            .FirstAsync(ct);
    }

    public async Task<List<ReportDto>> GetMyAsync(long userId, CancellationToken ct = default)
    {
        return await _db.Reports
            .Where(r => r.ReporterUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
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

    public async Task HandleAsync(long reportId, long adminId, ReportStatus status, string? note, CancellationToken ct = default)
    {
        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw AppException.NotFound("举报不存在");

        report.Status = status;
        report.HandledBy = adminId;
        report.HandledAt = DateTime.UtcNow;
        report.HandleNote = note;

        await _db.SaveChangesAsync(ct);
    }

}
