using System.Text.Json;
using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

/// <summary>
/// 书单分享：用户自选书籍 + 推荐语，生成随机 token 的公开快照；好友通过 token 匿名查看。
/// 合规约束：仅展示被动计数（浏览/收藏），公开到榜单需用户主动开启，且内容可审核、可举报、可下架。
/// </summary>
public class BookListShareService
{
    private const int MaxBooks = 20;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly IAppDbContext _db;
    private readonly SensitiveWordService _sensitive;
    private readonly IWechatService _wechat;

    public BookListShareService(IAppDbContext db, SensitiveWordService sensitive, IWechatService wechat)
    {
        _db = db;
        _sensitive = sensitive;
        _wechat = wechat;
    }

    public async Task<BookListShareDto> CreateAsync(long userId, CreateBookListShareRequest request, CancellationToken ct = default)
    {
        var ids = (request.BookIds ?? new List<long>()).Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
            throw AppException.BadRequest("books_required", "请至少选择一本书");
        if (ids.Count > MaxBooks)
            throw AppException.BadRequest("too_many_books", $"最多选择 {MaxBooks} 本书");

        var books = await _db.ReadingBooks
            .Where(b => b.UserId == userId && ids.Contains(b.Id))
            .ToListAsync(ct);
        if (books.Count == 0)
            throw AppException.BadRequest("books_not_found", "所选书籍不存在");

        // 保持用户勾选顺序
        books = ids.Select(id => books.FirstOrDefault(b => b.Id == id)).Where(b => b is not null).Select(b => b!).ToList();

        var title = (request.Title ?? string.Empty).Trim();
        if (title.Length == 0) title = "我的书单";
        if (title.Length > 30) title = title[..30];
        var remark = (request.Remark ?? string.Empty).Trim();
        if (remark.Length > 100) remark = remark[..100];

        await EnsureContentSafeAsync(userId, title, remark, ct);

        var items = books.Select(b => new BookListShareItemDto
        {
            BookId = b.Id,
            Title = b.Title,
            Author = b.Author,
            CoverUrl = b.CoverUrl,
            Status = b.Status.ToString(),
            TotalMinutes = b.TotalMinutes
        }).ToList();

        var share = new BookListShare
        {
            UserId = userId,
            Token = await GenerateTokenAsync(ct),
            Title = title,
            Remark = remark.Length == 0 ? null : remark,
            ItemsJson = JsonSerializer.Serialize(items, JsonOpts),
            IsPublic = request.IsPublic,
            CreatedAt = DateTime.UtcNow
        };
        _db.BookListShares.Add(share);
        await _db.SaveChangesAsync(ct);

        return await BuildDtoAsync(share, userId, ct);
    }

    public async Task<BookListShareDto?> GetByTokenAsync(string token, long? viewerId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var share = await _db.BookListShares
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token, ct);
        if (share is null) return null;

        // 浏览量只统计他人浏览，作者自己查看不计
        var isOwner = viewerId.HasValue && viewerId.Value == share.UserId;
        if (!isOwner)
        {
            share.ViewCount += 1;
            await _db.SaveChangesAsync(ct);
        }

