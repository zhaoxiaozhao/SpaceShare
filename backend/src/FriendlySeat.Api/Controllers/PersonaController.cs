using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/persona")]
[Authorize]
public class PersonaController : ControllerBase
{
    private readonly PersonaService _persona;
    private readonly ICurrentUser _currentUser;

    public PersonaController(PersonaService persona, ICurrentUser currentUser)
    {
        _persona = persona;
        _currentUser = currentUser;
    }

    /// <summary>画像题库（匿名可看）</summary>
    [HttpGet("questions")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PersonaQuestionDto>>> Questions(CancellationToken ct)
        => Ok(await _persona.GetQuestionsAsync(ct));

    /// <summary>提交答案生成/更新画像</summary>
    [HttpPost("submit")]
    public async Task<ActionResult<PersonaProfileDto>> Submit([FromBody] PersonaSubmitRequest request, CancellationToken ct)
        => Ok(await _persona.SubmitAsync(_currentUser.UserId!.Value, request, ct));

    /// <summary>我的画像</summary>
    [HttpGet("me")]
    public async Task<ActionResult<PersonaProfileDto>> Me(CancellationToken ct)
    {
        var dto = await _persona.GetMineAsync(_currentUser.UserId!.Value, ct);
        if (dto is null) return NoContent();
        return Ok(dto);
    }

    /// <summary>设置是否公开</summary>
    [HttpPost("visibility")]
    public async Task<ActionResult<PersonaProfileDto>> Visibility([FromBody] PersonaVisibilityRequest request, CancellationToken ct)
        => Ok(await _persona.SetVisibilityAsync(_currentUser.UserId!.Value, request.IsPublic, ct));

    /// <summary>画像海报用：昵称 + 头像</summary>
    [HttpGet("avatar")]
    public async Task<ActionResult<PersonaAvatarDto>> Avatar(CancellationToken ct)
        => Ok(await _persona.GetAvatarAsync(_currentUser.UserId!.Value, ct));
}
