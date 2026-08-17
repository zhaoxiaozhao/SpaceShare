using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers;

[ApiController]
[Route("api/v1/credit")]
[Authorize]
public class CreditController : ControllerBase
{
    private readonly CreditService _credit;
    private readonly ICurrentUser _currentUser;

    public CreditController(CreditService credit, ICurrentUser currentUser)
    {
        _credit = credit;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<CreditSummaryDto>> Get(CancellationToken ct)
    {
        return Ok(await _credit.GetSummaryAsync(_currentUser.UserId!.Value, ct));
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<List<CreditTransactionDto>>> GetTransactions(CancellationToken ct)
    {
        var summary = await _credit.GetSummaryAsync(_currentUser.UserId!.Value, ct);
        return Ok(summary.Transactions);
    }

    [HttpGet("contribution")]
    public async Task<ActionResult<PublicContributionDto>> GetContribution(CancellationToken ct)
    {
        return Ok(await _credit.GetContributionAsync(_currentUser.UserId!.Value, ct));
    }
}
