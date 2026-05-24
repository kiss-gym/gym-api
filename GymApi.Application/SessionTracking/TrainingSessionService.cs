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

    public async Task<TrainingSession> GetSessionAsync(
        Guid sessionId, CancellationToken ct = default)
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
        session.AddExercise(autoLabel, photoUrl, properties);
        await repository.SaveAsync(session, ct);
        return FindExercise(session, session.Exercises.Last().Id);
    }

    public async Task<ExerciseEntry> UpdateExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        string? autoLabel,
        string? photoUrl,
        IEnumerable<ExerciseProperty>? properties = null,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.UpdateExercise(exerciseId, autoLabel, photoUrl, properties);
        await repository.SaveAsync(session, ct);
        return FindExercise(session, exerciseId);
    }

    public async Task<ExerciseEntry> StartExerciseAsync(
        Guid sessionId, Guid exerciseId, CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.StartExercise(exerciseId);
        await repository.SaveAsync(session, ct);
        return FindExercise(session, exerciseId);
    }

    public async Task<ExerciseEntry> FinishExerciseAsync(
        Guid sessionId, Guid exerciseId, CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.FinishExercise(exerciseId);
        await repository.SaveAsync(session, ct);
        return FindExercise(session, exerciseId);
    }

    public async Task RemoveExerciseAsync(
        Guid sessionId, Guid exerciseId, CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.RemoveExercise(exerciseId);
        await repository.SaveAsync(session, ct);
    }

    public async Task<TrainingSession> RenameSessionAsync(
        Guid sessionId, string label, CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.Rename(label);
        await repository.SaveAsync(session, ct);
        return session;
    }

    public async Task<TrainingSession> FinishSessionAsync(
        Guid sessionId, CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.Finish();
        await repository.SaveAsync(session, ct);
        return session;
    }

    public async Task<IReadOnlyList<TrainingSession>> GetSessionsAsync(
        SessionStatus? status = null,
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

    // ── Set operations ────────────────────────────────────────────────────────

    public async Task<ExerciseSet> AddSetAsync(
        Guid sessionId,
        Guid exerciseId,
        decimal? weight,
        int? repetitions,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.AddSet(exerciseId, weight, repetitions);
        await repository.SaveAsync(session, ct);
        return FindExercise(session, exerciseId).SortedSets[^1];
    }

    public async Task<ExerciseSet> AddCopyOfLastSetAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.AddCopyOfLastSet(exerciseId);
        await repository.SaveAsync(session, ct);
        return FindExercise(session, exerciseId).SortedSets.Last();
    }

    public async Task<ExerciseSet> UpdateSetAsync(
        Guid sessionId,
        Guid exerciseId,
        Guid setId,
        decimal? weight,
        int? repetitions,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.UpdateSet(exerciseId, setId, weight, repetitions);
        await repository.SaveAsync(session, ct);
        return FindExercise(session, exerciseId).SortedSets.First(s => s.Id == setId);
    }

    public async Task DeleteSetAsync(
        Guid sessionId,
        Guid exerciseId,
        Guid setId,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.RemoveSet(exerciseId, setId);
        await repository.SaveAsync(session, ct);
    }

    public async Task<ExerciseSet> CompleteSetAsync(
        Guid sessionId,
        Guid exerciseId,
        Guid setId,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.CompleteSet(exerciseId, setId);
        await repository.SaveAsync(session, ct);
        return FindExercise(session, exerciseId).SortedSets.First(s => s.Id == setId);
    }

    public async Task<ExerciseSet> UnCompleteSetAsync(
        Guid sessionId,
        Guid exerciseId,
        Guid setId,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.UnCompleteSet(exerciseId, setId);
        await repository.SaveAsync(session, ct);
        return FindExercise(session, exerciseId).SortedSets.First(s => s.Id == setId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ExerciseEntry FindExercise(TrainingSession session, Guid exerciseId)
    {
        return session.Exercises.FirstOrDefault(e => e.Id == exerciseId)
               ?? throw new KeyNotFoundException(
                   $"Exercise {exerciseId} not found in session {session.Id}.");
    }

    private void EnsureAuthenticated()
    {
        if (!userContext.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("No user authenticated.");
        }
    }
}
