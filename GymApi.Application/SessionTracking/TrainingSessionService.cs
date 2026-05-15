using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;

namespace GymApi.Application.SessionTracking;

public sealed class TrainingSessionService(
    IUserContext userContext,
    ITrainingSessionRepository repository)
    : ITrainingSessionService
{
    public async Task<TrainingSession> CreateSessionAsync(
        Guid? inheritFromSessionId = null,
        string? label = null,
        CancellationToken ct = default)
    {
        EnsureAuthenticated();

        var session = TrainingSession.Create(userContext.UserId!.Value);
        if (label != null)
        {
            session.Rename(label);
        }

        if (inheritFromSessionId.HasValue)
        {
            var parent = await repository.GetByIdAsync(inheritFromSessionId.Value, ct);
            if (parent != null)
            {
                session.InheritFrom(parent);
            }
        }

        await repository.SaveAsync(session, ct);
        return session;
    }

    public async Task<TrainingSession> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        EnsureAuthenticated();

        var session = await repository.GetByIdAsync(sessionId, ct)
                      ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        return session.UserId != userContext.UserId
            ? throw new UnauthorizedAccessException("You do not have access to this session.")
            : session;
    }

    public async Task<ExerciseEntry> AddExerciseAsync(
        Guid sessionId,
        string autoLabel,
        string? photoUrl,
        IEnumerable<ExerciseProperty>? properties = null,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        var exercise = session.AddExercise(autoLabel, photoUrl, properties);
        await repository.SaveAsync(session, ct);
        return exercise;
    }

    public async Task<ExerciseEntry> StartExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        var exercise = session.StartExercise(exerciseId);
        await repository.SaveAsync(session, ct);
        return exercise;
    }

    public async Task<ExerciseEntry> FinishExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.FinishExercise(exerciseId);
        var exercise = session.Exercises.First(e => e.Id == exerciseId);
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

    public async Task<TrainingSession> RenameSessionAsync(
        Guid sessionId,
        string label,
        CancellationToken ct = default)
    {
        
        var session = await GetSessionAsync(sessionId, ct);
        session.Rename(label);
        await repository.SaveAsync(session, ct);
        return session;
    }

    public async Task<TrainingSession> FinishSessionAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.Finish();
        await repository.SaveAsync(session, ct);
        return session;
    }

    public async Task<IReadOnlyList<TrainingSession>> GetSessionsAsync(SessionStatus? status = null,
        string? sort = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default)
    {
        EnsureAuthenticated();

        return await repository.GetSessionsAsync(
            userId: userContext.UserId!.Value,
            status: status,
            sort: sort,
            page: page ?? 1,
            pageSize: pageSize ?? 10,
            ct: ct);
    }

    public async Task<int> GetSessionsCountAsync(
        SessionStatus? status = null,
        CancellationToken ct = default)
    {
        EnsureAuthenticated();

        return await repository.CountSessionsAsync(
            userId: userContext.UserId!.Value,
            status: status,
            ct: ct);
    }

    public async Task DeleteSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        await repository.DeleteAsync(sessionId, ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void EnsureAuthenticated()
    {
        if (!userContext.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("No user authenticated.");
        }
    }
}
