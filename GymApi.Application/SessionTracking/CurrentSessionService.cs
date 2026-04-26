using GymApi.Domain.SessionTracking;

namespace GymApi.Application.SessionTracking;

public sealed class CurrentSessionService(ISessionRepository repository) : ICurrentSessionService
{
    public async Task<TrainingSession> CreateAsync(
        Guid userId,
        Guid? inheritFromSessionId = null,
        CancellationToken ct = default)
    {
        var session = TrainingSession.Create(userId);

        if (inheritFromSessionId.HasValue)
        {
            var previous = await repository.FindAsync(inheritFromSessionId.Value, ct);
            if (previous != null)
            {
                session.InheritFrom(previous);
            }
        }

        await repository.SaveAsync(session, ct);
        return session;
    }

    public async Task<TrainingSession> GetAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await repository.FindAsync(sessionId, ct)
               ?? throw new KeyNotFoundException($"Session {sessionId} not found.");
    }

    public async Task<ExerciseEntry> AddExerciseAsync(
        Guid sessionId,
        string autoLabel,
        string? photoUrl,
        DateTimeOffset? maxEndAt,
        IEnumerable<ExerciseProperty>? properties = null,
        CancellationToken ct = default)
    {
        var session = await GetAsync(sessionId, ct);
        var exercise = session.AddExercise(autoLabel, photoUrl, maxEndAt, properties);
        await repository.SaveAsync(session, ct);
        return exercise;
    }

    public async Task<ExerciseEntry> StartExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        DateTimeOffset? maxEndAt = null,
        CancellationToken ct = default)
    {
        var session = await GetAsync(sessionId, ct);
        var exercise = session.StartExercise(exerciseId, maxEndAt);
        await repository.SaveAsync(session, ct);
        return exercise;
    }

    public async Task RemoveExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var session = await GetAsync(sessionId, ct);
        session.RemoveExercise(exerciseId);
        await repository.SaveAsync(session, ct);
    }

    public async Task<TrainingSession> FinishAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await GetAsync(sessionId, ct);
        session.Finish();
        await repository.SaveAsync(session, ct);
        return session;
    }
}
