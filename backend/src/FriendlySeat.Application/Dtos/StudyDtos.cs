namespace FriendlySeat.Application.Dtos;

public class StudySessionDto
{
    public long Id { get; set; }
    public string Type { get; set; } = "Other";
    public long? VenueId { get; set; }
    public string? VenueName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "Active";
}

public class StudyTodayDto
{
    public int TodayMinutes { get; set; }
    public int SessionCount { get; set; }
    public int ConsecutiveDays { get; set; }
    public int? TargetMinutes { get; set; }
    public double TargetProgress { get; set; }
    public StudySessionDto? ActiveSession { get; set; }
}

public class StudyGoalDto
{
    public long Id { get; set; }
    public string Period { get; set; } = "Daily";
    public int TargetMinutes { get; set; }
    public int AchievedMinutes { get; set; }
    public double Progress { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class SetStudyGoalRequest
{
    public string Period { get; set; } = "Daily";
    public int TargetMinutes { get; set; }
}

public class StartStudyRequest
{
    public string? Type { get; set; }
    public long? VenueId { get; set; }
    public string? Note { get; set; }
}

public class StudyReportDto
{
    public string Period { get; set; } = "Weekly";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public int TotalMinutes { get; set; }
    public int StudyDays { get; set; }
    public int SessionCount { get; set; }
    public int MaxDailyMinutes { get; set; }
    public int LongestStreak { get; set; }
    public List<KeyValuePair<string, int>> TypeDistribution { get; set; } = new();
    public List<KeyValuePair<string, int>> DailyMinutes { get; set; } = new();
}

public class StudyAchievementDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool Earned { get; set; }
    public DateTime? EarnedAt { get; set; }
}
