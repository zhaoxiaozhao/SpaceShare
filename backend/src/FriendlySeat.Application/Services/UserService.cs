using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class UserService
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _notifications;

    public UserService(IAppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<UserDto> GetProfileAsync(long userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        return AuthService.ToDto(user);
    }

    public async Task<UserDto> UpdateProfileAsync(long userId, UserProfileUpdateRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        if (!string.IsNullOrWhiteSpace(request.Nickname)) user.Nickname = request.Nickname.Trim();
        if (!string.IsNullOrWhiteSpace(request.AvatarUrl)) user.AvatarUrl = request.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return AuthService.ToDto(user);
    }

    public async Task<List<UserContactDto>> GetContactsAsync(long userId, CancellationToken ct = default)
    {
        return await _db.UserContacts
            .Where(c => c.UserId == userId)
            .Select(c => new UserContactDto
            {
                Id = c.Id,
                ContactType = c.ContactType.ToString(),
                ContactValue = c.ContactValue,
                IsPublic = c.IsPublic
            })
            .ToListAsync(ct);
    }

    public async Task<UserContactDto> UpsertContactAsync(long userId, UpsertContactRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ContactType>(request.ContactType, true, out var type))
            throw AppException.BadRequest("contact_type_invalid", "联系方式类型无效");
        if (string.IsNullOrWhiteSpace(request.ContactValue))
            throw AppException.BadRequest("contact_value_required", "联系方式不能为空");

        var contact = await _db.UserContacts.FirstOrDefaultAsync(c => c.UserId == userId && c.ContactType == type, ct);
        if (contact is null)
        {
            contact = new UserContact { UserId = userId, ContactType = type };
            _db.UserContacts.Add(contact);
        }
        contact.ContactValue = request.ContactValue.Trim();
        contact.IsPublic = request.IsPublic;
        contact.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new UserContactDto
        {
            Id = contact.Id,
            ContactType = contact.ContactType.ToString(),
            ContactValue = contact.ContactValue,
            IsPublic = contact.IsPublic
        };
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync(long userId, bool? unread, CancellationToken ct = default)
    {
        var query = _db.Notifications.Where(n => n.UserId == userId);
        if (unread == true) query = query.Where(n => !n.IsRead);

        return await query.OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Title = n.Title,
                Content = n.Content,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task MarkNotificationsReadAsync(long userId, CancellationToken ct = default)
    {
        var items = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        foreach (var item in items) item.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnreadCountAsync(long userId, CancellationToken ct = default)
    {
        await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default)
    {
        return await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task<bool> CheckContactAuthorizationAsync(long viewerId, long shareId, CancellationToken ct = default)
    {
        var share = await _db.SeatShares
            .Include(s => s.Reservations)
            .FirstOrDefaultAsync(s => s.Id == shareId, ct)
            ?? throw AppException.NotFound("分享不存在");

        if (!share.AllowContact)
            return false;

        // 仅当分享者授权、且对方已成功预约该分享
        var reserved = share.Reservations.Any(
            r => r.UserId == viewerId && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Arrived));
        return reserved;
    }

    public async Task<ContactResultDto?> GetShareOwnerContactAsync(long viewerId, long shareId, CancellationToken ct = default)
    {
        if (!await CheckContactAuthorizationAsync(viewerId, shareId, ct))
            return null;

        var share = await _db.SeatShares.FirstAsync(s => s.Id == shareId, ct);
        var contact = await _db.UserContacts
            .Where(c => c.UserId == share.OwnerUserId && c.IsPublic)
            .OrderBy(c => c.ContactType)
            .FirstOrDefaultAsync(ct);

        if (contact is null) return null;

        return new ContactResultDto
        {
            ContactType = contact.ContactType.ToString(),
            ContactValue = contact.ContactValue
        };
    }
}
