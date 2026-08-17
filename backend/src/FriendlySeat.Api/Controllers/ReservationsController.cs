using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly ReservationService _reservations;
    private readonly ICurrentUser _currentUser;

    public ReservationsController(ReservationService reservations, ICurrentUser currentUser)
    {
        _reservations = reservations;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create([FromBody] ReservationCreateRequest request, CancellationToken ct)
    {
        return Ok(await _reservations.CreateAsync(_currentUser.UserId!.Value, request, ct));
    }

    [HttpGet("my")]
    public async Task<ActionResult<MyReservationSummaryDto>> GetMy(CancellationToken ct)
    {
        return Ok(await _reservations.GetMySummaryAsync(_currentUser.UserId!.Value, ct));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ReservationDto>> Get(long id, CancellationToken ct)
    {
        var result = await _reservations.GetMySummaryAsync(_currentUser.UserId!.Value, ct);
        var reservation = result.Upcoming.Concat(result.History).FirstOrDefault(r => r.Id == id);
        if (reservation is null) return NotFound();
        return Ok(reservation);
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        await _reservations.CancelAsync(id, _currentUser.UserId!.Value, ct);
        return Ok();
    }

    [HttpPost("{id:long}/arrive")]
    public async Task<ActionResult<ArrivalResultDto>> Arrive(long id, [FromQuery] double? lat, [FromQuery] double? lng, CancellationToken ct)
    {
        return Ok(await _reservations.ArriveAsync(id, _currentUser.UserId!.Value, lat, lng, ct));
    }

    [HttpPost("{id:long}/complete")]
    public async Task<IActionResult> Complete(long id, CancellationToken ct)
    {
        await _reservations.CompleteAsync(id, _currentUser.UserId!.Value, ct);
        return Ok();
    }
}
