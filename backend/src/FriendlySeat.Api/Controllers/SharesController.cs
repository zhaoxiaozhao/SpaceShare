using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/shares")]
[Authorize]
public class SharesController : ControllerBase
{
    private readonly ShareService _shares;
    private readonly UserService _users;
    private readonly ICurrentUser _currentUser;

    public SharesController(ShareService shares, UserService users, ICurrentUser currentUser)
    {
        _shares = shares;
        _users = users;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<ActionResult<SeatShareDto>> Create([FromBody] ShareCreateRequest request, CancellationToken ct)
    {
        return Ok(await _shares.CreateShareAsync(_currentUser.UserId!.Value, request, ct));
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<SeatShareDto>>> GetMy(CancellationToken ct)
    {
        return Ok(await _shares.GetMySharesAsync(_currentUser.UserId!.Value, ct));
    }

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<ShareDetailDto>> Get(long id, CancellationToken ct)
    {
        var share = await _shares.GetShareAsync(id, _currentUser.UserId, ct);
        if (share is null) return NotFound();
        return Ok(share);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        await _shares.CancelShareAsync(id, _currentUser.UserId!.Value, ct);
        return Ok();
    }

    [HttpGet("{id:long}/contact")]
    public async Task<ActionResult<ContactResultDto?>> GetContact(long id, CancellationToken ct)
    {
        var contact = await _users.GetShareOwnerContactAsync(_currentUser.UserId!.Value, id, ct);
        return Ok(contact);
    }
}
