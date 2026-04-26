using GymApi.Domain.UserManagement;

namespace GymApi.Api.Models.Responses;

public sealed record UserResponse(Guid Id, string Email, string Name)
{
    public static UserResponse From(User user)
    {
        return new UserResponse(user.Id, user.Email, user.Name);
    }
}