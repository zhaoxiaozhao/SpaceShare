using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/study")]
[Authorize]
public class StudyController : ControllerBase
{
    private readonly StudyService _study;
    private readonly ICurrentUser _currentUser;

    public StudyController(StudyService study, ICurrentUser currentUser)
    {
        _study = study;
        _currentUser = currentUser;
    }

    [HttpPost("sessions")]
    public async Task<ActionResult<StudySessionDto>> Start([FromBody] StartStudyRequest request, CancellationToken ct)
    {
        return Ok(await _study.StartAsync(_currentUser.UserId!.Value, request, ct));
    }

    [HttpPost("sessions/{id:long}/end")]
    public async Task<ActionResult<StudySessionDto>> End(long id, CancellationToken ct)
    {
        return Ok(await _study.EndAsync(_currentUser.UserId!.Value, id, ct));
    }

    [HttpPost("sessions/end")]
    public async Task<ActionResult<StudySessionDto>> EndActive(CancellationToken ct)
    {
        return Ok(await _study.EndActiveAsync(_currentUser.UserId!.Value, ct));
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<List<StudySessionDto>>> GetSessions([FromQuery] int take = 50, CancellationToken ct = default)
    {
        return Ok(await _study.GetSessionsAsync(_currentUser.UserId!.Value, take, ct));
    }

    [HttpGet("today")]
    public async Task<ActionResult<StudyTodayDto>> GetToday(CancellationToken ct)
    {
        return Ok(await _study.GetTodayAsync(_currentUser.UserId!.Value, ct));
    }

    [HttpPost("goals")]
    public async Task<ActionResult<StudyGoalDto>> SetGoal([FromBody] SetStudyGoalRequest request, CancellationToken ct)
    {
        return Ok(await _study.SetGoalAsync(_currentUser.UserId!.Value, request, ct));
    }

    [HttpGet("goals")]
    public async Task<ActionResult<List<StudyGoalDto>>> GetGoals(CancellationToken ct)
    {
        return Ok(await _study.GetGoalsAsync(_currentUser.UserId!.Value, ct));
    }

    [HttpGet("report")]
    public async Task<ActionResult<StudyReportDto>> GetReport([FromQuery] string? period, CancellationToken ct)
    {
        return Ok(await _study.GetReportAsync(_currentUser.UserId!.Value, period ?? "weekly", ct));
    }

    [HttpGet("achievements")]
    public async Task<ActionResult<List<StudyAchievementDto>>> GetAchievements(CancellationToken ct)
    {
        return Ok(await _study.GetAchievementsAsync(_currentUser.UserId!.Value, ct));
    }
}
