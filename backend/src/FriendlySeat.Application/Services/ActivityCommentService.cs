using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

/// <summary>
/// 活动留言：用于给活动营造氛围、相互鼓励。内容限 50 字，做本地敏感词 + 微信内容安全校验；
/// 被举报后自动隐藏并进入后台审核。
/// </summary>
public class ActivityCommentService
{
    /// <summary>留言最大字数</summary>
    public const int MaxLength = 50;

    private readonly IAppDbContext _db;
    private readonly SensitiveWordService _sensitive;
    private readonly IWechatService _wechat;

    public ActivityCommentService(IAppDbContext db, SensitiveWordService sensitive, IWechatService wechat)
    {
        _db = db;
        _sensitive = sensitive;
        _wechat = wechat;
    }

    public async Task<List<ActivityCommentDto>> GetByActivityAsync(long activityId, long? viewerId, int take = 30, CancellationToken ct = default)
    {
        var list = await _db.ActivityComments
            .Include(c => c.User)
            .Where(c => c.ActivityId == activityId && c.Status == CommentStatus.Visible)
            .OrderByDescending(c => c.Id)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct);

        return list.Select(c => ToDto(c, viewerId)).ToList();
    }

    public async Task<ActivityCommentDto> CreateAsync(long userId, CreateActivityCommentRequest request, CancellationToken ct = default)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == request.ActivityId, ct)
            ?? throw AppException.NotFound("活动不存在");
        if (activity.Status != ActivityStatus.Published)
            throw AppException.BadRequest("activity_not_open", "该活动当前不可留言");

        var content = (request.Content ?? string.Empty).Trim();
        if (content.Length == 0)
            throw AppException.BadRequest("content_required", "请先写点什么");
        if (content.Length > MaxLength)
            throw AppException.BadRequest("content_too_long", $"留言最多 {MaxLength} 字");

        await EnsureContentSafeAsync(userId, content, ct);

        var comment = new ActivityComment
        {
            ActivityId = activity.Id,
            UserId = userId,
            Content = content,
            Status = CommentStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ActivityComments.Add(comment);
        await _db.SaveChangesAsync(ct);

        var saved = await _db.ActivityComments.Include(c => c.User).FirstAsync(c => c.Id == comment.Id, ct);
        return ToDto(saved, userId);
    }

    public async Task DeleteAsync(long userId, long id, CancellationToken ct = default)
    {
        var comment = await _db.ActivityComments.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw AppException.NotFound("留言不存在");
        if (comment.UserId != userId)
            throw AppException.Forbidden("只能删除自己的留言");

        _db.ActivityComments.Remove(comment);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>被举报：自动隐藏并进入审核</summary>
    public async Task HideByReportAsync(long id, CancellationToken ct = default)
    {
        var comment = await _db.ActivityComments.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (comment is null || comment.Status == CommentStatus.Hidden) return;
        comment.Status = CommentStatus.Hidden;
        comment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // ============ 管理端 ============

    public async Task<List<ActivityCommentDto>> AdminListAsync(string? status, CancellationToken ct = default)
    {
        var query = _db.ActivityComments.Include(c => c.User).Include(c => c.Activity).AsQueryable();
        if (Enum.TryParse<CommentStatus>(status, true, out var st))
        {
            query = query.Where(c => c.Status == st);
        }

        var list = await query
            .OrderByDescending(c => c.Id)
            .Take(200)
            .ToListAsync(ct);

        return list.Select(c =>
        {
            var dto = ToDto(c, null);
            dto.ActivityTitle = c.Activity?.Title;
            return dto;
        }).ToList();
    }

    /// <summary>审核：通过=恢复展示；驳回=删除</summary>
    public async Task AdminReviewAsync(long id, bool approve, CancellationToken ct = default)
    {
        var comment = await _db.ActivityComments.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw AppException.NotFound("留言不存在");

        if (approve)
        {
            comment.Status = CommentStatus.Visible;
            comment.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.ActivityComments.Remove(comment);
        }
        await _db.SaveChangesAsync(ct);
    }

    private static ActivityCommentDto ToDto(ActivityComment c, long? viewerId) => new()
    {
        Id = c.Id,
        ActivityId = c.ActivityId,
        Content = c.Content,
        OwnerName = c.User?.Nickname ?? "友邻",
        OwnerAvatar = c.User?.AvatarUrl,
        IsOwner = viewerId.HasValue && viewerId.Value == c.UserId,
        Status = c.Status.ToString(),
        CreatedAt = c.CreatedAt
    };

    private async Task EnsureContentSafeAsync(long userId, string content, CancellationToken ct)
    {
        var hit = await _sensitive.FirstHitAsync(new[] { content }, ct);
        if (hit is not null)
            throw AppException.BadRequest("content_sensitive", $"内容包含敏感词「{hit}」，请修改后重试");

        var openId = await _db.Users.Where(u => u.Id == userId).Select(u => u.OpenId).FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(openId)) return;
        var r = await _wechat.MsgSecCheckAsync(openId!, 2, content, ct);
        if (r.Status == ContentCheckStatus.Risky)
            throw AppException.BadRequest("content_risky", "内容未通过安全检测，请修改后重试");
    }
}
