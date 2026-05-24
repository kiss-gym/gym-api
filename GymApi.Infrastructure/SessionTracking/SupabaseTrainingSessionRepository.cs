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

        if (!exists)
        {
            await db.TrainingSessions.AddAsync(session, ct);
            await db.SaveChangesAsync(ct);
            return;
        }

        await ReplaceExistingSessionGraphAsync(session, ct);
    }

    public async Task<TrainingSession?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.TrainingSessions
            .AsNoTracking()
            .Include(s => s.Exercises)
                .ThenInclude(e => e.SortedSets)
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
                .ThenInclude(e => e.SortedSets)
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

    private async Task ReplaceExistingSessionGraphAsync(
        TrainingSession session,
        CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction ? await db.Database.BeginTransactionAsync(ct) : null;

        // Update the aggregate root row directly; children are handled below.
        await db.TrainingSessions
            .Where(s => s.Id == session.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.UserId, session.UserId)
                .SetProperty(s => s.Label, session.Label)
                .SetProperty(s => s.CreatedAt, session.CreatedAt)
                .SetProperty(s => s.FinishedAt, session.FinishedAt)
                .SetProperty(s => s.Status, session.Status)
                .SetProperty(s => s.InheritedFromSessionId, session.InheritedFromSessionId), ct);

        // Replace child rows from the current in-memory snapshot.
        await db.ExerciseEntries
            .Where(e => EF.Property<Guid>(e, "session_id") == session.Id)
            .ExecuteDeleteAsync(ct);

        foreach (var exercise in session.Exercises)
        {
            db.Entry(exercise).State = EntityState.Added;
            db.Entry(exercise).Property("session_id").CurrentValue = session.Id;

            foreach (var set in exercise.SortedSets)
            {
                db.Entry(set).State = EntityState.Added;
                db.Entry(set).Property("exercise_id").CurrentValue = exercise.Id;
            }
        }

        await db.SaveChangesAsync(ct);
        if (tx is not null)
        {
            await tx.CommitAsync(ct);
        }
    }

    private static IQueryable<TrainingSession> ApplySort(
        IQueryable<TrainingSession> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return query.OrderByDescending(s => s.CreatedAt);
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
