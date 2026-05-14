using System.Security.Claims;
using GymApi.Domain.UserManagement;
using Microsoft.AspNetCore.Http;

namespace GymApi.Infrastructure.UserManagement;

/// <summary>
/// Reads the current user's ID from the JWT sub claim
/// validated by ASP.NET Core's JwtBearer middleware.
/// </summary>
public sealed class JwtUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid? UserId
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)
                ?.Value;

            if (sub is null)
            {
                return null;
            }

            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }
}
