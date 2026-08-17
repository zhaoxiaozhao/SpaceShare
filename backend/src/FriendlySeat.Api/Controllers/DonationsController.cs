using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/donations")]
[Authorize]
public class DonationsController : ControllerBase
{
    private readonly DonationService _donations;
    private readonly ICurrentUser _currentUser;

    public DonationsController(DonationService donations, ICurrentUser currentUser)
    {
        _donations = donations;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<ActionResult<DonationDto>> Create([FromBody] DonationCreateRequest request, CancellationToken ct)
    {
        return Ok(await _donations.CreateAsync(_currentUser.UserId!.Value, request, ct));
    }

    [HttpGet]
    public async Task<ActionResult<DonationSummaryDto>> Get(CancellationToken ct)
    {
        return Ok(await _donations.GetSummaryAsync(_currentUser.UserId!.Value, ct));
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<DonationDto>>> GetMy(CancellationToken ct)
    {
        var summary = await _donations.GetSummaryAsync(_currentUser.UserId!.Value, ct);
        return Ok(summary.MyDonations);
    }
}
