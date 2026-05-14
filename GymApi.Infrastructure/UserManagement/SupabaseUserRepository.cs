using GymApi.Domain.UserManagement;
using GymApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GymApi.Infrastructure.UserManagement;

/// <summary>
/// Supabase/Postgres implementation of IUserRepository using EF Core.
/// </summary>
public sealed class SupabaseUserRepository(GymApiDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // AsNoTracking: repository returns detached domain objects
        return await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);
    }

    public async Task SaveAsync(User user, CancellationToken ct = default)
    {
        var exists = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == user.Id, ct);

        if (exists)
        {
            // ExecuteUpdate avoids tracking conflicts — updates directly in DB
            await db.Users
                .Where(u => u.Id == user.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.Email, user.Email)
                    .SetProperty(u => u.Name, user.Name),
                    ct);
        }
        else
        {
            await db.Users.AddAsync(user, ct);
            await db.SaveChangesAsync(ct);
        }
    }
}
