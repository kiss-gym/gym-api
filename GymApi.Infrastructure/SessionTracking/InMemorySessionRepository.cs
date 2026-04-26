using System.Collections.Concurrent;
using GymApi.Domain.SessionTracking;

namespace GymApi.Infrastructure.SessionTracking;

public sealed class InMemorySessionRepository : ISessionRepository
{
    private readonly ConcurrentDictionary<Guid, TrainingSession> _sessions = new();

    public Task<TrainingSession?> FindAsync(Guid sessionId, CancellationToken ct = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<TrainingSession?> FindLatestByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var latest = _sessions.Values
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault();
            
        return Task.FromResult(latest);
    }

    public Task SaveAsync(TrainingSession session, CancellationToken ct = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }
}
