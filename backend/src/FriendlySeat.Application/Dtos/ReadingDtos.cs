namespace FriendlySeat.Application.Dtos;

public class ReadingBookDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? CoverUrl { get; set; }
    public long? VenueId { get; set; }
    public string? VenueName { get; set; }
    public string Status { get; set; } = "WantToRead";
    public int CurrentProgress { get; set; }
    public int? TotalPages { get; set; }
    public double ProgressPercent { get; set; }
    public string? LastPosition { get; set; }
    public int TotalMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool HasActiveSession { get; set; }
}

public class ReadingBookListDto
{
    public List<ReadingBookDto> Books { get; set; } = new();
    public int ReadingCount { get; set; }
    public int FinishedCount { get; set; }
    public int WantToReadCount { get; set; }
    public int TodayMinutes { get; set; }
}

public class CreateReadingBookRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? CoverUrl { get; set; }
    public long? VenueId { get; set; }
    public string Status { get; set; } = "WantToRead";
    public int CurrentProgress { get; set; }
    public int? TotalPages { get; set; }
    public string? LastPosition { get; set; }
}

public class UpdateReadingBookRequest
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? CoverUrl { get; set; }
    public long? VenueId { get; set; }
    public string Status { get; set; } = "WantToRead";
    public int CurrentProgress { get; set; }
    public int? TotalPages { get; set; }
    public string? LastPosition { get; set; }
}

public class StartReadingRequest
{
    public long? VenueId { get; set; }
}

public class EndReadingRequest
{
    public int? Progress { get; set; }
    public string? LastPosition { get; set; }
}

public class ReadingSessionDto
{
    public long Id { get; set; }
    public long BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public long? VenueId { get; set; }
    public string? VenueName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "Active";
}

public class ReadingNoteDto
{
    public long Id { get; set; }
    public long BookId { get; set; }
    public string Type { get; set; } = "Note";
    public string Content { get; set; } = string.Empty;
    public string? Position { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReadingNoteRequest
{
    public string Type { get; set; } = "Note";
    public string Content { get; set; } = string.Empty;
    public string? Position { get; set; }
}

public class ReadingStatsDto
{
    public int TodayMinutes { get; set; }
    public int WeekMinutes { get; set; }
    public int TotalMinutes { get; set; }
    public int ConsecutiveDays { get; set; }
    public int ReadingBooks { get; set; }
    public int FinishedBooks { get; set; }
    public ReadingSessionDto? ActiveSession { get; set; }
}

public class ReadingCalendarDayDto
{
    public string Date { get; set; } = string.Empty;
    public int Minutes { get; set; }
}

public class ReadingYearlyReportDto
{
    public int Year { get; set; }
    public int TotalMinutes { get; set; }
    public int ReadingDays { get; set; }
    public int SessionsCount { get; set; }
    public int BooksFinished { get; set; }
    public int LongestStreak { get; set; }
    public List<ReadingCalendarDayDto> MonthlyMinutes { get; set; } = new();
    public List<KeyValuePair<string, int>> DailyMinutes { get; set; } = new();
}