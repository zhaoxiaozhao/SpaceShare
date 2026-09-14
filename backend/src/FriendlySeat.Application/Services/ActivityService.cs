using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

/// <summary>
/// 线下活动：用户发布（需审核）、名额制报名。平台仅提供信息展示与报名撮合，活动内容由发布者负责。
/// </summary>
public class ActivityService
{
    private static readonly string[] ValidCategories =
        { "reading", "lecture", "exhibition", "study", "sharing", "workshop", "film", "music", "art", "sports", "competition", "volunteer", "other" };

    private readonly IAppDbContext _db;
    private readonly INotificationService _notifications;

    public ActivityService(IAppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<ActivityDto> CreateAsync(long userId, ActivityCreateRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await ValidateAsync(request, ct);

        var entity = new Activity
        {
            CreatorUserId = userId,
            Title = request.Title.Trim(),
            Category = ValidCategories.Contains(request.Category) ? request.Category : "other",
            VenueId = request.VenueId,
            LocationText = string.IsNullOrWhiteSpace(request.LocationText) ? null : request.LocationText.Trim(),
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            SignupDeadline = request.SignupDeadline,
            Capacity = request.Capacity,
            Description = request.Description?.Trim() ?? string.Empty,
            CoverImage = string.IsNullOrWhiteSpace(request.CoverImage) ? null : request.CoverImage.Trim(),
            Status = ActivityStatus.PendingReview,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Activities.Add(entity);
        await _db.SaveChangesAsync(ct);

        return await GetDtoAsync(entity.Id, userId, includeSignups: true, ct) ?? throw AppException.NotFound();
    }

    public async Task<ActivityDto> UpdateAsync(long id, long userId, ActivityCreateRequest request, CancellationToken ct = default)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw AppException.NotFound("活动不存在");
        if (activity.CreatorUserId != userId)
            throw AppException.Forbidden("只能编辑自己发布的活动");
        if (activity.Status != ActivityStatus.PendingReview && activity.Status != ActivityStatus.Rejected)
            throw AppException.BadRequest("activity_locked", "活动已发布或已结束，无法编辑");

        await ValidateAsync(request, ct);

        activity.Title = request.Title.Trim();
        activity.Category = ValidCategories.Contains(request.Category) ? request.Category : "other";
        activity.VenueId = request.VenueId;
        activity.LocationText = string.IsNullOrWhiteSpace(request.LocationText) ? null : request.LocationText.Trim();
        activity.StartAt = request.StartAt;
        activity.EndAt = request.EndAt;
        activity.SignupDeadline = request.SignupDeadline;
        activity.Capacity = request.Capacity;
        activity.Description = request.Description?.Trim() ?? string.Empty;
        activity.CoverImage = string.IsNullOrWhiteSpace(request.CoverImage) ? null : request.CoverImage.Trim();
        activity.Status = ActivityStatus.PendingReview;
        activity.ReviewRemark = null;
        activity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await GetDtoAsync(activity.Id, userId, includeSignups: true, ct) ?? throw AppException.NotFound();
    }

    public async Task CancelAsync(long id, long userId, CancellationToken ct = default)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw AppException.NotFound("活动不存在");
        if (activity.CreatorUserId != userId)
            throw AppException.Forbidden("只能取消自己发布的活动");
        if (activity.Status == ActivityStatus.Finished || activity.Status == ActivityStatus.Cancelled)
            throw AppException.BadRequest("activity_closed", "活动已结束或已取消");

        activity.Status = ActivityStatus.Cancelled;
        activity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<ActivityDto>> GetListAsync(long viewerUserId, string? category, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var q = _db.Activities.Where(a => a.Status == ActivityStatus.Published && a.EndAt > now);
        if (!string.IsNullOrWhiteSpace(category) && ValidCategories.Contains(category))
            q = q.Where(a => a.Category == category);

        var list = await q.OrderBy(a => a.StartAt).Take(100).ToListAsync(ct);
        return await BuildDtosAsync(list, viewerUserId, includeSignups: false, ct);
    }

    public async Task<List<ActivityDto>> GetMineAsync(long userId, CancellationToken ct = default)
    {
        var list = await _db.Activities
            .Where(a => a.CreatorUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .ToListAsync(ct);
        return await BuildDtosAsync(list, userId, includeSignups: true, ct);
    }

    public async Task<List<ActivityDto>> GetJoinedAsync(long userId, CancellationToken ct = default)
    {
        var ids = await _db.ActivitySignups
            .Where(s => s.UserId == userId && s.Status == ActivitySignupStatus.Joined)
            .Select(s => s.ActivityId)
            .Distinct()
            .ToListAsync(ct);
        var list = await _db.Activities
            .Where(a => ids.Contains(a.Id))
            .OrderByDescending(a => a.StartAt)
            .Take(50)
            .ToListAsync(ct);
        return await BuildDtosAsync(list, userId, includeSignups: false, ct);
    }

    public async Task<ActivityDto?> GetDetailAsync(long id, long viewerUserId, CancellationToken ct = default)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (activity is null) return null;
        if (activity.Status != ActivityStatus.Published && activity.CreatorUserId != viewerUserId)
            return null;
        return await GetDtoAsync(id, viewerUserId, includeSignups: true, ct);
    }

    public async Task SignupAsync(long id, long userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw AppException.NotFound("活动不存在");
        if (activity.Status != ActivityStatus.Published)
            throw AppException.BadRequest("activity_not_open", "活动未开放报名");

        var deadline = activity.SignupDeadline ?? activity.StartAt;
        if (now >= deadline)
            throw AppException.BadRequest("signup_closed", "报名已截止");

        var joined = await _db.ActivitySignups.CountAsync(s => s.ActivityId == id && s.Status == ActivitySignupStatus.Joined, ct);
        var existing = await _db.ActivitySignups.FirstOrDefaultAsync(s => s.ActivityId == id && s.UserId == userId, ct);
        if (existing is not null && existing.Status == ActivitySignupStatus.Joined)
            throw AppException.Conflict("already_signed_up", "你已报名该活动");
        if (joined >= activity.Capacity)
            throw AppException.BadRequest("activity_full", "名额已满");

        if (existing is null)
        {
            _db.ActivitySignups.Add(new ActivitySignup
            {
                ActivityId = id,
                UserId = userId,
                Status = ActivitySignupStatus.Joined,
                CreatedAt = now
            });
        }
        else
        {
            existing.Status = ActivitySignupStatus.Joined;
            existing.CreatedAt = now;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task CancelSignupAsync(long id, long userId, CancellationToken ct = default)
    {
        var signup = await _db.ActivitySignups.FirstOrDefaultAsync(s => s.ActivityId == id && s.UserId == userId, ct)
            ?? throw AppException.NotFound("报名记录不存在");
        if (signup.Status != ActivitySignupStatus.Joined)
            throw AppException.BadRequest("not_joined", "你尚未报名该活动");

        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (activity is not null && activity.StartAt <= DateTime.UtcNow)
            throw AppException.BadRequest("activity_started", "活动已开始，无法取消报名");

        signup.Status = ActivitySignupStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
    }

    // ---- 管理端 ----

    public async Task<List<ActivityDto>> AdminListAsync(string? status, CancellationToken ct = default)
    {
        var q = _db.Activities.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ActivityStatus>(status, true, out var st))
            q = q.Where(a => a.Status == st);

        var list = await q.OrderByDescending(a => a.CreatedAt).Take(200).ToListAsync(ct);
        return await BuildDtosAsync(list, 0, includeSignups: true, ct, responsesForAll: true);
    }

    public async Task AdminReviewAsync(long id, bool approve, string? remark, CancellationToken ct = default)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw AppException.NotFound("活动不存在");
        if (activity.Status != ActivityStatus.PendingReview && activity.Status != ActivityStatus.Rejected)
            throw AppException.BadRequest("activity_not_pending", "该活动不在待审核状态");

        activity.Status = approve ? ActivityStatus.Published : ActivityStatus.Rejected;
        activity.ReviewRemark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
        activity.ReviewedAt = DateTime.UtcNow;
        activity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _notifications.SendAsync(activity.CreatorUserId, NotificationType.System,
            approve ? "活动审核通过" : "活动未通过审核",
            approve ? $"你发布的活动「{activity.Title}」已通过审核，现已展示。" : $"你发布的活动「{activity.Title}」未通过审核：{activity.ReviewRemark ?? "内容不符合要求"}。",
            null, ct);
    }

    public async Task AdminTakeDownAsync(long id, CancellationToken ct = default)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw AppException.NotFound("活动不存在");
        activity.Status = ActivityStatus.Cancelled;
        activity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // ---- 内部辅助 ----

    private static Task ValidateAsync(ActivityCreateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw AppException.BadRequest("title_required", "请填写活动标题");
        if (request.Title.Trim().Length > 50)
            throw AppException.BadRequest("title_too_long", "活动标题过长");
        if (request.EndAt <= request.StartAt)
            throw AppException.BadRequest("time_invalid", "结束时间需晚于开始时间");
        if (request.SignupDeadline.HasValue && request.SignupDeadline.Value > request.StartAt)
            throw AppException.BadRequest("deadline_invalid", "报名截止时间不能晚于活动开始时间");
        if (request.Capacity <= 0 || request.Capacity > 1000)
            throw AppException.BadRequest("capacity_invalid", "报名名额需在 1–1000 之间");
        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 1000)
            throw AppException.BadRequest("description_too_long", "活动介绍过长");
        return Task.CompletedTask;
    }

