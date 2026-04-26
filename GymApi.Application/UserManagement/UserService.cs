using GymApi.Domain.UserManagement;

namespace GymApi.Application.UserManagement;

public sealed class UserService(IUserRepository userRepository, IUserContext userContext) : IUserService
{
    public async Task<User?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated) return null;
        return await userRepository.GetByIdAsync(userContext.UserId!.Value, ct);
    }

    public async Task<User> RegisterAsync(string email, string name, CancellationToken ct = default)
    {
        var existing = await userRepository.GetByEmailAsync(email, ct);
        if (existing != null) throw new InvalidOperationException("User already exists.");

        var user = User.Create(Guid.NewGuid(), email, name);
        await userRepository.SaveAsync(user, ct);
        return user;
    }
}
