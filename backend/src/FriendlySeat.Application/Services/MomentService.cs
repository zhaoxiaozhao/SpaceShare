using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FriendlySeat.Application.Services;

/// <summary>
/// 用户动态（社区新鲜事）：把用户在平台上产生的行为自动生成一条轻量动态，
/// 展示在交流板「动态」标签。生成前检查公开开关 + 本地敏感词 + 频控去重；
/// 发布失败只记日志，绝不影响触发动态的原始业务（如分享、签到等）。
/// </summary>
public class MomentService
{
    /// <summary>同一用户两条动态的最小间隔（毫秒）</summary>
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(60);

    /// <summary>同一用户每小时动态上限</summary>
    private const int HourlyLimit = 20;

    /// <summary>去重窗口：同用户同类型同源对象在此窗口内不重复发布</summary>
    private static readonly TimeSpan DedupWindow = TimeSpan.FromMinutes(30);

    private readonly IAppDbContext _db;
    private readonly SensitiveWordService _sensitive;
    private readonly ILogger<MomentService> _logger;

    public MomentService(IAppDbContext db, SensitiveWordService sensitive, ILogger<MomentService> logger)
    {
        _db = db;
        _sensitive = sensitive;
        _logger = logger;
    }

    /// <summary>
    /// 查询动态（交流板「动态」标签）。venueId 传 0 表示不按场馆过滤。
    /// </summary>
    public async Task<List<UserMomentDto>> GetListAsync(long? venueId, long? viewerId, int take = 20, long? beforeId = null, CancellationToken ct = default)
    {
        var query = _db.UserMoments
            .Include(m => m.User)
            .AsQueryable();

        if (venueId.HasValue && venueId.Value > 0)
            query = query.Where(m => m.VenueId == null || m.VenueId == venueId.Value);

        if (beforeId.HasValue && beforeId.Value > 0)
            query = query.Where(m => m.Id < beforeId.Value);

        var limit = Math.Clamp(take, 1, 100);
        var list = await query
            .OrderByDescending(m => m.Id)
            .Take(limit)
            .ToListAsync(ct);

        var ownerIds = list.Where(m => m.User != null).Select(m => m.User!.Id).Distinct().ToHashSet();
        return list.Select(m => new UserMomentDto
        {
            Id = m.Id,
            VenueId = m.VenueId,
            VenueName = m.VenueName,
            Type = m.Type,
            Content = m.Content,
            ImageUrl = m.ImageUrl,
            CreatedAt = m.CreatedAt,
            OwnerId = m.UserId,
            OwnerName = m.User?.Nickname,
            OwnerAvatar = m.User?.AvatarUrl,
            IsOwner = viewerId.HasValue && m.UserId == viewerId.Value
        }).ToList();
    }

    /// <summary>
    /// 发布一条动态。任何拦截（未开公开、敏感词、频控、去重）均静默返回 false，
    /// 调用方无需关心发布结果。
    /// </summary>
    public async Task<bool> PublishAsync(long userId, long? venueId, string? venueName, string type, string content, string? targetKey = null, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content)) return false;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return false;
            if (!user.MomentsPublic) return false;

            // 本地敏感词兜底（内容中可能含用户填写文字，如便签全文）
            if (await _sensitive.FirstHitAsync(new[] { content }, ct) is not null) return false;

            var now = DateTime.UtcNow;

            // 频控：同用户最小间隔 + 每小时条数上限
            var last = await _db.UserMoments
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.Id)
                .Select(m => new { m.CreatedAt })
                .FirstOrDefaultAsync(ct);
            if (last is not null && now - last.CreatedAt < MinInterval) return false;

            var hourAgo = now.AddHours(-1);
            var hourCount = await _db.UserMoments.CountAsync(m => m.UserId == userId && m.CreatedAt >= hourAgo, ct);
            if (hourCount >= HourlyLimit) return false;

            // 去重：同类型同源对象在窗口内只发一次
            if (!string.IsNullOrEmpty(targetKey))
            {
                var dedupWindowStart = now - DedupWindow;
                var dup = await _db.UserMoments.AnyAsync(
                    m => m.UserId == userId && m.Type == type && m.TargetKey == targetKey && m.CreatedAt >= dedupWindowStart, ct);
                if (dup) return false;
            }

            _db.UserMoments.Add(new UserMoment
            {
                UserId = userId,
                VenueId = venueId,
                VenueName = venueName,
                Type = type,
                Content = content.Trim(),
                TargetKey = targetKey,
                CreatedAt = now
            });
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            // 动态发布失败不影响原始业务
            _logger.LogWarning(ex, "发布用户动态失败 userId={UserId} type={Type}", userId, type);
            return false;
        }
    }

    /// <summary>删除自己的动态</summary>
    public async Task<bool> DeleteAsync(long userId, long momentId, CancellationToken ct = default)
    {
        var moment = await _db.UserMoments.FirstOrDefaultAsync(m => m.Id == momentId && m.UserId == userId, ct);
        if (moment is null) return false;
        _db.UserMoments.Remove(moment);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>按源对象级联删除（如便签被隐藏/删除时同步删除其动态）</summary>
    public async Task DeleteByTargetAsync(string type, string targetKey, CancellationToken ct = default)
    {
        var moments = await _db.UserMoments
            .Where(m => m.Type == type && m.TargetKey == targetKey)
            .ToListAsync(ct);
        if (moments.Count == 0) return;
        _db.UserMoments.RemoveRange(moments);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>覆盖已存在动态的文案（如便签内容更新后同步刷新）</summary>
    public async Task UpdateContentForTargetAsync(string type, string targetKey, string content, CancellationToken ct = default)
    {
        try
        {
            var moment = await _db.UserMoments
                .FirstOrDefaultAsync(m => m.Type == type && m.TargetKey == targetKey, ct);
            if (moment is null) return;
            if (await _sensitive.FirstHitAsync(new[] { content }, ct) is not null) return;
            moment.Content = content.Trim();
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新动态文案失败 type={Type} target={Target}", type, targetKey);
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