        return await BuildDtoAsync(share, viewerId, ct);
    }

    public async Task<List<BookListShareDto>> GetMyAsync(long userId, CancellationToken ct = default)
    {
        var shares = await _db.BookListShares
            .Include(s => s.User)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Id)
            .Take(20)
            .ToListAsync(ct);

        var result = new List<BookListShareDto>();
        foreach (var s in shares) result.Add(await BuildDtoAsync(s, userId, ct));
        return result;
    }

    /// <summary>收藏/取消收藏（纯计数，不含任何奖励或解锁）；不能收藏自己的书单。</summary>
    public async Task<BookListShareDto> ToggleFavoriteAsync(long userId, string token, CancellationToken ct = default)
    {
        var share = await _db.BookListShares
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token, ct)
            ?? throw AppException.NotFound("书单不存在");
        if (share.UserId == userId)
            throw AppException.BadRequest("cannot_favorite_own", "不能收藏自己的书单");

        var fav = await _db.BookListShareFavorites
            .FirstOrDefaultAsync(f => f.ShareId == share.Id && f.UserId == userId, ct);
        if (fav is null)
        {
            _db.BookListShareFavorites.Add(new BookListShareFavorite
            {
                ShareId = share.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            _db.BookListShareFavorites.Remove(fav);
        }
        await _db.SaveChangesAsync(ct);

        return await BuildDtoAsync(share, userId, ct);
    }

    /// <summary>设置公开到热门书单榜（仅本人）</summary>
    public async Task<BookListShareDto> SetVisibilityAsync(long userId, string token, bool isPublic, CancellationToken ct = default)
    {
        var share = await _db.BookListShares
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token, ct)
            ?? throw AppException.NotFound("书单不存在");
        if (share.UserId != userId)
            throw AppException.Forbidden("只能管理自己的书单");

        share.IsPublic = isPublic;
        await _db.SaveChangesAsync(ct);

        return await BuildDtoAsync(share, userId, ct);
    }

    /// <summary>热门书单榜（仅公开书单，按收藏数/浏览数排序）</summary>
    public async Task<List<BookListShareBoardItemDto>> GetBoardAsync(int take, CancellationToken ct = default)
    {
        take = Math.Clamp(take <= 0 ? 20 : take, 1, 50);

        var ids = await _db.BookListShares
            .Where(s => s.IsPublic)
            .OrderByDescending(s => s.Favorites.Count)
            .ThenByDescending(s => s.ViewCount)
            .ThenByDescending(s => s.Id)
            .Take(take)
            .Select(s => s.Id)
            .ToListAsync(ct);
        if (ids.Count == 0) return new();

        var shares = await _db.BookListShares
            .Include(s => s.User)
            .Include(s => s.Favorites)
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(ct);

        return ids
            .Select(id => shares.FirstOrDefault(s => s.Id == id))
            .Where(s => s is not null)
            .Select(s => new BookListShareBoardItemDto
            {
                Id = s!.Id,
                Token = s.Token,
                Title = s.Title,
                Remark = s.Remark,
                OwnerName = s.User?.Nickname ?? "书友",
                OwnerAvatar = s.User?.AvatarUrl,
                Count = Deserialize(s.ItemsJson).Count,
                FavoriteCount = s.Favorites.Count,
                ViewCount = s.ViewCount,
                CreatedAt = s.CreatedAt
            })
            .ToList();
    }

    /// <summary>书单封面：后端代理云存储取回并以 base64 返回（绕开小程序云下载/域名限制）</summary>
    public async Task<List<BookListCoverDto>> GetCoversAsync(string token, CancellationToken ct = default)
    {
        var share = await _db.BookListShares.FirstOrDefaultAsync(s => s.Token == token, ct)
            ?? throw AppException.NotFound("书单不存在");

        var books = Deserialize(share.ItemsJson);
        var result = new List<BookListCoverDto>();
        long totalBytes = 0;
        foreach (var b in books.Take(10))
        {
            if (string.IsNullOrWhiteSpace(b.CoverUrl)) continue;
            var bytes = await _wechat.DownloadCloudFileAsync(b.CoverUrl!, ct);
            if (bytes is null || bytes.Length == 0) continue;
            // 单张过大或累计过大则跳过，避免响应体过大
            if (bytes.Length > 2_500_000 || totalBytes + bytes.Length > 6_000_000) continue;
            totalBytes += bytes.Length;
            result.Add(new BookListCoverDto
            {
                BookId = b.BookId,
                DataUrl = $"data:{DetectImageMime(bytes)};base64,{Convert.ToBase64String(bytes)}"
            });
        }
        return result;
    }

    private static string DetectImageMime(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8) return "image/jpeg";
        if (b.Length >= 4 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
        if (b.Length >= 12 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return "image/webp";
        if (b.Length >= 3 && b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46) return "image/gif";
        return "image/jpeg";
    }

    private async Task<BookListShareDto> BuildDtoAsync(BookListShare share, long? viewerId, CancellationToken ct)
    {
        var books = Deserialize(share.ItemsJson);
        var favoriteCount = await _db.BookListShareFavorites.CountAsync(f => f.ShareId == share.Id, ct);
        var favorited = viewerId.HasValue
            && await _db.BookListShareFavorites.AnyAsync(f => f.ShareId == share.Id && f.UserId == viewerId.Value, ct);

        return new BookListShareDto
        {
            Id = share.Id,
            Token = share.Token,
            Title = share.Title,
            Remark = share.Remark,
            OwnerName = share.User?.Nickname ?? "书友",
            OwnerAvatar = share.User?.AvatarUrl,
            Count = books.Count,
            TotalMinutes = books.Sum(b => b.TotalMinutes),
            ViewCount = share.ViewCount,
            FavoriteCount = favoriteCount,
            Favorited = favorited,
            IsOwner = viewerId.HasValue && viewerId.Value == share.UserId,
            IsPublic = share.IsPublic,
            CreatedAt = share.CreatedAt,
            Books = books
        };
    }

    private static List<BookListShareItemDto> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<BookListShareItemDto>>(json, JsonOpts) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private async Task<string> GenerateTokenAsync(CancellationToken ct)
    {
        for (var i = 0; i < 5; i++)
        {
            var token = Guid.NewGuid().ToString("N");
            var exists = await _db.BookListShares.AnyAsync(s => s.Token == token, ct);
            if (!exists) return token;
        }
        throw AppException.BadRequest("token_generate_failed", "生成分享失败，请重试");
    }

    private async Task EnsureContentSafeAsync(long userId, string title, string remark, CancellationToken ct)
    {
        var hit = await _sensitive.FirstHitAsync(new[] { title, remark }, ct);
        if (hit is not null)
            throw AppException.BadRequest("content_sensitive", $"内容包含敏感词「{hit}」，请修改后重试");

        var openId = await _db.Users.Where(u => u.Id == userId).Select(u => u.OpenId).FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(openId)) return;
        foreach (var text in new[] { title, remark })
        {
            if (string.IsNullOrWhiteSpace(text)) continue;
            var r = await _wechat.MsgSecCheckAsync(openId!, 2, text, ct);
            if (r.Status == ContentCheckStatus.Risky)
                throw AppException.BadRequest("content_risky", "内容未通过安全检测，请修改后重试");
        }
    }
}
