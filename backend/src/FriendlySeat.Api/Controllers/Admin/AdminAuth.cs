using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendlySeat.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/v1/admin")]
public abstract class AdminControllerBase : ControllerBase
{
}
