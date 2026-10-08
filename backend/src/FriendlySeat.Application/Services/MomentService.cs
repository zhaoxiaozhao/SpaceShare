using FriendlySeat.Application.Common;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FriendlySeat.Application.Services;

/// <summary>
/// 用户行为自动生成交流板帖子：把用户在平台上产生的行为（分享座位/换座/写便签/
/// 开始阅读/到馆打卡）自动生成一条交流板帖子，展示在对应场馆的交流板（含「全部」）。
/// 生成前检查公开开关 + 本地敏感词 + 频控去重；发布失败只记日志，绝不影响原始业务。
/// </summary>
public class MomentService
{
    /// <summary>同用户两条自动帖的最小间隔</summary>
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(60);

    /// <summary>同用户每小时自动帖上限</summary>
    private const int HourlyLimit = 20;

    /// <summary>去重窗口：同类型同源对象在此窗口内不重复发帖</summary>
    private static readonly TimeSpan DedupWindow = TimeSpan.FromMinutes(30);

    private const string AutoCategory = "moment";

    private readonly IAppDbContext _db;
    private readonly SensitiveWordService _sensitive;
    private readonly ILogger<MomentService> _logger;

    public MomentService(IAppDbContext db, SensitiveWordService sensitive, ILogger<MomentService> logger)
    {
        _db = db;
        _sensitive = sensitive;
        _logger = logger;
    }

    private static string TypeTitle(string type) => type switch
    {
        "seat_share" => "分享了座位",
        "swap" => "发起了换座",
        "seat_note" => "留下了便签",
        "reading" => "正在阅读",
        "check_in" => "到馆打卡",
        "read_note" => "读书笔记",
        "read_highlight" => "书中摘抄",
        _ => "动态"
    };

    /// <summary>
    /// 生成一条自动交流帖。任何拦截（未开公开、无场馆、敏感词、频控、去重）均静默返回 false，
    /// 调用方无需关心结果。
    /// </summary>
    public async Task<bool> PublishAsync(long userId, long? venueId, string? venueName, string type, string content, string? targetKey = null, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            if (!venueId.HasValue || venueId.Value <= 0) return false;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return false;
            if (!user.MomentsPublic) return false;

            // 本地敏感词兜底（内容中可能含用户填写文字，如便签全文）
            if (await _sensitive.FirstHitAsync(new[] { content }, ct) is not null) return false;

            var now = DateTime.UtcNow;

            // 频控：同用户最小间隔 + 每小时条数上限（仅统计自动帖）
            var last = await _db.VenuePosts
                .Where(p => p.UserId == userId && p.IsAuto)
                .OrderByDescending(p => p.Id)
                .Select(p => new { p.CreatedAt })
                .FirstOrDefaultAsync(ct);
            if (last is not null && now - last.CreatedAt < MinInterval) return false;

            var hourAgo = now.AddHours(-1);
            var hourCount = await _db.VenuePosts.CountAsync(p => p.UserId == userId && p.IsAuto && p.CreatedAt >= hourAgo, ct);
            if (hourCount >= HourlyLimit) return false;

            // 去重：同源对象在窗口内只发一次
            if (!string.IsNullOrEmpty(targetKey))
            {
                var dup = await _db.VenuePosts.AnyAsync(
                    p => p.UserId == userId && p.IsAuto && p.TargetKey == targetKey && p.CreatedAt >= now - DedupWindow, ct);
                if (dup) return false;
            }

            _db.VenuePosts.Add(new VenuePost
            {
                VenueId = venueId.Value,
                UserId = userId,
                Category = AutoCategory,
                Title = TypeTitle(type),
                Content = content.Trim(),
                Status = CommentStatus.Visible,
                IsAuto = true,
                TargetKey = targetKey,
                CreatedAt = now,
                UpdatedAt = now
            });
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            // 自动发帖失败不影响原始业务
            _logger.LogWarning(ex, "生成用户动态帖失败 userId={UserId} type={Type}", userId, type);
            return false;
        }
    }

    /// <summary>按源对象级联删除（如便签被隐藏/删除时同步删除其自动帖）</summary>
    public async Task DeleteByTargetAsync(string type, string targetKey, CancellationToken ct = default)
    {
        var posts = await _db.VenuePosts
            .Where(p => p.IsAuto && p.TargetKey == targetKey)
            .ToListAsync(ct);
        if (posts.Count == 0) return;
        _db.VenuePosts.RemoveRange(posts);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>覆盖已存在自动帖的文案（如便签内容更新后同步刷新）</summary>
    public async Task UpdateContentForTargetAsync(string type, string targetKey, string content, CancellationToken ct = default)
    {
        try
        {
            var post = await _db.VenuePosts
                .FirstOrDefaultAsync(p => p.IsAuto && p.TargetKey == targetKey, ct);
            if (post is null) return;
            if (await _sensitive.FirstHitAsync(new[] { content }, ct) is not null) return;
            post.Content = content.Trim();
            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新动态帖文案失败 type={Type} target={Target}", type, targetKey);
        }
    }

    /// <summary>
    /// 解析座位上下文（场馆/区块/座位号），供分享、换座、便签、到馆等动态生成使用。
    /// </summary>
    public async Task<(long? VenueId, string VenueName, string ZoneName, string SeatCode)?> ResolveSeatAsync(long seatId, CancellationToken ct = default)
    {
        var ctx = await _db.Seats
            .Where(s => s.Id == seatId)
            .Select(s => new
            {
                SeatCode = s.Code,
                ZoneName = s.Zone!.Name,
                VenueId = s.Zone!.Floor!.VenueId,
                VenueName = s.Zone!.Floor!.Venue!.Name
            })
            .FirstOrDefaultAsync(ct);
        if (ctx is null) return null;
        return (ctx.VenueId, ctx.VenueName, ctx.ZoneName, ctx.SeatCode);
    }
}
