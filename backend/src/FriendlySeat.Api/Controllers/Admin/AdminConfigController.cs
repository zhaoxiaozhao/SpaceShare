
using FriendlySeat.Application.Services;
using FriendlySeat.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers.Admin;

public class AdminConfigController : AdminControllerBase
{
    private readonly AdminConfigService _config;
    private readonly ICurrentAdmin _currentAdmin;

    public AdminConfigController(AdminConfigService config, ICurrentAdmin currentAdmin)
    {
        _config = config;
        _currentAdmin = currentAdmin;
    }

    [HttpGet("config")]
    public async Task<ActionResult<List<ConfigItemDto>>> GetAll(CancellationToken ct)
        => Ok(await _config.GetAllAsync(ct));

    [HttpPut("config/{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateConfigRequest request, CancellationToken ct)
    {
        await _config.UpdateAsync(id, request.Value, _currentAdmin.AdminId!.Value, ct);
        return Ok();
    }
}

public class UpdateConfigRequest
{
    public string? Value { get; set; }
}
