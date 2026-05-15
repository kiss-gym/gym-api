using GymApi.Api.Models.Responses;
using GymApi.Application.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymApi.Api.Controllers;

/// <summary>User Management — generic subdomain.</summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize]
public sealed class UserController(IUserService userService) : ControllerBase
{
    /// <summary>Get the current authenticated user profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var user = await userService.GetCurrentUserAsync(ct);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(UserResponse.From(user));
    }
}
