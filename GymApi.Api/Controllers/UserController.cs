using GymApi.Api.Models.Requests;
using GymApi.Api.Models.Responses;
using GymApi.Application.UserManagement;
using GymApi.Domain.UserManagement;
using GymApi.Infrastructure.UserManagement;
using Microsoft.AspNetCore.Mvc;

namespace GymApi.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
public sealed class UserController(
    IUserService userService, 
    IUserRepository userRepository, 
    IUserContext userContext) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var user = await userService.RegisterAsync(request.Email, request.Name, ct);
        return Ok(UserResponse.From(user));
    }

    /// <summary>
    /// Mock login endpoint to simulate authentication.
    /// In production, this will be replaced by Supabase JWT authentication.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct);
        if (user == null) return NotFound("User not found.");

        if (userContext is MockUserContext mock)
        {
            mock.UserId = user.Id;
        }

        return Ok(UserResponse.From(user));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var user = await userService.GetCurrentUserAsync(ct);
        if (user == null) return Unauthorized();
        return Ok(UserResponse.From(user));
    }
}
