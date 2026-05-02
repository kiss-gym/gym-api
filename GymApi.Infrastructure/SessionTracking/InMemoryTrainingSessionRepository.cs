using System.Collections.Concurrent;
using GymApi.Domain.SessionTracking;

namespace GymApi.Infrastructure.SessionTracking;

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

    public Task<IReadOnlyList<TrainingSession>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<TrainingSession>>(_sessions.Values.ToList().AsReadOnly());
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _sessions.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
