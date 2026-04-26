using System.Collections.Concurrent;
using GymApi.Domain.UserManagement;

namespace GymApi.Infrastructure.UserManagement;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = _users.Values.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task SaveAsync(User user, CancellationToken ct = default)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }
}
