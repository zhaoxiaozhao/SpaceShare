using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/reading")]
[Authorize]
public class ReadingController : ControllerBase
{
    private readonly ReadingService _reading;
    private readonly BookListShareService _bookLists;
    private readonly ICurrentUser _currentUser;

    public ReadingController(ReadingService reading, BookListShareService bookLists, ICurrentUser currentUser)
    {
        _reading = reading;
        _bookLists = bookLists;
        _currentUser = currentUser;
    }

    // ============ 书籍 ============
    [HttpPost("books")]
    public async Task<ActionResult<ReadingBookDto>> CreateBook([FromBody] CreateReadingBookRequest request, CancellationToken ct)
        => Ok(await _reading.CreateBookAsync(_currentUser.UserId!.Value, request, ct));

    [HttpPut("books/{id:long}")]
    public async Task<ActionResult<ReadingBookDto>> UpdateBook(long id, [FromBody] UpdateReadingBookRequest request, CancellationToken ct)
        => Ok(await _reading.UpdateBookAsync(_currentUser.UserId!.Value, id, request, ct));

    [HttpDelete("books/{id:long}")]
    public async Task<IActionResult> DeleteBook(long id, CancellationToken ct)
    {
        await _reading.DeleteBookAsync(_currentUser.UserId!.Value, id, ct);
        return Ok();
    }

    [HttpGet("books")]
    public async Task<ActionResult<ReadingBookListDto>> GetBooks([FromQuery] string? status, CancellationToken ct)
        => Ok(await _reading.GetBooksAsync(_currentUser.UserId!.Value, status, ct));

    [HttpGet("books/{id:long}")]
    public async Task<ActionResult<ReadingBookDto>> GetBook(long id, CancellationToken ct)
    {
        var book = await _reading.GetBookAsync(_currentUser.UserId!.Value, id, ct);
        if (book is null) return NotFound();
        return Ok(book);
    }

    // ============ 阅读会话 ============
    [HttpPost("books/{id:long}/start")]
    public async Task<ActionResult<ReadingSessionDto>> Start(long id, [FromBody] StartReadingRequest? request, CancellationToken ct)
        => Ok(await _reading.StartAsync(_currentUser.UserId!.Value, id, request?.VenueId, ct));

    [HttpPost("books/{id:long}/end")]
    public async Task<ActionResult<ReadingSessionDto>> End(long id, [FromBody] EndReadingRequest? request, CancellationToken ct)
        => Ok(await _reading.EndActiveAsync(_currentUser.UserId!.Value, request?.Progress, request?.LastPosition, ct));

    [HttpPost("end-active")]
    public async Task<ActionResult<ReadingSessionDto>> EndActive([FromBody] EndReadingRequest? request, CancellationToken ct)
        => Ok(await _reading.EndActiveAsync(_currentUser.UserId!.Value, request?.Progress, request?.LastPosition, ct));

    [HttpGet("sessions")]
    public async Task<ActionResult<List<ReadingSessionDto>>> GetSessions([FromQuery] int take, CancellationToken ct)
        => Ok(await _reading.GetSessionsAsync(_currentUser.UserId!.Value, take, ct));

    // ============ 摘抄/笔记 ============
    [HttpPost("books/{id:long}/notes")]
    public async Task<ActionResult<ReadingNoteDto>> AddNote(long id, [FromBody] CreateReadingNoteRequest request, CancellationToken ct)
        => Ok(await _reading.AddNoteAsync(_currentUser.UserId!.Value, id, request, ct));

    [HttpDelete("notes/{id:long}")]
    public async Task<IActionResult> DeleteNote(long id, CancellationToken ct)
    {
        await _reading.DeleteNoteAsync(_currentUser.UserId!.Value, id, ct);
        return Ok();
    }

    [HttpGet("books/{id:long}/notes")]
    public async Task<ActionResult<List<ReadingNoteDto>>> GetNotes(long id, [FromQuery] string? type, CancellationToken ct)
        => Ok(await _reading.GetNotesAsync(_currentUser.UserId!.Value, id, type, ct));

    // ============ 统计与报告 ============
    [HttpGet("stats")]
    public async Task<ActionResult<ReadingStatsDto>> GetStats(CancellationToken ct)
        => Ok(await _reading.GetStatsAsync(_currentUser.UserId!.Value, ct));

    [HttpGet("calendar")]
    public async Task<ActionResult<List<ReadingCalendarDayDto>>> GetCalendar([FromQuery] int? year, CancellationToken ct)
        => Ok(await _reading.GetCalendarAsync(_currentUser.UserId!.Value, year ?? DateTime.UtcNow.Year, ct));

    [HttpGet("yearly-report")]
    public async Task<ActionResult<ReadingYearlyReportDto>> GetYearlyReport([FromQuery] int? year, CancellationToken ct)
        => Ok(await _reading.GetYearlyReportAsync(_currentUser.UserId!.Value, year ?? DateTime.UtcNow.Year, ct));

    // ============ 书单分享 ============
    [HttpPost("list-shares")]
    public async Task<ActionResult<BookListShareDto>> CreateListShare([FromBody] CreateBookListShareRequest request, CancellationToken ct)
        => Ok(await _bookLists.CreateAsync(_currentUser.UserId!.Value, request, ct));

    [HttpGet("list-shares/my")]
    public async Task<ActionResult<List<BookListShareDto>>> GetMyListShares(CancellationToken ct)
        => Ok(await _bookLists.GetMyAsync(_currentUser.UserId!.Value, ct));

    /// <summary>热门书单榜（仅公开书单，匿名可看）</summary>
    [HttpGet("list-shares/board")]
    [AllowAnonymous]
    public async Task<ActionResult<List<BookListShareBoardItemDto>>> GetListShareBoard([FromQuery] int take, CancellationToken ct)
        => Ok(await _bookLists.GetBoardAsync(take, ct));

    [HttpPost("list-shares/{token}/favorite")]
    public async Task<ActionResult<BookListShareDto>> ToggleListShareFavorite(string token, CancellationToken ct)
        => Ok(await _bookLists.ToggleFavoriteAsync(_currentUser.UserId!.Value, token, ct));

    [HttpPost("list-shares/{token}/visibility")]
    public async Task<ActionResult<BookListShareDto>> SetListShareVisibility(string token, [FromBody] BookListShareVisibilityRequest request, CancellationToken ct)
        => Ok(await _bookLists.SetVisibilityAsync(_currentUser.UserId!.Value, token, request.IsPublic, ct));

    [HttpDelete("list-shares/{token}")]
    public async Task<IActionResult> DeleteListShare(string token, CancellationToken ct)
    {
        await _bookLists.DeleteAsync(_currentUser.UserId!.Value, token, ct);
        return Ok();
    }

    /// <summary>书单分享落地页（匿名查看）</summary>
    [HttpGet("list-shares/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<BookListShareDto>> GetListShare(string token, CancellationToken ct)
    {
        var share = await _bookLists.GetByTokenAsync(token, _currentUser.UserId, ct);
        if (share is null) return NotFound();
        return Ok(share);
    }

    /// <summary>书单封面（后端代理云存储，base64，供海报绘制；匿名）</summary>
    [HttpGet("list-shares/{token}/covers")]
    [AllowAnonymous]
    public async Task<ActionResult<BookListCoversDto>> GetListShareCovers(string token, CancellationToken ct)
        => Ok(await _bookLists.GetCoversAsync(token, ct));
}

public class EndReadingRequest
{
    public int? Progress { get; set; }
    public string? LastPosition { get; set; }
}