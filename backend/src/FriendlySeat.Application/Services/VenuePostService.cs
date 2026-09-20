using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

/// <summary>
/// 场馆交流板：按场馆组织的学习交流与信息共享（公开帖、无私信、无联系方式）。
/// 内容做本地敏感词 + 微信内容安全校验；被举报自动隐藏并进入后台审核。
/// </summary>
public class VenuePostService
{
    public const int MaxTitleLength = 50;
    public const int MaxContentLength = 1000;
    public const int MaxCommentLength = 200;

    private readonly IAppDbContext _db;
    private readonly SensitiveWordService _sensitive;
    private readonly IWechatService _wechat;
    private readonly ConfigOptionsService _configOptions;

    public VenuePostService(IAppDbContext db, SensitiveWordService sensitive, IWechatService wechat, ConfigOptionsService configOptions)
    {
        _db = db;
        _sensitive = sensitive;
        _wechat = wechat;
        _configOptions = configOptions;
    }

    public async Task<List<VenuePostDto>> GetListAsync(long venueId, string? category, string? sort, long? viewerId, int take = 30, CancellationToken ct = default)
    {
        var query = _db.VenuePosts
            .Include(p => p.User)
            .Where(p => p.VenueId == venueId && p.Status == CommentStatus.Visible);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var codes = (await _configOptions.GetVenuePostCategoriesAsync(ct)).Select(c => c.Code).ToHashSet();
            if (codes.Contains(category)) query = query.Where(p => p.Category == category);
        }

