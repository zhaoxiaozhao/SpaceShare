using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/seats")]
[AllowAnonymous]
public class SeatsController : ControllerBase
{
    private readonly VenueService _venues;
    private readonly ShareService _shares;

    public SeatsController(VenueService venues, ShareService shares)
    {
        _venues = venues;
        _shares = shares;
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SeatDto>> Get(long id, CancellationToken ct)
    {
        var seat = await _venues.GetSeatAsync(id, ct);
        if (seat is null) return NotFound();
        return Ok(seat);
    }

    [HttpGet("{id:long}/shares")]
    public async Task<ActionResult<List<SeatShareDto>>> GetShares(long id, CancellationToken ct)
    {
        return Ok(await _shares.GetSharesBySeatIdsAsync(new List<long> { id }, ct));
    }
}
