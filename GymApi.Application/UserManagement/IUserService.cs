using GymApi.Domain.UserManagement;

namespace GymApi.Application.UserManagement;

public interface IUserService
{
    Task<User?> GetCurrentUserAsync(CancellationToken ct = default);
}
