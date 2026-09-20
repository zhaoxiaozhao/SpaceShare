using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

/// <summary>
/// 访客视角的用户公开主页：只暴露昵称/头像等已公开信息、用户主动公开的友邻画像与公开帖子。
/// 刻意不包含信用分、学习/阅读统计等个人数据（用户协议已声明信用分不对外公开）。
/// </summary>
public class UserProfileService
{
    public const int MaxPosts = 20;

    private readonly IAppDbContext _db;
    private readonly PersonaService _persona;
    private readonly VenuePostService _posts;

    public UserProfileService(IAppDbContext db, PersonaService persona, VenuePostService posts)
    {
        _db = db;
        _persona = persona;
        _posts = posts;
    }

    public async Task<UserProfileDto?> GetAsync(long userId, long? viewerId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.Nickname, u.AvatarUrl, u.CreatedAt })
            .FirstOrDefaultAsync(ct);
        if (user is null) return null;

        var postCount = await _db.VenuePosts
            .CountAsync(p => p.UserId == userId && p.Status == CommentStatus.Visible, ct);

        var posts = await _posts.GetPublicByUserAsync(userId, viewerId, MaxPosts, ct);
        var persona = await _persona.GetPublicAsync(userId, ct);

        return new UserProfileDto
        {
            Id = user.Id,
            Nickname = string.IsNullOrWhiteSpace(user.Nickname) ? "友邻" : user.Nickname!,
            AvatarUrl = user.AvatarUrl,
            JoinedAt = user.CreatedAt,
            IsSelf = viewerId.HasValue && viewerId.Value == userId,
            PostCount = postCount,
            PersonaPublic = persona is not null,
            Persona = persona,
            Posts = posts
        };
    }
}
