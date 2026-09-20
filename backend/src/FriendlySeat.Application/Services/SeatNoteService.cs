using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

/// <summary>
/// 座位便签：每人在同一座位最多一条，可修改/删除；被举报后自动隐藏并进入后台审核。
/// 内容限制 50 字，并做本地敏感词 + 微信内容安全校验。
/// </summary>
public class SeatNoteService
{
    /// <summary>便签最大字数</summary>
    public const int MaxLength = 50;

    private readonly IAppDbContext _db;
    private readonly SensitiveWordService _sensitive;
    private readonly IWechatService _wechat;

    public SeatNoteService(IAppDbContext db, SensitiveWordService sensitive, IWechatService wechat)
    {
        _db = db;
        _sensitive = sensitive;
        _wechat = wechat;
    }

    public async Task<List<SeatNoteDto>> GetBySeatAsync(long seatId, long? viewerId, int take = 20, CancellationToken ct = default)
    {
        var list = await _db.SeatNotes
            .Include(n => n.User)
            .Where(n => n.SeatId == seatId && n.Status == SeatNoteStatus.Visible)
            .OrderByDescending(n => n.UpdatedAt)
            .Take(Math.Clamp(take, 1, 50))
            .ToListAsync(ct);

        return list.Select(n => ToDto(n, viewerId)).ToList();
    }

    public async Task<SeatNoteDto> CreateOrUpdateAsync(long userId, CreateSeatNoteRequest request, CancellationToken ct = default)
    {
        var seat = await _db.Seats.FirstOrDefaultAsync(s => s.Id == request.SeatId, ct)
            ?? throw AppException.NotFound("座位不存在");

        var content = (request.Content ?? string.Empty).Trim();
        if (content.Length == 0)
            throw AppException.BadRequest("content_required", "请先写点什么");
        if (content.Length > MaxLength)
            throw AppException.BadRequest("content_too_long", $"便签最多 {MaxLength} 字");

        await EnsureContentSafeAsync(userId, content, ct);

        var note = await _db.SeatNotes
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.SeatId == seat.Id && n.UserId == userId, ct);

        if (note is null)
        {
            note = new SeatNote
            {
                SeatId = seat.Id,
                UserId = userId,
                Content = content,
                Status = SeatNoteStatus.Visible,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.SeatNotes.Add(note);
            await _db.SaveChangesAsync(ct);
            note = await _db.SeatNotes.Include(n => n.User).FirstAsync(n => n.Id == note.Id, ct);
        }
        else
        {
            if (note.Status == SeatNoteStatus.Hidden)
                throw AppException.BadRequest("note_under_review", "该便签因举报待审核，暂不可修改");

            note.Content = content;
            note.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return ToDto(note, userId);
    }

    public async Task DeleteAsync(long userId, long noteId, CancellationToken ct = default)
    {
        var note = await _db.SeatNotes.FirstOrDefaultAsync(n => n.Id == noteId, ct)
            ?? throw AppException.NotFound("便签不存在");
        if (note.UserId != userId)
            throw AppException.Forbidden("只能删除自己的便签");

        _db.SeatNotes.Remove(note);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>被举报：自动隐藏并进入审核</summary>
    public async Task HideByReportAsync(long noteId, CancellationToken ct = default)
    {
        var note = await _db.SeatNotes.FirstOrDefaultAsync(n => n.Id == noteId, ct);
        if (note is null || note.Status == SeatNoteStatus.Hidden) return;
        note.Status = SeatNoteStatus.Hidden;
        note.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // ============ 管理端 ============

    public async Task<List<SeatNoteDto>> AdminListAsync(string? status, CancellationToken ct = default)
    {
        var query = _db.SeatNotes.Include(n => n.User).Include(n => n.Seat).AsQueryable();
        if (Enum.TryParse<SeatNoteStatus>(status, true, out var st))
        {
            query = query.Where(n => n.Status == st);
        }

        var list = await query
            .OrderByDescending(n => n.UpdatedAt)
            .Take(200)
            .ToListAsync(ct);

        return list.Select(n =>
        {
            var dto = ToDto(n, null);
            dto.SeatCode = n.Seat?.Code;
            return dto;
        }).ToList();
    }

    /// <summary>审核：通过=恢复展示；驳回=删除</summary>
    public async Task AdminReviewAsync(long id, bool approve, long operatorId, CancellationToken ct = default)
    {
        var note = await _db.SeatNotes.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw AppException.NotFound("便签不存在");

        if (approve)
        {
            note.Status = SeatNoteStatus.Visible;
            note.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.SeatNotes.Remove(note);
        }
        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminUserId = operatorId,
            Action = "seat_note.review",
            EntityType = "SeatNote",
            EntityId = id.ToString(),
            Detail = approve ? "审核通过" : "驳回删除",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private static SeatNoteDto ToDto(SeatNote n, long? viewerId) => new()
    {
        Id = n.Id,
        SeatId = n.SeatId,
        Content = n.Content,
        OwnerName = n.User?.Nickname ?? "友邻",
        OwnerAvatar = n.User?.AvatarUrl,
        IsOwner = viewerId.HasValue && viewerId.Value == n.UserId,
        Status = n.Status.ToString(),
        CreatedAt = n.CreatedAt,
        UpdatedAt = n.UpdatedAt
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