    private async Task<ActivityDto?> GetDtoAsync(long id, long viewerUserId, bool includeSignups, CancellationToken ct)
    {
        var entity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (entity is null) return null;
        var dtos = await BuildDtosAsync(new List<Activity> { entity }, viewerUserId, includeSignups, ct);
        return dtos.FirstOrDefault();
    }

    private async Task<List<ActivityDto>> BuildDtosAsync(
        List<Activity> list, long viewerUserId, bool includeSignups, CancellationToken ct, bool responsesForAll = false)
    {
        if (list.Count == 0) return new List<ActivityDto>();

        var ids = list.Select(a => a.Id).ToList();
        var creatorIds = list.Select(a => a.CreatorUserId).Distinct().ToList();
        var venueIds = list.Where(a => a.VenueId.HasValue).Select(a => a.VenueId!.Value).Distinct().ToList();

        var counts = await _db.ActivitySignups
            .Where(s => ids.Contains(s.ActivityId) && s.Status == ActivitySignupStatus.Joined)
            .GroupBy(s => s.ActivityId)
            .Select(g => new { ActivityId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var countMap = counts.ToDictionary(x => x.ActivityId, x => x.Count);

        var mySignups = await _db.ActivitySignups
            .Where(s => s.UserId == viewerUserId && ids.Contains(s.ActivityId) && s.Status == ActivitySignupStatus.Joined)
            .Select(s => s.ActivityId)
            .ToListAsync(ct);
        var mySignupSet = mySignups.ToHashSet();

        var creatorNames = await _db.Users.Where(u => creatorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nickname ?? "", ct);
        var venueNames = await _db.Venues.Where(v => venueIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.Name, ct);

        var signupEntities = new List<ActivitySignup>();
        if (includeSignups)
        {
            signupEntities = await _db.ActivitySignups
                .Where(s => ids.Contains(s.ActivityId) && s.Status == ActivitySignupStatus.Joined)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync(ct);
        }
        var signupUserIds = signupEntities.Select(s => s.UserId).Distinct().ToList();
        var signupUsers = await _db.Users.Where(u => signupUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Nickname, u.AvatarUrl })
            .ToListAsync(ct);
        var signupUserMap = signupUsers.ToDictionary(u => u.Id);

        var now = DateTime.UtcNow;
        var result = new List<ActivityDto>();
        foreach (var a in list)
        {
            var count = countMap.TryGetValue(a.Id, out var c) ? c : 0;
            var isMine = a.CreatorUserId == viewerUserId;
            var deadline = a.SignupDeadline ?? a.StartAt;
            var dto = new ActivityDto
            {
                Id = a.Id,
                CreatorUserId = a.CreatorUserId,
                CreatorNickname = creatorNames.TryGetValue(a.CreatorUserId, out var cn) ? cn : "",
                Title = a.Title,
                Category = a.Category,
                VenueId = a.VenueId,
                VenueName = a.VenueId.HasValue && venueNames.TryGetValue(a.VenueId.Value, out var vn) ? vn : null,
                LocationText = a.LocationText,
                StartAt = a.StartAt,
                EndAt = a.EndAt,
                SignupDeadline = a.SignupDeadline,
                Capacity = a.Capacity,
                SignupCount = count,
                Description = a.Description,
                CoverImage = a.CoverImage,
                Status = a.Status.ToString(),
                ReviewRemark = a.ReviewRemark,
                CreatedAt = a.CreatedAt,
                IsMine = isMine,
                IsSignedUp = mySignupSet.Contains(a.Id),
                IsFull = count >= a.Capacity,
                SignupOpen = a.Status == ActivityStatus.Published && now < deadline && count < a.Capacity
            };

            if (includeSignups)
            {
                dto.Signups = signupEntities
                    .Where(s => s.ActivityId == a.Id)
                    .Select(s =>
                    {
                        signupUserMap.TryGetValue(s.UserId, out var su);
                        return new ActivitySignupDto
                        {
                            Id = s.Id,
                            UserId = s.UserId,
                            UserNickname = su?.Nickname ?? "",
                            UserAvatar = su?.AvatarUrl,
                            Status = s.Status.ToString(),
                            CreatedAt = s.CreatedAt
                        };
                    })
                    .ToList();
            }
            result.Add(dto);
        }
        return result;
    }
}
