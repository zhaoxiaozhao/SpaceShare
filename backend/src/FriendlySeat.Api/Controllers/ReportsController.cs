using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reports;
    private readonly ICurrentUser _currentUser;

    public ReportsController(ReportService reports, ICurrentUser currentUser)
    {
        _reports = reports;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<ActionResult<ReportDto>> Create([FromBody] ReportCreateRequest request, CancellationToken ct)
    {
        return Ok(await _reports.CreateAsync(_currentUser.UserId!.Value, request, ct));
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<ReportDto>>> GetMy(CancellationToken ct)
    {
        return Ok(await _reports.GetMyAsync(_currentUser.UserId!.Value, ct));
    }
}
