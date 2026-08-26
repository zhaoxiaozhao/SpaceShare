using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class ReadingService
{
    private static readonly TimeZoneInfo ChinaTz = TimeZoneInfo.CreateCustomTimeZone(
        "China Standard Time", TimeSpan.FromHours(8), "China Standard Time", "China Standard Time");

    private static DateTime ToCn(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, ChinaTz);
    private static DateTime CnDayStartUtc(DateTime cnDate) => TimeZoneInfo.ConvertTimeToUtc(cnDate.Date, ChinaTz);

    private readonly IAppDbContext _db;

    public ReadingService(IAppDbContext db)
    {
        _db = db;
    }

    // ============ 书籍 CRUD ============
    public async Task<ReadingBookDto> CreateBookAsync(long userId, CreateReadingBookRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw AppException.BadRequest("title_required", "书名不能为空");

        var venueName = await ResolveVenueNameAsync(request.VenueId, ct);

        var book = new ReadingBook
        {
            UserId = userId,
            Title = request.Title.Trim(),
            Author = request.Author,
            CoverUrl = request.CoverUrl,
            VenueId = request.VenueId,
            VenueName = venueName,
            Status = ParseBookStatus(request.Status),
            CurrentProgress = Math.Max(0, request.CurrentProgress),
            TotalPages = request.TotalPages is > 0 ? request.TotalPages : null,
            LastPosition = request.LastPosition,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ReadingBooks.Add(book);
        await _db.SaveChangesAsync(ct);

        return await GetBookDtoAsync(book.Id, ct) ?? throw AppException.NotFound();
    }

    public async Task<ReadingBookDto> UpdateBookAsync(long userId, long bookId, UpdateReadingBookRequest request, CancellationToken ct = default)
    {
        var book = await _db.ReadingBooks.FirstOrDefaultAsync(b => b.Id == bookId && b.UserId == userId, ct)
            ?? throw AppException.NotFound("书籍不存在");

        if (!string.IsNullOrWhiteSpace(request.Title)) book.Title = request.Title.Trim();
        if (request.Author != null) book.Author = request.Author;
        if (request.CoverUrl != null) book.CoverUrl = request.CoverUrl;
        if (request.VenueId.HasValue)
        {
            book.VenueId = request.VenueId;
            book.VenueName = await ResolveVenueNameAsync(request.VenueId, ct);
        }
        book.Status = ParseBookStatus(request.Status);
        book.CurrentProgress = Math.Max(0, request.CurrentProgress);
        book.TotalPages = request.TotalPages is > 0 ? request.TotalPages : null;
        if (request.LastPosition != null) book.LastPosition = request.LastPosition;
        book.UpdatedAt = DateTime.UtcNow;

        // 进度达到总页数 → 自动标记已读
        if (book.TotalPages.HasValue && book.CurrentProgress >= book.TotalPages.Value)
        {
            book.Status = BookStatus.Finished;
        }

        await _db.SaveChangesAsync(ct);
        return await GetBookDtoAsync(book.Id, ct) ?? throw AppException.NotFound();
    }

    public async Task DeleteBookAsync(long userId, long bookId, CancellationToken ct = default)
    {
        var book = await _db.ReadingBooks.FirstOrDefaultAsync(b => b.Id == bookId && b.UserId == userId, ct)
            ?? throw AppException.NotFound("书籍不存在");

        var sessions = await _db.ReadingSessions.Where(s => s.BookId == bookId).ToListAsync(ct);
        var notes = await _db.ReadingNotes.Where(n => n.BookId == bookId).ToListAsync(ct);
        _db.ReadingSessions.RemoveRange(sessions);
        _db.ReadingNotes.RemoveRange(notes);
        _db.ReadingBooks.Remove(book);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ReadingBookListDto> GetBooksAsync(long userId, string? status, CancellationToken ct = default)
    {
        var query = _db.ReadingBooks.Where(b => b.UserId == userId);
        if (Enum.TryParse<BookStatus>(status, true, out var st))
        {
            query = query.Where(b => b.Status == st);
        }

        var books = await query.OrderByDescending(b => b.UpdatedAt).ToListAsync(ct);

        var nowUtc = DateTime.UtcNow;
        var todayStartUtc = CnDayStartUtc(ToCn(nowUtc));
        var todayMinutes = await _db.ReadingSessions
            .Where(s => s.UserId == userId && s.StartedAt >= todayStartUtc && s.Status == ReadingSessionStatus.Completed)
            .SumAsync(s => (int?)s.DurationMinutes, ct) ?? 0;

        var active = await _db.ReadingSessions
            .Where(s => s.UserId == userId && s.Status == ReadingSessionStatus.Active)
            .Select(s => s.BookId)
            .ToListAsync(ct);
        var activeSet = active.ToHashSet();

        return new ReadingBookListDto
        {
            Books = books.Select(b => ToBookDto(b, activeSet.Contains(b.Id))).ToList(),
            ReadingCount = books.Count(b => b.Status == BookStatus.Reading),
            FinishedCount = books.Count(b => b.Status == BookStatus.Finished),
            WantToReadCount = books.Count(b => b.Status == BookStatus.WantToRead),
            TodayMinutes = todayMinutes
        };
    }

    public async Task<ReadingBookDto?> GetBookAsync(long userId, long bookId, CancellationToken ct = default)
    {
        var book = await _db.ReadingBooks.FirstOrDefaultAsync(b => b.Id == bookId && b.UserId == userId, ct);
        if (book is null) return null;
        var hasActive = await _db.ReadingSessions.AnyAsync(
            s => s.UserId == userId && s.BookId == bookId && s.Status == ReadingSessionStatus.Active, ct);
        return ToBookDto(book, hasActive);
    }

    // ============ 阅读会话 ============
    public async Task<ReadingSessionDto> StartAsync(long userId, long bookId, long? venueId, CancellationToken ct = default)
    {
        var book = await _db.ReadingBooks.FirstOrDefaultAsync(b => b.Id == bookId && b.UserId == userId, ct)
            ?? throw AppException.NotFound("书籍不存在");

        var active = await _db.ReadingSessions.FirstOrDefaultAsync(
            s => s.UserId == userId && s.Status == ReadingSessionStatus.Active, ct);
        if (active is not null)
        {
            throw AppException.Conflict("reading_already_active", "已有进行中的阅读，请先结束当前阅读");
        }

        var session = new ReadingSession
        {
            UserId = userId,
            BookId = bookId,
            VenueId = venueId ?? book.VenueId,
            VenueName = venueId.HasValue ? await ResolveVenueNameAsync(venueId, ct) : book.VenueName,
            StartedAt = DateTime.UtcNow,
            Status = ReadingSessionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _db.ReadingSessions.Add(session);

        if (book.Status == BookStatus.WantToRead) book.Status = BookStatus.Reading;
        book.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return new ReadingSessionDto
        {
            Id = session.Id,
            BookId = bookId,
            BookTitle = book.Title,
            VenueId = session.VenueId,
            VenueName = session.VenueName,
            StartedAt = session.StartedAt,
            Status = session.Status.ToString()
        };
    }

    public async Task<ReadingSessionDto> EndAsync(long userId, long sessionId, int? progress, string? lastPosition, CancellationToken ct = default)
    {
        var session = await _db.ReadingSessions
            .Include(s => s.Book)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct)
            ?? throw AppException.NotFound("阅读记录不存在");
        if (session.Status != ReadingSessionStatus.Active)
            throw AppException.BadRequest("reading_not_active", "该阅读已结束");

        var endedAt = DateTime.UtcNow;
        var minutes = Math.Max(1, (int)Math.Round((endedAt - session.StartedAt).TotalMinutes));
        session.EndedAt = endedAt;
        session.DurationMinutes = minutes;
        session.Status = ReadingSessionStatus.Completed;

        if (session.Book is not null)
        {
            session.Book.TotalMinutes += minutes;
            session.Book.UpdatedAt = DateTime.UtcNow;
            if (progress.HasValue)
            {
                session.Book.CurrentProgress = Math.Max(0, progress.Value);
                if (session.Book.TotalPages.HasValue && session.Book.CurrentProgress >= session.Book.TotalPages.Value)
                {
                    session.Book.Status = BookStatus.Finished;
                }
                else if (session.Book.Status == BookStatus.WantToRead)
                {
                    session.Book.Status = BookStatus.Reading;
                }
            }
            if (!string.IsNullOrWhiteSpace(lastPosition)) session.Book.LastPosition = lastPosition;
        }

        await _db.SaveChangesAsync(ct);
        return new ReadingSessionDto
        {
            Id = session.Id,
            BookId = session.BookId,
            BookTitle = session.Book?.Title ?? string.Empty,
            VenueId = session.VenueId,
            VenueName = session.VenueName,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            DurationMinutes = minutes,
            Status = session.Status.ToString()
        };
    }

    public async Task<ReadingSessionDto> EndActiveAsync(long userId, int? progress, string? lastPosition, CancellationToken ct = default)
    {
        var active = await _db.ReadingSessions.FirstOrDefaultAsync(
            s => s.UserId == userId && s.Status == ReadingSessionStatus.Active, ct);
        if (active is null) throw AppException.NotFound("没有进行中的阅读");
        return await EndAsync(userId, active.Id, progress, lastPosition, ct);
    }

    public async Task<List<ReadingSessionDto>> GetSessionsAsync(long userId, int take = 50, CancellationToken ct = default)
    {
        return await _db.ReadingSessions
            .Where(s => s.UserId == userId && s.Status == ReadingSessionStatus.Completed)
            .Include(s => s.Book)
            .OrderByDescending(s => s.StartedAt)
            .Take(take)
            .Select(s => new ReadingSessionDto
            {
                Id = s.Id,
                BookId = s.BookId,
                BookTitle = s.Book!.Title,
                VenueId = s.VenueId,
                VenueName = s.VenueName,
                StartedAt = s.StartedAt,
                EndedAt = s.EndedAt,
                DurationMinutes = s.DurationMinutes,
                Status = s.Status.ToString()
            })
            .ToListAsync(ct);
    }

    // ============ 摘抄/笔记 ============
    public async Task<ReadingNoteDto> AddNoteAsync(long userId, long bookId, CreateReadingNoteRequest request, CancellationToken ct = default)
    {
        var book = await _db.ReadingBooks.AnyAsync(b => b.Id == bookId && b.UserId == userId, ct);
        if (!book) throw AppException.NotFound("书籍不存在");
        if (string.IsNullOrWhiteSpace(request.Content))
            throw AppException.BadRequest("content_required", "内容不能为空");

        var type = request.Type == "Highlight" ? ReadingNoteType.Highlight : ReadingNoteType.Note;
        var note = new ReadingNote
        {
            UserId = userId,
            BookId = bookId,
            Type = type,
            Content = request.Content.Trim(),
            Position = request.Position,
            CreatedAt = DateTime.UtcNow
        };
        _db.ReadingNotes.Add(note);
        await _db.SaveChangesAsync(ct);
        return ToNoteDto(note);
    }

    public async Task DeleteNoteAsync(long userId, long noteId, CancellationToken ct = default)
    {
        var note = await _db.ReadingNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == userId, ct)
            ?? throw AppException.NotFound("笔记不存在");
        _db.ReadingNotes.Remove(note);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<ReadingNoteDto>> GetNotesAsync(long userId, long bookId, string? type, CancellationToken ct = default)
    {
        var query = _db.ReadingNotes.Where(n => n.UserId == userId && n.BookId == bookId);
        if (type == "Highlight") query = query.Where(n => n.Type == ReadingNoteType.Highlight);
        else if (type == "Note") query = query.Where(n => n.Type == ReadingNoteType.Note);

        return await query.OrderByDescending(n => n.CreatedAt)
            .Select(n => new ReadingNoteDto
            {
                Id = n.Id,
                BookId = n.BookId,
                Type = n.Type.ToString(),
                Content = n.Content,
                Position = n.Position,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(ct);
    }

    // ============ 统计与报告 ============
    public async Task<ReadingStatsDto> GetStatsAsync(long userId, CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;
        var nowCn = ToCn(nowUtc);
        var todayStartUtc = CnDayStartUtc(nowCn);

        var weekStart = nowCn.Date.AddDays(-(int)nowCn.DayOfWeek);
        var weekStartUtc = CnDayStartUtc(weekStart);

        var sessions = await _db.ReadingSessions
            .Where(s => s.UserId == userId && s.Status == ReadingSessionStatus.Completed)
            .Select(s => new { s.StartedAt, s.DurationMinutes })
            .ToListAsync(ct);

        var todayMinutes = sessions.Where(s => s.StartedAt >= todayStartUtc).Sum(s => s.DurationMinutes);
        var weekMinutes = sessions.Where(s => s.StartedAt >= weekStartUtc).Sum(s => s.DurationMinutes);
        var totalMinutes = sessions.Sum(s => s.DurationMinutes);

        var active = await _db.ReadingSessions
            .Include(s => s.Book)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == ReadingSessionStatus.Active, ct);

        var books = await _db.ReadingBooks.Where(b => b.UserId == userId).ToListAsync(ct);

        return new ReadingStatsDto
        {
            TodayMinutes = todayMinutes,
            WeekMinutes = weekMinutes,
            TotalMinutes = totalMinutes,
            ConsecutiveDays = await CalcConsecutiveDaysAsync(userId, nowCn, ct),
            ReadingBooks = books.Count(b => b.Status == BookStatus.Reading),
            FinishedBooks = books.Count(b => b.Status == BookStatus.Finished),
            ActiveSession = active is null ? null : new ReadingSessionDto
            {
                Id = active.Id,
                BookId = active.BookId,
                BookTitle = active.Book?.Title ?? string.Empty,
                VenueId = active.VenueId,
                VenueName = active.VenueName,
                StartedAt = active.StartedAt,
                Status = active.Status.ToString()
            }
        };
    }

    public async Task<List<ReadingCalendarDayDto>> GetCalendarAsync(long userId, int year, CancellationToken ct = default)
    {
        var yearStart = new DateTime(year, 1, 1);
        var yearStartUtc = CnDayStartUtc(yearStart);
        var yearEndUtc = CnDayStartUtc(new DateTime(year, 12, 31)).AddDays(1);

        var sessions = await _db.ReadingSessions
            .Where(s => s.UserId == userId && s.Status == ReadingSessionStatus.Completed
                && s.StartedAt >= yearStartUtc && s.StartedAt < yearEndUtc)
            .Select(s => new { s.StartedAt, s.DurationMinutes })
            .ToListAsync(ct);

        return sessions
            .GroupBy(s => ToCn(s.StartedAt).Date.ToString("yyyy-MM-dd"))
            .Select(g => new ReadingCalendarDayDto { Date = g.Key, Minutes = g.Sum(x => x.DurationMinutes) })
            .OrderBy(d => d.Date)
            .ToList();
    }

    public async Task<ReadingYearlyReportDto> GetYearlyReportAsync(long userId, int year, CancellationToken ct = default)
    {
        var yearStartUtc = CnDayStartUtc(new DateTime(year, 1, 1));
        var yearEndUtc = CnDayStartUtc(new DateTime(year, 12, 31)).AddDays(1);

        var sessions = await _db.ReadingSessions
            .Where(s => s.UserId == userId && s.Status == ReadingSessionStatus.Completed
                && s.StartedAt >= yearStartUtc && s.StartedAt < yearEndUtc)
            .Select(s => new { s.StartedAt, s.DurationMinutes })
            .ToListAsync(ct);

        var totalMinutes = sessions.Sum(s => s.DurationMinutes);
        var readingDays = sessions.Select(s => ToCn(s.StartedAt).Date).Distinct().Count();

        var finishedBooks = await _db.ReadingBooks
            .CountAsync(b => b.UserId == userId && b.Status == BookStatus.Finished, ct);

        var monthly = sessions
            .GroupBy(s => ToCn(s.StartedAt).ToString("yyyy-MM"))
            .Select(g => new ReadingCalendarDayDto { Date = g.Key, Minutes = g.Sum(x => x.DurationMinutes) })
            .OrderBy(d => d.Date)
            .ToList();

        var dailyMinutes = sessions
            .GroupBy(s => ToCn(s.StartedAt).Date)
            .Select(g => new KeyValuePair<string, int>(g.Key.ToString("MM-dd"), g.Sum(x => x.DurationMinutes)))
            .OrderBy(kv => kv.Key)
            .ToList();

        return new ReadingYearlyReportDto
        {
            Year = year,
            TotalMinutes = totalMinutes,
            ReadingDays = readingDays,
            SessionsCount = sessions.Count,
            BooksFinished = finishedBooks,
            LongestStreak = await CalcLongestStreakAsync(sessions.Select(s => ToCn(s.StartedAt).Date).Distinct().ToList(), ct),
            MonthlyMinutes = monthly,
            DailyMinutes = dailyMinutes
        };
    }

    // ============ 辅助 ============
    private async Task<ReadingBookDto?> GetBookDtoAsync(long bookId, CancellationToken ct)
    {
        var book = await _db.ReadingBooks.FirstOrDefaultAsync(b => b.Id == bookId, ct);
        if (book is null) return null;
        var hasActive = await _db.ReadingSessions.AnyAsync(
            s => s.BookId == bookId && s.Status == ReadingSessionStatus.Active, ct);
        return ToBookDto(book, hasActive);
    }

    private static ReadingBookDto ToBookDto(ReadingBook b, bool hasActive)
    {
        var percent = b.TotalPages is > 0 ? Math.Round(b.CurrentProgress * 100.0 / b.TotalPages.Value, 0) : 0;
        return new ReadingBookDto
        {
            Id = b.Id,
            Title = b.Title,
            Author = b.Author,
            CoverUrl = b.CoverUrl,
            VenueId = b.VenueId,
            VenueName = b.VenueName,
            Status = b.Status.ToString(),
            CurrentProgress = b.CurrentProgress,
            TotalPages = b.TotalPages,
            ProgressPercent = percent,
            LastPosition = b.LastPosition,
            TotalMinutes = b.TotalMinutes,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt,
            HasActiveSession = hasActive
        };
    }

    private static ReadingNoteDto ToNoteDto(ReadingNote n) => new()
    {
        Id = n.Id,
        BookId = n.BookId,
        Type = n.Type.ToString(),
        Content = n.Content,
        Position = n.Position,
        CreatedAt = n.CreatedAt
    };

    private static BookStatus ParseBookStatus(string status) =>
        Enum.TryParse<BookStatus>(status, true, out var s) ? s : BookStatus.WantToRead;

    private async Task<string?> ResolveVenueNameAsync(long? venueId, CancellationToken ct)
    {
        if (!venueId.HasValue) return null;
        return await _db.Venues.Where(v => v.Id == venueId.Value).Select(v => v.Name).FirstOrDefaultAsync(ct);
    }

    // 连续阅读天数（中国时区）
    private async Task<int> CalcConsecutiveDaysAsync(long userId, DateTime cnNow, CancellationToken ct)
    {
        var dates = await _db.ReadingSessions
            .Where(s => s.UserId == userId && s.Status == ReadingSessionStatus.Completed)
            .Select(s => s.StartedAt)
            .ToListAsync(ct);

        var daySet = dates.Select(d => TimeZoneInfo.ConvertTimeFromUtc(d, ChinaTz).Date).ToHashSet();
        if (!daySet.Contains(cnNow.Date)) return 0;

        var count = 0;
        var cursor = cnNow.Date;
        while (daySet.Contains(cursor))
        {
            count++;
            cursor = cursor.AddDays(-1);
        }
        return count;
    }

    private static async Task<int> CalcLongestStreakAsync(List<DateTime> dates, CancellationToken ct)
    {
        if (dates.Count == 0) return 0;
        var daySet = dates.ToHashSet();
        var maxStreak = 0;
        var currentStreak = 0;
        DateTime? prev = null;
        foreach (var d in daySet.OrderBy(x => x))
        {
            currentStreak = prev.HasValue && (d - prev.Value).Days == 1 ? currentStreak + 1 : 1;
            if (currentStreak > maxStreak) maxStreak = currentStreak;
            prev = d;
        }
        return maxStreak;
    }
}