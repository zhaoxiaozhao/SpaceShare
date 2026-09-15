using System.Text.Json;
using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

/// <summary>
/// 书单分享：用户自选书籍 + 推荐语，生成随机 token 的公开快照；好友通过 token 匿名查看。
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
            CreatedAt = DateTime.UtcNow
        };
        _db.BookListShares.Add(share);
        await _db.SaveChangesAsync(ct);

        return await BuildDtoAsync(share, ct);
    }

    public async Task<BookListShareDto?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var share = await _db.BookListShares
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token, ct);
        if (share is null) return null;

        share.ViewCount += 1;
        await _db.SaveChangesAsync(ct);

        return await BuildDtoAsync(share, ct);
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
        foreach (var s in shares) result.Add(await BuildDtoAsync(s, ct));
        return result;
    }

    private async Task<BookListShareDto> BuildDtoAsync(BookListShare share, CancellationToken ct)
    {
        var books = Deserialize(share.ItemsJson);
        return new BookListShareDto
        {
            Token = share.Token,
            Title = share.Title,
            Remark = share.Remark,
            OwnerName = share.User?.Nickname ?? "书友",
            OwnerAvatar = share.User?.AvatarUrl,
            Count = books.Count,
            TotalMinutes = books.Sum(b => b.TotalMinutes),
            ViewCount = share.ViewCount,
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
