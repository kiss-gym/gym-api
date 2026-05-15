using System.Collections.Concurrent;
using GymApi.Domain.SessionTracking;

namespace GymApi.Tests.Fakes;

public sealed class InMemoryTrainingSessionRepository : ITrainingSessionRepository
{
    private readonly ConcurrentDictionary<Guid, TrainingSession> _sessions = new();

    public Task SaveAsync(TrainingSession session, CancellationToken ct = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task<TrainingSession?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _sessions.TryGetValue(id, out var session);
        return Task.FromResult(session);
    }

    public Task<IReadOnlyList<TrainingSession>> GetSessionsAsync(
        Guid userId,
        SessionStatus? status = null,
        string? sort = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = _sessions.Values
            .Where(s => s.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        // Simple sort for test purposes
        query = sort?.ToLowerInvariant() switch
        {
            "finishedat:desc" => query.OrderByDescending(s => s.FinishedAt),
            "finishedat"      => query.OrderBy(s => s.FinishedAt),
            "createdat:desc"  => query.OrderByDescending(s => s.CreatedAt),
            "createdat"       => query.OrderBy(s => s.CreatedAt),
            _                 => query.OrderByDescending(s => s.CreatedAt)
        };

        var result = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<TrainingSession>>(result);
    }

    public Task<int> CountSessionsAsync(
        Guid userId,
        SessionStatus? status = null,
        CancellationToken ct = default)
    {
        var query = _sessions.Values.Where(s => s.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        return Task.FromResult(query.Count());
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _sessions.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
