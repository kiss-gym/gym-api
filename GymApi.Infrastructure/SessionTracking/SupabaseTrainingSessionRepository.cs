using GymApi.Domain.SessionTracking;
using GymApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GymApi.Infrastructure.SessionTracking;

/// <summary>
/// Supabase/Postgres implementation of ITrainingSessionRepository using EF Core.
/// Replaces InMemoryTrainingSessionRepository in production.
/// </summary>
public sealed class SupabaseTrainingSessionRepository(GymApiDbContext db) : ITrainingSessionRepository
{
    public async Task SaveAsync(TrainingSession session, CancellationToken ct = default)
    {
        var exists = await db.TrainingSessions
            .AsNoTracking()
            .AnyAsync(s => s.Id == session.Id, ct);

        if (exists)
        {
            // Attach and mark modified — exercises are owned via cascade
            db.TrainingSessions.Update(session);
        }
        else
        {
            await db.TrainingSessions.AddAsync(session, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<TrainingSession?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.TrainingSessions
            .AsNoTracking()
            .Include(s => s.Exercises)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<IReadOnlyList<TrainingSession>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.TrainingSessions
            .AsNoTracking()
            .Include(s => s.Exercises)
            .ToListAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await db.TrainingSessions
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(ct);
    }
}