        var hot = string.Equals(sort, "hot", StringComparison.OrdinalIgnoreCase);
        var list = await (hot
                ? query.OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.LikeCount * 2 + p.CommentCount * 3).ThenByDescending(p => p.Id)
                : query.OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.Id))
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct);

        var labels = await CategoryLabelsAsync(ct);
        var likedIds = viewerId.HasValue
            ? (await _db.VenuePostLikes.Where(l => l.UserId == viewerId.Value && list.Select(p => p.Id).Contains(l.PostId))
                .Select(l => l.PostId).ToListAsync(ct)).ToHashSet()
            : new HashSet<long>();

        return list.Select(p => ToDto(p, viewerId, labels, likedIds.Contains(p.Id))).ToList();
    }

    /// <summary>某用户的公开帖子（访客主页展示）</summary>
    public async Task<List<VenuePostDto>> GetPublicByUserAsync(long userId, long? viewerId, int take = 20, CancellationToken ct = default)
    {
        var list = await _db.VenuePosts.Include(p => p.User).Include(p => p.Venue)
            .Where(p => p.UserId == userId && p.Status == CommentStatus.Visible)
            .OrderByDescending(p => p.Id)
            .Take(Math.Clamp(take, 1, 50))
            .ToListAsync(ct);

        var labels = await CategoryLabelsAsync(ct);
        var likedIds = viewerId.HasValue
            ? (await _db.VenuePostLikes.Where(l => l.UserId == viewerId.Value && list.Select(p => p.Id).Contains(l.PostId))
                .Select(l => l.PostId).ToListAsync(ct)).ToHashSet()
            : new HashSet<long>();

        return list.Select(p => ToDto(p, viewerId, labels, likedIds.Contains(p.Id))).ToList();
    }

    public async Task<VenuePostDetailDto?> GetDetailAsync(long id, long? viewerId, CancellationToken ct = default)
    {
        var post = await _db.VenuePosts.Include(p => p.User).Include(p => p.Venue)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null || (post.Status != CommentStatus.Visible && post.UserId != viewerId))
            return null;

        var tops = await _db.VenuePostComments.Include(c => c.User)
            .Where(c => c.PostId == id && c.Status == CommentStatus.Visible && c.ParentCommentId == null)
            .OrderBy(c => c.Id)
            .Take(200)
            .ToListAsync(ct);

        var topIds = tops.Select(t => t.Id).ToList();
        var replies = await _db.VenuePostComments.Include(c => c.User)
            .Where(c => c.PostId == id && c.Status == CommentStatus.Visible
                && c.ParentCommentId != null && topIds.Contains(c.ParentCommentId.Value))
            .OrderBy(c => c.Id)
            .Take(800)
            .ToListAsync(ct);

        var replyToIds = replies.Where(c => c.ReplyToUserId.HasValue).Select(c => c.ReplyToUserId!.Value).Distinct().ToList();
        var nameMap = replyToIds.Count == 0
            ? new Dictionary<long, string>()
            : await _db.Users.Where(u => replyToIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Nickname })
                .ToDictionaryAsync(x => x.Id, x => string.IsNullOrWhiteSpace(x.Nickname) ? "友邻" : x.Nickname!, ct);

        var repliesByParent = replies.GroupBy(r => r.ParentCommentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).ToList());

        var labels = await CategoryLabelsAsync(ct);
        var liked = viewerId.HasValue
            && await _db.VenuePostLikes.AnyAsync(l => l.PostId == id && l.UserId == viewerId.Value, ct);

        return new VenuePostDetailDto
        {
            Post = ToDto(post, viewerId, labels, liked),
            Comments = tops.Select(t =>
            {
                var dto = ToCommentDto(t, viewerId, nameMap);
                dto.Replies = repliesByParent.TryGetValue(t.Id, out var rs)
                    ? rs.Select(r => ToCommentDto(r, viewerId, nameMap)).ToList()
                    : new List<VenuePostCommentDto>();
                return dto;
            }).ToList()
        };
    }

    public async Task<VenuePostDto> CreateAsync(long userId, CreateVenuePostRequest request, CancellationToken ct = default)
    {
        var venue = await _db.Venues.FirstOrDefaultAsync(v => v.Id == request.VenueId, ct)
            ?? throw AppException.NotFound("场馆不存在");

        var codes = (await _configOptions.GetVenuePostCategoriesAsync(ct)).Select(c => c.Code).ToHashSet();
        var category = codes.Contains(request.Category) ? request.Category : "chat";

        var title = (request.Title ?? string.Empty).Trim();
        var content = (request.Content ?? string.Empty).Trim();
        if (title.Length == 0)
            throw AppException.BadRequest("title_required", "请填写标题");
        if (title.Length > MaxTitleLength)
            throw AppException.BadRequest("title_too_long", $"标题最多 {MaxTitleLength} 字");
        if (content.Length == 0)
            throw AppException.BadRequest("content_required", "请填写内容");
        if (content.Length > MaxContentLength)
            throw AppException.BadRequest("content_too_long", $"内容最多 {MaxContentLength} 字");

        await EnsureContentSafeAsync(userId, new[] { title, content }, ct);

        // 封面图内容安全（imgSecCheck，需临时可访问地址）
        if (!string.IsNullOrWhiteSpace(request.CoverImageUrl))
        {
            var r = await _wechat.ImgSecCheckUrlAsync(request.CoverImageUrl!, ct);
            if (r.Status == ContentCheckStatus.Risky)
                throw AppException.BadRequest("image_risky", "图片未通过安全检测，请更换后重试");
        }

        var post = new VenuePost
        {
            VenueId = venue.Id,
            UserId = userId,
            Category = category,
            Title = title,
            Content = content,
            CoverImage = string.IsNullOrWhiteSpace(request.CoverImage) ? null : request.CoverImage!.Trim(),
            Status = CommentStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.VenuePosts.Add(post);
        await _db.SaveChangesAsync(ct);

        var saved = await _db.VenuePosts.Include(p => p.User).Include(p => p.Venue).FirstAsync(p => p.Id == post.Id, ct);
        var labels = await CategoryLabelsAsync(ct);
        return ToDto(saved, userId, labels, false);
    }

    public async Task<VenuePostDto> ToggleLikeAsync(long userId, long postId, CancellationToken ct = default)
    {
        var post = await _db.VenuePosts.Include(p => p.User).Include(p => p.Venue)
            .FirstOrDefaultAsync(p => p.Id == postId, ct)
            ?? throw AppException.NotFound("帖子不存在");
        if (post.Status != CommentStatus.Visible)
            throw AppException.BadRequest("post_hidden", "该帖子不可互动");

        var like = await _db.VenuePostLikes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, ct);
        var liked = like is null;
        if (like is null)
        {
            _db.VenuePostLikes.Add(new VenuePostLike { PostId = postId, UserId = userId, CreatedAt = DateTime.UtcNow });
            post.LikeCount += 1;
        }
        else
        {
            _db.VenuePostLikes.Remove(like);
            post.LikeCount = Math.Max(0, post.LikeCount - 1);
        }
        await _db.SaveChangesAsync(ct);

        var labels = await CategoryLabelsAsync(ct);
        return ToDto(post, userId, labels, liked);
    }

    public async Task<VenuePostCommentDto> AddCommentAsync(long userId, long postId, CreateVenuePostCommentRequest request, CancellationToken ct = default)
    {
        var post = await _db.VenuePosts.FirstOrDefaultAsync(p => p.Id == postId, ct)
            ?? throw AppException.NotFound("帖子不存在");
        if (post.Status != CommentStatus.Visible)
            throw AppException.BadRequest("post_hidden", "该帖子不可评论");

        var content = (request.Content ?? string.Empty).Trim();
        if (content.Length == 0)
            throw AppException.BadRequest("content_required", "请填写评论内容");
        if (content.Length > MaxCommentLength)
            throw AppException.BadRequest("content_too_long", $"评论最多 {MaxCommentLength} 字");

        await EnsureContentSafeAsync(userId, new[] { content }, ct);

        // 回复：ParentCommentId 指向一级评论（回复"回复"时归并到同一楼）
        long? rootId = null;
        long? replyToUserId = null;
        if (request.ParentCommentId is long parentId && parentId > 0)
        {
            var parent = await _db.VenuePostComments.FirstOrDefaultAsync(c => c.Id == parentId, ct)
                ?? throw AppException.NotFound("评论不存在");
            if (parent.PostId != postId)
                throw AppException.BadRequest("parent_mismatch", "回复的评论不属于该帖子");
            rootId = parent.ParentCommentId ?? parent.Id;
            replyToUserId = parent.UserId;
        }

        var comment = new VenuePostComment
        {
            PostId = postId,
            UserId = userId,
            Content = content,
            ParentCommentId = rootId,
            ReplyToUserId = replyToUserId,
            Status = CommentStatus.Visible,
            CreatedAt = DateTime.UtcNow
        };
        _db.VenuePostComments.Add(comment);
        post.CommentCount += 1;
        await _db.SaveChangesAsync(ct);

        var saved = await _db.VenuePostComments.Include(c => c.User).FirstAsync(c => c.Id == comment.Id, ct);
        Dictionary<long, string>? nameMap = null;
        if (saved.ReplyToUserId.HasValue)
        {
            var name = await _db.Users.Where(u => u.Id == saved.ReplyToUserId.Value).Select(u => u.Nickname).FirstOrDefaultAsync(ct);
            nameMap = new Dictionary<long, string> { [saved.ReplyToUserId.Value] = string.IsNullOrWhiteSpace(name) ? "友邻" : name };
        }
        return ToCommentDto(saved, userId, nameMap);
    }

    public async Task DeletePostAsync(long userId, long postId, CancellationToken ct = default)
    {
        var post = await _db.VenuePosts.FirstOrDefaultAsync(p => p.Id == postId, ct)
            ?? throw AppException.NotFound("帖子不存在");
        if (post.UserId != userId)
            throw AppException.Forbidden("只能删除自己的帖子");

        _db.VenuePosts.Remove(post);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteCommentAsync(long userId, long commentId, CancellationToken ct = default)
    {
        var comment = await _db.VenuePostComments.FirstOrDefaultAsync(c => c.Id == commentId, ct)
            ?? throw AppException.NotFound("评论不存在");
        if (comment.UserId != userId)
            throw AppException.Forbidden("只能删除自己的评论");

        var post = await _db.VenuePosts.FirstOrDefaultAsync(p => p.Id == comment.PostId, ct);

        // 删一级评论：级联删除其下回复
        var removed = 1;
        if (comment.ParentCommentId == null)
        {
            var replies = await _db.VenuePostComments.Where(c => c.ParentCommentId == comment.Id).ToListAsync(ct);
            removed += replies.Count;
            _db.VenuePostComments.RemoveRange(replies);
        }
        if (post is not null) post.CommentCount = Math.Max(0, post.CommentCount - removed);

        _db.VenuePostComments.Remove(comment);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>帖子被举报：自动隐藏</summary>
    public async Task HideByReportAsync(long postId, CancellationToken ct = default)
    {
        var post = await _db.VenuePosts.FirstOrDefaultAsync(p => p.Id == postId, ct);
        if (post is null || post.Status == CommentStatus.Hidden) return;
        post.Status = CommentStatus.Hidden;
        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>评论被举报：自动隐藏（一级评论连同其回复一起隐藏）</summary>
    public async Task HideCommentByReportAsync(long commentId, CancellationToken ct = default)
    {
        var comment = await _db.VenuePostComments.FirstOrDefaultAsync(c => c.Id == commentId, ct);
        if (comment is null || comment.Status == CommentStatus.Hidden) return;

        if (comment.ParentCommentId == null)
        {
            var replies = await _db.VenuePostComments
                .Where(c => c.ParentCommentId == comment.Id && c.Status != CommentStatus.Hidden)
                .ToListAsync(ct);
            foreach (var r in replies) r.Status = CommentStatus.Hidden;
        }
        comment.Status = CommentStatus.Hidden;
        await _db.SaveChangesAsync(ct);
    }

    // ============ 管理端 ============

    public async Task<List<VenuePostDto>> AdminListAsync(string? status, CancellationToken ct = default)
    {
        var query = _db.VenuePosts.Include(p => p.User).Include(p => p.Venue).AsQueryable();
        if (Enum.TryParse<CommentStatus>(status, true, out var st))
        {
            query = query.Where(p => p.Status == st);
        }

        var list = await query.OrderByDescending(p => p.Id).Take(200).ToListAsync(ct);
        var labels = await CategoryLabelsAsync(ct);
        return list.Select(p => ToDto(p, null, labels, false)).ToList();
    }

    public async Task AdminReviewAsync(long id, bool approve, long operatorId, CancellationToken ct = default)
    {
        var post = await _db.VenuePosts.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw AppException.NotFound("帖子不存在");
        if (approve)
        {
            post.Status = CommentStatus.Visible;
            post.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.VenuePosts.Remove(post);
        }
        Audit(operatorId, "venue_post.review", "VenuePost", id, approve ? "审核通过" : "驳回删除");
        await _db.SaveChangesAsync(ct);
    }

    public async Task AdminPinAsync(long id, bool pinned, long operatorId, CancellationToken ct = default)
    {
        var post = await _db.VenuePosts.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw AppException.NotFound("帖子不存在");
        post.IsPinned = pinned;
        post.UpdatedAt = DateTime.UtcNow;
        Audit(operatorId, "venue_post.pin", "VenuePost", id, pinned ? "置顶" : "取消置顶");
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<VenuePostCommentDto>> AdminCommentListAsync(string? status, CancellationToken ct = default)
    {
        var query = _db.VenuePostComments.Include(c => c.User).AsQueryable();
        if (Enum.TryParse<CommentStatus>(status, true, out var st))
        {
            query = query.Where(c => c.Status == st);
        }
        var list = await query.OrderByDescending(c => c.Id).Take(200).ToListAsync(ct);

        var replyToIds = list.Where(c => c.ReplyToUserId.HasValue).Select(c => c.ReplyToUserId!.Value).Distinct().ToList();
        var nameMap = replyToIds.Count == 0
            ? new Dictionary<long, string>()
            : await _db.Users.Where(u => replyToIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Nickname })
                .ToDictionaryAsync(x => x.Id, x => string.IsNullOrWhiteSpace(x.Nickname) ? "友邻" : x.Nickname!, ct);

        return list.Select(c => ToCommentDto(c, null, nameMap)).ToList();
    }

    public async Task AdminCommentReviewAsync(long id, bool approve, long operatorId, CancellationToken ct = default)
    {
        var comment = await _db.VenuePostComments.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw AppException.NotFound("评论不存在");

        var post = await _db.VenuePosts.FirstOrDefaultAsync(p => p.Id == comment.PostId, ct);

        if (approve)
        {
            comment.Status = CommentStatus.Visible;
            // 一级评论通过时，恢复因举报被连带隐藏的回复
            if (comment.ParentCommentId == null)
            {
                var replies = await _db.VenuePostComments
                    .Where(c => c.ParentCommentId == comment.Id && c.Status != CommentStatus.Visible)
                    .ToListAsync(ct);
                foreach (var r in replies) r.Status = CommentStatus.Visible;
            }
        }
        else
        {
            var removed = 1;
            if (comment.ParentCommentId == null)
            {
                var replies = await _db.VenuePostComments.Where(c => c.ParentCommentId == comment.Id).ToListAsync(ct);
                removed += replies.Count;
                _db.VenuePostComments.RemoveRange(replies);
            }
            if (post is not null) post.CommentCount = Math.Max(0, post.CommentCount - removed);
            _db.VenuePostComments.Remove(comment);
        }
        Audit(operatorId, "venue_post_comment.review", "VenuePostComment", id, approve ? "审核通过" : "驳回删除");
        await _db.SaveChangesAsync(ct);
    }

    private void Audit(long operatorId, string action, string entityType, long entityId, string detail)
    {
        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminUserId = operatorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            Detail = detail,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task<Dictionary<string, string>> CategoryLabelsAsync(CancellationToken ct)
    {
        var list = await _configOptions.GetVenuePostCategoriesAsync(ct);
        return list.ToDictionary(c => c.Code, c => c.Label);
    }

    private static VenuePostDto ToDto(VenuePost p, long? viewerId, Dictionary<string, string> labels, bool liked) => new()
    {
        Id = p.Id,
        VenueId = p.VenueId,
        VenueName = p.Venue?.Name,
        Category = p.Category,
        CategoryLabel = labels.TryGetValue(p.Category, out var label) ? label : p.Category,
        Title = p.Title,
        Content = p.Content,
        CoverImage = p.CoverImage,
        OwnerId = p.UserId,
        OwnerName = p.User?.Nickname ?? "友邻",
        OwnerAvatar = p.User?.AvatarUrl,
        IsOwner = viewerId.HasValue && viewerId.Value == p.UserId,
        IsPinned = p.IsPinned,
        Liked = liked,
        LikeCount = p.LikeCount,
        CommentCount = p.CommentCount,
        Status = p.Status.ToString(),
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };

    private static VenuePostCommentDto ToCommentDto(VenuePostComment c, long? viewerId, Dictionary<long, string>? nameMap = null) => new()
    {
        Id = c.Id,
        PostId = c.PostId,
        Content = c.Content,
        OwnerId = c.UserId,
        OwnerName = c.User?.Nickname ?? "友邻",
        OwnerAvatar = c.User?.AvatarUrl,
        IsOwner = viewerId.HasValue && viewerId.Value == c.UserId,
        Status = c.Status.ToString(),
        CreatedAt = c.CreatedAt,
        ParentCommentId = c.ParentCommentId,
        ReplyToUserId = c.ReplyToUserId,
        ReplyToName = c.ReplyToUserId.HasValue && nameMap is not null && nameMap.TryGetValue(c.ReplyToUserId.Value, out var n) ? n : null
    };

    private async Task EnsureContentSafeAsync(long userId, IEnumerable<string?> texts, CancellationToken ct)
    {
        var list = texts.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t!).ToList();

        var hit = await _sensitive.FirstHitAsync(list, ct);
        if (hit is not null)
            throw AppException.BadRequest("content_sensitive", $"内容包含敏感词「{hit}」，请修改后重试");

        var openId = await _db.Users.Where(u => u.Id == userId).Select(u => u.OpenId).FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(openId)) return;
        foreach (var text in list)
        {
            var r = await _wechat.MsgSecCheckAsync(openId!, 2, text, ct);
            if (r.Status == ContentCheckStatus.Risky)
                throw AppException.BadRequest("content_risky", "内容未通过安全检测，请修改后重试");
        }
    }
}
