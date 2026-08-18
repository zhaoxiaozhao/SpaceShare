using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class StudyService
{
    private readonly IAppDbContext _db;

    public StudyService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<StudySessionDto> StartAsync(long userId, StartStudyRequest request, CancellationToken ct = default)
    {
        var active = await _db.StudySessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == StudySessionStatus.Active, ct);
        if (active is not null)
        {
            throw AppException.Conflict("study_already_active", "已有进行中的学习记录，请先结束当前学习");
        }

        var session = new StudySession
        {
            UserId = userId,
            Type = Enum.TryParse<StudyType>(request.Type, true, out var t) ? t : StudyType.Other,
            VenueId = request.VenueId,
            Note = request.Note,
            StartedAt = DateTime.UtcNow,
            Status = StudySessionStatus.Active,
            DurationMinutes = 0
        };
        _db.StudySessions.Add(session);
        await _db.SaveChangesAsync(ct);

        return new StudySessionDto
        {
            Id = session.Id,
            Type = session.Type.ToString(),
            VenueId = session.VenueId,
            StartedAt = session.StartedAt,
            Status = session.Status.ToString()
        };
    }

    public async Task<StudySessionDto> EndAsync(long userId, long sessionId, CancellationToken ct = default)
    {
        var session = await _db.StudySessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);
        if (session is null) throw AppException.NotFound("学习记录不存在");
        if (session.Status != StudySessionStatus.Active) throw AppException.BadRequest("study_not_active", "该学习记录已结束");

        var endedAt = DateTime.UtcNow;
        var minutes = Math.Max(1, (int)Math.Round((endedAt - session.StartedAt).TotalMinutes));
        session.EndedAt = endedAt;
        session.DurationMinutes = minutes;
        session.Status = StudySessionStatus.Completed;
        await _db.SaveChangesAsync(ct);

        await RefreshGoalAsync(userId, session.StartedAt.Date, minutes, ct);
        await CheckAchievementsAsync(userId, ct);

        return new StudySessionDto
        {
            Id = session.Id,
            Type = session.Type.ToString(),
            VenueId = session.VenueId,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            DurationMinutes = minutes,
            Status = session.Status.ToString()
        };
    }

    public async Task<StudySessionDto> EndActiveAsync(long userId, CancellationToken ct = default)
    {
        var active = await _db.StudySessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == StudySessionStatus.Active, ct);
        if (active is null) throw AppException.NotFound("没有进行中的学习记录");
        return await EndAsync(userId, active.Id, ct);
    }

    public async Task<StudyTodayDto> GetTodayAsync(long userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var dayStart = now.Date;

        var sessions = await _db.StudySessions
            .Where(s => s.UserId == userId && s.StartedAt >= dayStart && s.Status == StudySessionStatus.Completed)
            .ToListAsync(ct);

        var todayMinutes = sessions.Sum(s => s.DurationMinutes);

        var goal = await _db.StudyGoals
            .FirstOrDefaultAsync(g => g.UserId == userId && g.Period == GoalPeriod.Daily
                && g.PeriodStart == dayStart, ct);

        var active = await _db.StudySessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == StudySessionStatus.Active, ct);

        return new StudyTodayDto
        {
            TodayMinutes = todayMinutes,
            SessionCount = sessions.Count,
            ConsecutiveDays = await CalcConsecutiveDaysAsync(userId, now, ct),
            TargetMinutes = goal?.TargetMinutes,
            TargetProgress = goal is { TargetMinutes: > 0 } ? Math.Round(todayMinutes * 100.0 / goal.TargetMinutes, 1) : 0,
            ActiveSession = active is null ? null : ToSessionDto(active)
        };
    }

    public async Task<List<StudySessionDto>> GetSessionsAsync(long userId, int take = 50, CancellationToken ct = default)
    {
        return await _db.StudySessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .Take(take)
            .Select(s => new StudySessionDto
            {
                Id = s.Id,
                Type = s.Type.ToString(),
                VenueId = s.VenueId,
                StartedAt = s.StartedAt,
                EndedAt = s.EndedAt,
                DurationMinutes = s.DurationMinutes,
                Status = s.Status.ToString()
            })
            .ToListAsync(ct);
    }

    public async Task<StudyGoalDto> SetGoalAsync(long userId, SetStudyGoalRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<GoalPeriod>(request.Period, true, out var period))
            throw AppException.BadRequest("period_invalid", "目标周期无效");
        if (request.TargetMinutes is < 30 or > 1440)
            throw AppException.BadRequest("target_invalid", "目标时长需在 30 到 1440 分钟之间");

        var now = DateTime.UtcNow;
        var (start, end) = GetPeriodRange(period, now);

        var goal = await _db.StudyGoals
            .FirstOrDefaultAsync(g => g.UserId == userId && g.Period == period && g.PeriodStart == start, ct);

        if (goal is null)
        {
            goal = new StudyGoal
            {
                UserId = userId,
                Period = period,
                PeriodStart = start,
                PeriodEnd = end,
                TargetMinutes = request.TargetMinutes,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.StudyGoals.Add(goal);
        }
        else
        {
            goal.TargetMinutes = request.TargetMinutes;
            goal.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
        return await GetGoalDtoAsync(goal, ct);
    }

    public async Task<List<StudyGoalDto>> GetGoalsAsync(long userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var result = new List<StudyGoalDto>();
        foreach (GoalPeriod period in Enum.GetValues<GoalPeriod>())
        {
            var (start, _) = GetPeriodRange(period, now);
            var goal = await _db.StudyGoals
                .FirstOrDefaultAsync(g => g.UserId == userId && g.Period == period && g.PeriodStart == start, ct);
            if (goal is null) continue;
            result.Add(await GetGoalDtoAsync(goal, ct));
        }
        return result;
    }

    public async Task<StudyReportDto> GetReportAsync(long userId, string period, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var p = period?.ToLowerInvariant() == "monthly" ? "monthly" : "weekly";
        var (start, end) = p == "weekly" ? GetPeriodRange(GoalPeriod.Weekly, now) : GetPeriodRange(GoalPeriod.Monthly, now);

        var sessions = await _db.StudySessions
            .Where(s => s.UserId == userId && s.Status == StudySessionStatus.Completed
                && s.StartedAt >= start && s.StartedAt < end)
            .ToListAsync(ct);

        var report = new StudyReportDto
        {
            Period = p == "weekly" ? "weekly" : "monthly",
            Start = start,
            End = end,
            TotalMinutes = sessions.Sum(s => s.DurationMinutes),
            SessionCount = sessions.Count,
            StudyDays = sessions.Select(s => s.StartedAt.Date).Distinct().Count(),
            MaxDailyMinutes = sessions.GroupBy(s => s.StartedAt.Date)
                .Select(g => g.Sum(s => s.DurationMinutes))
                .DefaultIfEmpty(0).Max(),
            TypeDistribution = sessions
                .GroupBy(s => s.Type)
                .OrderByDescending(g => g.Sum(s => s.DurationMinutes))
                .Select(g => new KeyValuePair<string, int>(g.Key.ToString(), g.Sum(s => s.DurationMinutes)))
                .ToList(),
            DailyMinutes = Enumerable.Range(0, (end - start).Days)
                .Select(i => new KeyValuePair<string, int>(start.AddDays(i).ToString("MM-dd"),
                    sessions.Where(s => s.StartedAt.Date == start.AddDays(i).Date).Sum(s => s.DurationMinutes)))
                .ToList()
        };

        var days = sessions.Select(s => s.StartedAt.Date).Distinct().OrderBy(d => d).ToList();
        var streak = 0;
        var longest = 0;
        for (var i = 0; i < days.Count; i++)
        {
            if (i > 0 && (days[i] - days[i - 1]).TotalDays == 1)
            {
                streak++;
            }
            else
            {
                streak = 1;
            }
            longest = Math.Max(longest, streak);
        }
        report.LongestStreak = longest;

        return report;
    }

    public async Task<List<StudyAchievementDto>> GetAchievementsAsync(long userId, CancellationToken ct = default)
    {
        var earned = await _db.StudyAchievements
            .Where(a => a.UserId == userId)
            .ToDictionaryAsync(a => a.Code, a => (DateTime?)a.EarnedAt, ct);

        var stats = await GetLifeStatsAsync(userId, ct);

        var defs = new[]
        {
            ("first_study", "初学乍练", "完成第一次学习记录", "📘", stats.TotalMinutes >= 1),
            ("seven_days", "七日不断", "连续学习 7 天", "📅", stats.LongestStreak >= 7),
            ("hundred_hours", "百小时", "累计学习 100 小时", "⏱️", stats.TotalMinutes >= 6000),
            ("morning", "晨读者", "累计早晨学习达到 10 小时", "🌅", stats.MorningMinutes >= 600),
            ("night", "夜读者", "累计夜间学习达到 10 小时", "🌙", stats.NightMinutes >= 600),
            ("long_term", "长期主义", "连续学习 30 天", "🔥", stats.LongestStreak >= 30),
            ("fifty_hours", "半百小时", "累计学习 50 小时", "🕐", stats.TotalMinutes >= 3000),
            ("first_week", "每周坚持", "一周内学习 5 天", "🗓️", stats.WeekStudyDays >= 5)
        };

        return defs.Select(d =>
        {
            earned.TryGetValue(d.Item1, out var earnedAt);
            return new StudyAchievementDto
            {
                Code = d.Item1,
                Title = d.Item2,
                Description = d.Item3,
                Icon = d.Item4,
                Earned = earnedAt.HasValue || d.Item5,
                EarnedAt = earnedAt
            };
        }).ToList();
    }

    private async Task<StudyGoalDto> GetGoalDtoAsync(StudyGoal goal, CancellationToken ct)
    {
        var achieved = await _db.StudySessions
            .Where(s => s.UserId == goal.UserId && s.Status == StudySessionStatus.Completed
                && s.StartedAt >= goal.PeriodStart && s.StartedAt < goal.PeriodEnd)
            .SumAsync(s => (int?)s.DurationMinutes, ct) ?? 0;

        return new StudyGoalDto
        {
            Id = goal.Id,
            Period = goal.Period.ToString(),
            TargetMinutes = goal.TargetMinutes,
            AchievedMinutes = achieved,
            Progress = goal.TargetMinutes > 0 ? Math.Round(achieved * 100.0 / goal.TargetMinutes, 1) : 0,
            PeriodStart = goal.PeriodStart,
            PeriodEnd = goal.PeriodEnd
        };
    }

    private async Task RefreshGoalAsync(long userId, DateTime day, int minutes, CancellationToken ct)
    {
        var goals = await _db.StudyGoals
            .Where(g => g.UserId == userId && g.PeriodStart <= day && g.PeriodEnd > day)
            .ToListAsync(ct);
        // 目标进度实时计算，无需额外更新
        await Task.CompletedTask;
    }

    private async Task<int> CalcConsecutiveDaysAsync(long userId, DateTime now, CancellationToken ct)
    {
        var days = await _db.StudySessions
            .Where(s => s.UserId == userId && s.Status == StudySessionStatus.Completed)
            .Select(s => s.StartedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(400)
            .ToListAsync(ct);

        if (days.Count == 0) return 0;

        var streak = 0;
        var expected = now.Date;
        var hasToday = days.Contains(expected);
        if (!hasToday)
        {
            // 今天还没学习：若昨天学了则连续从昨天算，否则为 0
            if (!days.Contains(expected.AddDays(-1))) return 0;
            expected = expected.AddDays(-1);
        }

        foreach (var d in days)
        {
            if (d == expected)
            {
                streak++;
                expected = expected.AddDays(-1);
            }
            else if (d < expected)
            {
                break;
            }
        }
        return streak;
    }

    private static (DateTime Start, DateTime End) GetPeriodRange(GoalPeriod period, DateTime now)
    {
        return period switch
        {
            GoalPeriod.Daily => (now.Date, now.Date.AddDays(1)),
            GoalPeriod.Weekly => (StartOfWeek(now), StartOfWeek(now).AddDays(7)),
            _ => (StartOfMonth(now), StartOfMonth(now).AddMonths(1))
        };
    }

    private static DateTime StartOfWeek(DateTime now)
    {
        var delta = ((int)now.DayOfWeek + 6) % 7; // 周一为起点
        return now.Date.AddDays(-delta);
    }

    private static DateTime StartOfMonth(DateTime now) => new(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    private async Task<(int TotalMinutes, int LongestStreak, int MorningMinutes, int NightMinutes, int WeekStudyDays)> GetLifeStatsAsync(long userId, CancellationToken ct)
    {
        var sessions = await _db.StudySessions
            .Where(s => s.UserId == userId && s.Status == StudySessionStatus.Completed)
            .ToListAsync(ct);

        var total = sessions.Sum(s => s.DurationMinutes);
        var morning = sessions.Where(s => s.StartedAt.Hour < 9).Sum(s => s.DurationMinutes);
        var night = sessions.Where(s => s.StartedAt.Hour >= 21).Sum(s => s.DurationMinutes);

        var (wStart, _) = GetPeriodRange(GoalPeriod.Weekly, DateTime.UtcNow);
        var weekDays = sessions.Where(s => s.StartedAt >= wStart).Select(s => s.StartedAt.Date).Distinct().Count();

        var days = sessions.Select(s => s.StartedAt.Date).Distinct().OrderBy(d => d).ToList();
        var streak = 0;
        var longest = 0;
        for (var i = 0; i < days.Count; i++)
        {
            streak = (i > 0 && (days[i] - days[i - 1]).TotalDays == 1) ? streak + 1 : 1;
            longest = Math.Max(longest, streak);
        }

        return (total, longest, morning, night, weekDays);
    }

    private async Task CheckAchievementsAsync(long userId, CancellationToken ct)
    {
        var earned = await _db.StudyAchievements
            .Where(a => a.UserId == userId)
            .Select(a => a.Code)
            .ToListAsync(ct);

        var stats = await GetLifeStatsAsync(userId, ct);

        var toCheck = new (string Code, bool Met)[]
        {
            ("first_study", true),
            ("seven_days", stats.LongestStreak >= 7),
            ("hundred_hours", stats.TotalMinutes >= 6000),
            ("morning", stats.MorningMinutes >= 600),
            ("night", stats.NightMinutes >= 600),
            ("long_term", stats.LongestStreak >= 30),
            ("fifty_hours", stats.TotalMinutes >= 3000),
            ("first_week", stats.WeekStudyDays >= 5)
        };

        var newOnes = toCheck
            .Where(x => x.Met && !earned.Contains(x.Code))
            .ToList();

        foreach (var item in newOnes)
        {
            _db.StudyAchievements.Add(new StudyAchievement
            {
                UserId = userId,
                Code = item.Code,
                EarnedAt = DateTime.UtcNow
            });
        }
        if (newOnes.Count > 0) await _db.SaveChangesAsync(ct);
    }

    private static StudySessionDto ToSessionDto(StudySession s) => new()
    {
        Id = s.Id,
        Type = s.Type.ToString(),
        VenueId = s.VenueId,
        StartedAt = s.StartedAt,
        EndedAt = s.EndedAt,
        DurationMinutes = s.DurationMinutes,
        Status = s.Status.ToString()
    };
}
