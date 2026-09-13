using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers.Admin;

public class AdminSwapController : AdminControllerBase
{
    private readonly SwapService _swap;
    private readonly ICurrentAdmin _currentAdmin;

    public AdminSwapController(SwapService swap, ICurrentAdmin currentAdmin)
    {
        _swap = swap;
        _currentAdmin = currentAdmin;
    }

    [HttpGet("swaps")]
    public async Task<ActionResult<List<SeatSwapDto>>> List([FromQuery] string? status, CancellationToken ct)
        => Ok(await _swap.AdminListAsync(status, ct));

    [HttpPost("swaps/{id:long}/take-down")]
    public async Task<IActionResult> TakeDown(long id, CancellationToken ct)
    {
        await _swap.AdminTakeDownAsync(id, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }
}
