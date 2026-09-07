
using FriendlySeat.Application.Services;
using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers.Admin;

public class AdminVenuesController : AdminControllerBase
{
    private readonly AdminVenueManagementService _venues;
    private readonly ICurrentAdmin _currentAdmin;

    public AdminVenuesController(AdminVenueManagementService venues, ICurrentAdmin currentAdmin)
    {
        _venues = venues;
        _currentAdmin = currentAdmin;
    }

    [HttpGet("cities")]
    public async Task<ActionResult<List<CityDto>>> GetCities(CancellationToken ct)
        => Ok(await _venues.GetCitiesAsync(ct));

    [HttpPost("cities")]
    public async Task<ActionResult<CityDto>> CreateCity([FromBody] AdminCityCreateRequest request, CancellationToken ct)
        => Ok(await _venues.CreateCityAsync(request, _currentAdmin.AdminId!.Value, ct));

    [HttpGet("venues")]
    public async Task<ActionResult<List<VenueListItemDto>>> GetVenues(CancellationToken ct)
        => Ok(await _venues.GetVenuesAsync(ct));

    [HttpPost("venues")]
    public async Task<ActionResult<VenueDto>> CreateVenue([FromBody] AdminVenueCreateRequest request, CancellationToken ct)
        => Ok(await _venues.CreateVenueAsync(request, _currentAdmin.AdminId!.Value, ct));

    [HttpPut("venues/{id:long}")]
    public async Task<ActionResult<VenueDto>> UpdateVenue(long id, [FromBody] AdminVenueUpdateRequest request, CancellationToken ct)
        => Ok(await _venues.UpdateVenueAsync(id, request, _currentAdmin.AdminId!.Value, ct));

    /// <summary>场馆显示/隐藏</summary>
    [HttpPost("venues/{id:long}/status")]
    public async Task<IActionResult> SetVenueStatus(long id, [FromQuery] bool visible, CancellationToken ct)
    {
        await _venues.SetVenueStatusAsync(id, visible, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpDelete("venues/{id:long}")]
    public async Task<IActionResult> DeleteVenue(long id, CancellationToken ct)
    {
        await _venues.DeleteVenueAsync(id, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpPut("floors/{id:long}")]
    public async Task<IActionResult> UpdateFloor(long id, [FromBody] AdminFloorRequest request, CancellationToken ct)
    {
        await _venues.UpdateFloorAsync(id, request, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpDelete("floors/{id:long}")]
    public async Task<IActionResult> DeleteFloor(long id, CancellationToken ct)
    {
        await _venues.DeleteFloorAsync(id, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpPut("cities/{id:long}")]
    public async Task<ActionResult<CityDto>> UpdateCity(long id, [FromBody] AdminCityUpdateRequest request, CancellationToken ct)
        => Ok(await _venues.UpdateCityAsync(id, request, _currentAdmin.AdminId!.Value, ct));

    [HttpPost("floors")]
    public async Task<IActionResult> AddFloor([FromBody] AdminFloorRequest request, CancellationToken ct)
    {
        await _venues.AddFloorAsync(request, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpPost("areas")]
    public async Task<ActionResult<long>> AddArea([FromBody] AdminAreaRequest request, CancellationToken ct)
    {
        var id = await _venues.AddAreaAsync(request, _currentAdmin.AdminId!.Value, ct);
        return Ok(id);
    }

    [HttpPut("areas/{id:long}")]
    public async Task<IActionResult> UpdateArea(long id, [FromBody] AdminAreaRequest request, CancellationToken ct)
    {
        await _venues.UpdateAreaAsync(id, request, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpDelete("areas/{id:long}")]
    public async Task<IActionResult> DeleteArea(long id, CancellationToken ct)
    {
        await _venues.DeleteAreaAsync(id, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpPost("zones")]
    public async Task<ActionResult<long>> AddZone([FromBody] AdminZoneRequest request, CancellationToken ct)
    {
        var id = await _venues.AddZoneAsync(request, _currentAdmin.AdminId!.Value, ct);
        return Ok(id);
    }

    [HttpPut("zones/{id:long}")]
    public async Task<IActionResult> UpdateZone(long id, [FromBody] AdminZoneRequest request, CancellationToken ct)
    {
        await _venues.UpdateZoneAsync(id, request, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpDelete("zones/{id:long}")]
    public async Task<IActionResult> DeleteZone(long id, CancellationToken ct)
    {
        await _venues.DeleteZoneAsync(id, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpGet("venues/{id:long}/detail")]
    public async Task<ActionResult<AdminVenueDetailDto>> GetVenueDetail(long id, CancellationToken ct)
    {
        return Ok(await _venues.GetVenueDetailAsync(id, ct));
    }

    [HttpPost("seats")]
    public async Task<ActionResult<long>> AddSeat([FromBody] AdminSeatRequest request, CancellationToken ct)
    {
        var id = await _venues.AddSeatAsync(request, _currentAdmin.AdminId!.Value, ct);
        return Ok(id);
    }

    [HttpPut("seats/{id:long}")]
    public async Task<IActionResult> UpdateSeat(long id, [FromBody] AdminSeatRequest request, CancellationToken ct)
    {
        await _venues.UpdateSeatAsync(id, request, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpDelete("seats/{id:long}")]
    public async Task<IActionResult> DeleteSeat(long id, CancellationToken ct)
    {
        await _venues.DeleteSeatAsync(id, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpPost("pois")]
    public async Task<ActionResult<PoiDto>> AddPoi([FromBody] AdminPoiRequest request, CancellationToken ct)
    {
        return Ok(await _venues.AddPoiAsync(request, _currentAdmin.AdminId!.Value, ct));
    }

    [HttpPut("pois/{id:long}")]
    public async Task<ActionResult<PoiDto>> UpdatePoi(long id, [FromBody] AdminPoiRequest request, CancellationToken ct)
    {
        return Ok(await _venues.UpdatePoiAsync(id, request, _currentAdmin.AdminId!.Value, ct));
    }

    [HttpDelete("pois/{id:long}")]
    public async Task<IActionResult> DeletePoi(long id, CancellationToken ct)
    {
        await _venues.DeletePoiAsync(id, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }

    [HttpPost("seats/{id:long}/status")]
    public async Task<IActionResult> SetSeatStatus(long id, [FromQuery] string status, CancellationToken ct)
    {
        if (!Enum.TryParse<SeatStatus>(status, true, out var parsed))
            return BadRequest(new { message = "状态无效" });
        await _venues.SetSeatStatusAsync(id, parsed, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }
}
