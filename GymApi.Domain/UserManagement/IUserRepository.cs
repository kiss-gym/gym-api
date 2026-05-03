namespace GymApi.Domain.UserManagement;

public interface IUserRepository
{
    // ReSharper disable UnusedParameter.Global
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task SaveAsync(User user, CancellationToken ct = default);
    // ReSharper restore UnusedParameter.Global
}
