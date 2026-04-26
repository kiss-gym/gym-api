using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;

namespace GymApi.Application.SessionTracking;

public sealed class CurrentSessionService(ISessionRepository repository, IUserContext userContext)
    : ICurrentSessionService
{
    public async Task<TrainingSession> CreateSessionAsync(
        Guid userId,
        Guid? inheritFromSessionId = null,
        CancellationToken ct = default)
    {
        // For now, we allow passing userId, but in a real app we'd likely validate it against userContext
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

    public async Task<TrainingSession> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await repository.FindAsync(sessionId, ct)
                      ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        // Authorization check: User can only access their own sessions
        if (userContext.IsAuthenticated && session.UserId != userContext.UserId)
        {
            throw new UnauthorizedAccessException("You do not have access to this session.");
        }

        return session;
    }

    public async Task<ExerciseEntry> AddExerciseAsync(
        Guid sessionId,
        string autoLabel,
        string? photoUrl,
        DateTimeOffset? maxEndAt,
        IEnumerable<ExerciseProperty>? properties = null,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
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
        var session = await GetSessionAsync(sessionId, ct);
        var exercise = session.StartExercise(exerciseId, maxEndAt);
        await repository.SaveAsync(session, ct);
        return exercise;
    }

    public async Task RemoveExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.RemoveExercise(exerciseId);
        await repository.SaveAsync(session, ct);
    }

    public async Task<TrainingSession> FinishSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.Finish();
        await repository.SaveAsync(session, ct);
        return session;
    }
}
