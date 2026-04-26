namespace GymApi.Domain.UserManagement;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task SaveAsync(User user, CancellationToken ct = default);
}