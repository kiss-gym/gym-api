using System.ComponentModel.DataAnnotations;
using GymApi.Domain.UserManagement;

namespace GymApi.Api.Models.Responses;

public sealed record UserResponse(Guid Id, [Required] string Email, [Required] string Name)
{
    public static UserResponse From(User user)
    {
        return new UserResponse(user.Id, user.Email, user.Name);
    }
}
