using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/cities")]
public class CitiesController : ControllerBase
{
    private readonly VenueService _venueService;

    public CitiesController(VenueService venueService)
    {
        _venueService = venueService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CityDto>>> GetCities(CancellationToken ct)
    {
        return Ok(await _venueService.GetCitiesAsync(ct));
    }
}
