using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class ReportService
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _notifications;

    public ReportService(IAppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
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
            else if (targetType == ReportTargetType.Activity)
            {
                var activity = await _db.Activities
                    .Where(a => a.Id == request.TargetId.Value)
                    .Select(a => new { a.CreatorUserId })
                    .FirstOrDefaultAsync(ct);
                if (activity is null)
                    throw AppException.NotFound("活动不存在");
                if (activity.CreatorUserId == reporterId)
                    throw AppException.BadRequest("cannot_report_own", "不能举报自己发布的活动");
                targetUserId = activity.CreatorUserId;
            }
            else if (targetType == ReportTargetType.BookListShare)
            {
                var share = await _db.BookListShares
                    .Where(s => s.Id == request.TargetId.Value)
                    .Select(s => new { s.UserId })
                    .FirstOrDefaultAsync(ct);
                if (share is null)
                    throw AppException.NotFound("书单不存在");
                if (share.UserId == reporterId)
                    throw AppException.BadRequest("cannot_report_own", "不能举报自己的书单");
                targetUserId = share.UserId;
            }
            else if (targetType == ReportTargetType.SeatNote)
            {
                var note = await _db.SeatNotes
                    .Where(n => n.Id == request.TargetId.Value)
                    .Select(n => new { n.UserId })
                    .FirstOrDefaultAsync(ct);
                if (note is null)
                    throw AppException.NotFound("便签不存在");
                if (note.UserId == reporterId)
                    throw AppException.BadRequest("cannot_report_own", "不能举报自己的便签");
                targetUserId = note.UserId;
            }
            else if (targetType == ReportTargetType.ActivityComment)
            {
                var comment = await _db.ActivityComments
                    .Where(c => c.Id == request.TargetId.Value)
                    .Select(c => new { c.UserId })
                    .FirstOrDefaultAsync(ct);
                if (comment is null)
                    throw AppException.NotFound("留言不存在");
                if (comment.UserId == reporterId)
                    throw AppException.BadRequest("cannot_report_own", "不能举报自己的留言");
                targetUserId = comment.UserId;
            }
            else if (targetType == ReportTargetType.VenuePost)
            {
                var post = await _db.VenuePosts
                    .Where(p => p.Id == request.TargetId.Value)
                    .Select(p => new { p.UserId })
                    .FirstOrDefaultAsync(ct);
                if (post is null)
                    throw AppException.NotFound("帖子不存在");
                if (post.UserId == reporterId)
                    throw AppException.BadRequest("cannot_report_own", "不能举报自己的帖子");
                targetUserId = post.UserId;
            }
            else if (targetType == ReportTargetType.VenuePostComment)
            {
                var comment = await _db.VenuePostComments
                    .Where(c => c.Id == request.TargetId.Value)
                    .Select(c => new { c.UserId })
                    .FirstOrDefaultAsync(ct);
                if (comment is null)
                    throw AppException.NotFound("评论不存在");
                if (comment.UserId == reporterId)
                    throw AppException.BadRequest("cannot_report_own", "不能举报自己的评论");
                targetUserId = comment.UserId;
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

        // 活动被举报：退回待审核（对其他用户不可见）
        if (targetType == ReportTargetType.Activity && request.TargetId.HasValue)
        {
            var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == request.TargetId.Value, ct);
            if (activity is not null && activity.Status == ActivityStatus.Published)
            {
                activity.Status = ActivityStatus.PendingReview;
                activity.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                await _notifications.SendAsync(activity.CreatorUserId, NotificationType.System,
                    "活动被举报，已退回审核",
                    $"你发布的活动「{activity.Title}」被举报，已暂时下架并退回审核。", null, ct);
            }
        }

        // 书单被举报：从热门榜下架（取消公开），已分享链接仍可用
        if (targetType == ReportTargetType.BookListShare && request.TargetId.HasValue)
        {
            var share = await _db.BookListShares.FirstOrDefaultAsync(s => s.Id == request.TargetId.Value, ct);
            if (share is not null && share.IsPublic)
            {
                share.IsPublic = false;
                await _db.SaveChangesAsync(ct);
                await _notifications.SendAsync(share.UserId, NotificationType.System,
                    "书单被举报，已从热门榜下架",
                    $"你的书单「{share.Title}」被举报，已暂时从热门书单榜下架。", null, ct);
            }
        }

        // 便签被举报：立即隐藏，进入后台审核
        if (targetType == ReportTargetType.SeatNote && request.TargetId.HasValue)
        {
            var note = await _db.SeatNotes.FirstOrDefaultAsync(n => n.Id == request.TargetId.Value, ct);
            if (note is not null && note.Status == SeatNoteStatus.Visible)
            {
                note.Status = SeatNoteStatus.Hidden;
                note.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                await _notifications.SendAsync(note.UserId, NotificationType.System,
                    "便签被举报，已暂时隐藏",
                    "你发布的座位便签被举报，已暂时隐藏并进入审核。", null, ct);
            }
        }

        // 活动留言被举报：立即隐藏，进入后台审核
        if (targetType == ReportTargetType.ActivityComment && request.TargetId.HasValue)
        {
            var comment = await _db.ActivityComments.FirstOrDefaultAsync(c => c.Id == request.TargetId.Value, ct);
            if (comment is not null && comment.Status == CommentStatus.Visible)
            {
                comment.Status = CommentStatus.Hidden;
                comment.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                await _notifications.SendAsync(comment.UserId, NotificationType.System,
                    "留言被举报，已暂时隐藏",
                    "你在活动下的留言被举报，已暂时隐藏并进入审核。", null, ct);
            }
        }

        // 场馆交流帖 / 评论被举报：立即隐藏，进入后台审核
        if (targetType == ReportTargetType.VenuePost && request.TargetId.HasValue)
        {
            var post = await _db.VenuePosts.FirstOrDefaultAsync(p => p.Id == request.TargetId.Value, ct);
            if (post is not null && post.Status == CommentStatus.Visible)
            {
                post.Status = CommentStatus.Hidden;
                post.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                await _notifications.SendAsync(post.UserId, NotificationType.System,
                    "帖子被举报，已暂时隐藏",
                    "你在场馆交流板发布的帖子被举报，已暂时隐藏并进入审核。", null, ct);
            }
        }
        if (targetType == ReportTargetType.VenuePostComment && request.TargetId.HasValue)
        {
            var comment = await _db.VenuePostComments.FirstOrDefaultAsync(c => c.Id == request.TargetId.Value, ct);
            if (comment is not null && comment.Status == CommentStatus.Visible)
            {
                comment.Status = CommentStatus.Hidden;
                await _db.SaveChangesAsync(ct);
                await _notifications.SendAsync(comment.UserId, NotificationType.System,
                    "评论被举报，已暂时隐藏",
                    "你在场馆交流板的评论被举报，已暂时隐藏并进入审核。", null, ct);
            }
        }

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
