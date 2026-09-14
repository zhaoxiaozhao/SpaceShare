using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/options")]
public class OptionsController : ControllerBase
{
    private readonly ConfigOptionsService _options;

    public OptionsController(ConfigOptionsService options)
    {
        _options = options;
    }

    /// <summary>可配置选项（活动分类/换座原因/座位标签），供小程序拉取</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ConfigOptionsDto>> Get(CancellationToken ct)
    {
        return Ok(await _options.GetAllAsync(ct));
    }
}
