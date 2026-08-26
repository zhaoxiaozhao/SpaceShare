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

    /// <summary>根据定位反查最近的可用城市（用于小程序自动识别所在城市）</summary>
    [HttpGet("nearest")]
    public async Task<ActionResult<CityDto?>> GetNearestCity([FromQuery] double lat, [FromQuery] double lng, CancellationToken ct)
    {
        return Ok(await _venueService.GetNearestCityAsync(lat, lng, ct));
    }
}
