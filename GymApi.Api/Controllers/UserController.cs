using GymApi.Api.Models.Requests;
using GymApi.Api.Models.Responses;
using GymApi.Application.UserManagement;
using GymApi.Domain.UserManagement;
using GymApi.Infrastructure.UserManagement;
using Microsoft.AspNetCore.Mvc;

namespace GymApi.Api.Controllers;

/// <summary>User Management — generic subdomain.</summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public sealed class UserController(
    IUserService userService,
    IUserRepository userRepository) : ControllerBase
{
    /// <summary>Register a new user.</summary>
    [HttpPost("register")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var user = await userService.RegisterAsync(request.Email, request.Name, ct);
        return Ok(UserResponse.From(user));
    }

    /// <summary>
    /// Login a user.
    /// The client will authenticate directly with Supabase.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct);
        if (user == null)
        {
            return NotFound("User not found.");
        }
        
        return Ok(UserResponse.From(user));
    }

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
