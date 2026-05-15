using GymApi.Domain.UserManagement;

namespace GymApi.Application.UserManagement;

public sealed class UserService(IUserRepository userRepository, IUserContext userContext) : IUserService
{
    public async Task<User?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated)
        {
            return null;
        }

        return await userRepository.GetByIdAsync(userContext.UserId!.Value, ct);
    }
}
