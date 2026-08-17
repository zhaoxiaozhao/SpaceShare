
using FriendlySeat.Application.Services;
using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers.Admin;

public class AdminReportsController : AdminControllerBase
{
    private readonly AdminReportService _reports;
    private readonly ICurrentAdmin _currentAdmin;

    public AdminReportsController(AdminReportService reports, ICurrentAdmin currentAdmin)
    {
        _reports = reports;
        _currentAdmin = currentAdmin;
    }

    [HttpGet("reports")]
    public async Task<ActionResult<List<ReportDto>>> GetReports([FromQuery] string? status, CancellationToken ct)
        => Ok(await _reports.GetReportsAsync(status, ct));

    [HttpPost("reports/{id:long}/handle")]
    public async Task<IActionResult> Handle(long id, [FromQuery] string status, [FromQuery] string? note, CancellationToken ct)
    {
        if (!Enum.TryParse<ReportStatus>(status, true, out var parsed))
            return BadRequest(new { message = "状态无效" });
        await _reports.HandleAsync(id, parsed, note, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpGet("reservations")]
    public async Task<ActionResult<List<AdminReservationDto>>> GetReservations([FromQuery] string? status, CancellationToken ct)
        => Ok(await _reports.GetReservationsAsync(status, ct));

    [HttpPost("reservations/{id:long}/force-cancel")]
    public async Task<IActionResult> ForceCancel(long id, [FromQuery] string? reason, CancellationToken ct)
    {
        await _reports.ForceCancelAsync(id, reason, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<List<AdminAuditLogDto>>> GetAuditLogs(CancellationToken ct)
        => Ok(await _reports.GetAuditLogsAsync(ct));
}
