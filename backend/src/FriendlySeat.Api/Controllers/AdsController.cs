using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/ads")]
public class AdsController : ControllerBase
{
    private readonly AdService _ads;
    private readonly ICurrentUser _currentUser;

    public AdsController(AdService ads, ICurrentUser currentUser)
    {
        _ads = ads;
        _currentUser = currentUser;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<AdDto>>> Get(CancellationToken ct, [FromQuery] string? placement = "home_feed")
    {
        return Ok(await _ads.GetAdsAsync(placement ?? "home_feed", _currentUser.UserId, ct));
    }

    [HttpPost("{id:long}/click")]
    [Authorize]
    public async Task<IActionResult> Click(long id, CancellationToken ct)
    {
        await _ads.ClickAsync(id, _currentUser.UserId!.Value, ct);
        return Ok();
    }
}
