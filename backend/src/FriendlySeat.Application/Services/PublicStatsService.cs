using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

/// <summary>
/// 公开统计：实时在线人数（正在自习/阅读），仅聚合数字，不含任何个人标识。
/// </summary>
public class PublicStatsService
{
    private readonly IAppDbContext _db;

    public PublicStatsService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<LiveStatsDto> GetLiveAsync(CancellationToken ct = default)
    {
        var studyUsers = await _db.StudySessions
            .Where(s => s.Status == StudySessionStatus.Active)
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(ct);

        var readingUsers = await _db.ReadingSessions
            .Where(r => r.Status == ReadingSessionStatus.Active)
            .Select(r => r.UserId)
            .Distinct()
            .ToListAsync(ct);

        var all = new HashSet<long>(studyUsers);
        all.UnionWith(readingUsers);

        return new LiveStatsDto
        {
            Studying = studyUsers.Count,
            Reading = readingUsers.Count,
            Total = all.Count
        };
    }
}
