using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/venues")]
public class VenuesController : ControllerBase
{
    private readonly VenueService _venueService;
    private readonly ShareService _shareService;

    public VenuesController(VenueService venueService, ShareService shareService)
    {
        _venueService = venueService;
        _shareService = shareService;
    }

    [HttpGet]
    public async Task<ActionResult<List<VenueListItemDto>>> GetVenues(
        [FromQuery] long? cityId,
        [FromQuery] string? keyword,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] double? radiusKm,
        CancellationToken ct)
    {
        return Ok(await _venueService.GetVenuesAsync(cityId, keyword, lat, lng, radiusKm, ct));
    }

    [HttpGet("nearby")]
    public async Task<ActionResult<List<VenueListItemDto>>> GetNearby(
        CancellationToken ct,
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double? radiusKm = 10)
    {
        return Ok(await _venueService.GetVenuesAsync(null, null, lat, lng, radiusKm, ct));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<VenueDetailDto>> GetVenue(long id, CancellationToken ct)
    {
        var venue = await _venueService.GetVenueAsync(id, ct);
        if (venue is null) return NotFound();
        return Ok(venue);
    }

    [HttpGet("{id:long}/available-seats")]
    public async Task<ActionResult<List<SeatDto>>> GetAvailableSeats(long id, CancellationToken ct)
    {
        var venue = await _venueService.GetVenueAsync(id, ct);
        if (venue is null) return NotFound();

        var seats = venue.Floors
            .SelectMany(f => f.Zones)
            .SelectMany(z => z.Seats)
            .Where(s => s.Status != "Unavailable")
            .ToList();
        return Ok(seats);
    }

    [HttpGet("{id:long}/shares")]
    public async Task<ActionResult<List<SeatShareDto>>> GetVenueShares(long id, CancellationToken ct)
    {
        var venue = await _venueService.GetVenueAsync(id, ct);
        if (venue is null) return NotFound();

        var seatIds = venue.Floors
            .SelectMany(f => f.Zones)
            .SelectMany(z => z.Seats)
            .Select(s => s.Id)
            .ToList();

        var shares = await _shareService.GetSharesBySeatIdsAsync(seatIds, ct);
        return Ok(shares);
    }
}
