using GymApi.Domain.SessionTracking;
using Microsoft.EntityFrameworkCore;

namespace GymApi.Infrastructure.SessionTracking;

/// <summary>
/// Supabase/Postgres implementation of ITrainingSessionRepository using EF Core.
/// All filtering, sorting and pagination are pushed to SQL — no in-memory filtering.
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

    public async Task<IReadOnlyList<TrainingSession>> GetSessionsAsync(
        Guid userId,
        SessionStatus? status = null,
        string? sort = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = db.TrainingSessions
            .AsNoTracking()
            .Include(s => s.Exercises)
            .Where(s => s.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        query = ApplySort(query, sort);

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountSessionsAsync(
        Guid userId,
        SessionStatus? status = null,
        CancellationToken ct = default)
    {
        var query = db.TrainingSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        return await query.CountAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await db.TrainingSessions
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IQueryable<TrainingSession> ApplySort(
        IQueryable<TrainingSession> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return query.OrderByDescending(s => s.CreatedAt); // sensible default
        }

        var parts = sort.Split(':');
        var property = parts[0].ToLowerInvariant();
        var descending = parts.Length > 1 &&
                         parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

        return property switch
        {
            "finishedat" => descending
                ? query.OrderByDescending(s => s.FinishedAt)
                : query.OrderBy(s => s.FinishedAt),
            "createdat" => descending
                ? query.OrderByDescending(s => s.CreatedAt)
                : query.OrderBy(s => s.CreatedAt),
            _ => query.OrderByDescending(s => s.CreatedAt)
        };
    }
}
