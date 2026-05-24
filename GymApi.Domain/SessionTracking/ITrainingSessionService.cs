namespace GymApi.Domain.SessionTracking;

/// <summary>
/// Domain-level abstraction for a training session service.
/// </summary>
public interface ITrainingSessionService
{
    // ReSharper disable UnusedParameter.Global
    Task<TrainingSession> CreateSessionAsync(
        Guid? inheritFromSessionId = null,
        string? label = null,
        CancellationToken ct = default);

    Task<TrainingSession> GetSessionAsync(
        Guid sessionId,
        CancellationToken ct = default);

    Task<ExerciseEntry> AddExerciseAsync(
        Guid sessionId,
        string autoLabel,
        string? photoUrl,
        IEnumerable<ExerciseProperty>? properties = null,
        CancellationToken ct = default);

    Task<ExerciseEntry> UpdateExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        string? autoLabel,
        string? photoUrl,
        IEnumerable<ExerciseProperty>? properties = null,
        CancellationToken ct = default);

    Task<ExerciseEntry> StartExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default);

    Task<ExerciseEntry> FinishExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default);

    Task RemoveExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default);

    Task<TrainingSession> RenameSessionAsync(
        Guid sessionId,
        string label,
        CancellationToken ct = default);

    Task<TrainingSession> FinishSessionAsync(
        Guid sessionId,
        CancellationToken ct = default);

    Task<IReadOnlyList<TrainingSession>> GetSessionsAsync(
        SessionStatus? status = null,
        string? sort = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default);

    Task<int> GetSessionsCountAsync(
        SessionStatus? status = null,
        CancellationToken ct = default);

    Task DeleteSessionAsync(
        Guid sessionId,
        CancellationToken ct = default);

    // ── Set operations ───────────────────────────────────────────────────────

    Task<ExerciseSet> AddSetAsync(
        Guid sessionId,
        Guid exerciseId,
        decimal? weight,
        int? repetitions,
        CancellationToken ct = default);

    Task<ExerciseSet> AddCopyOfLastSetAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default);

    Task<ExerciseSet> UpdateSetAsync(
        Guid sessionId,
        Guid exerciseId,
        Guid setId,
        decimal? weight,
        int? repetitions,
        CancellationToken ct = default);

    Task DeleteSetAsync(
        Guid sessionId,
        Guid exerciseId,
        Guid setId,
        CancellationToken ct = default);

    Task<ExerciseSet> CompleteSetAsync(
        Guid sessionId,
        Guid exerciseId,
        Guid setId,
        CancellationToken ct = default);

    Task<ExerciseSet> UnCompleteSetAsync(
        Guid sessionId,
        Guid exerciseId,
        Guid setId,
        CancellationToken ct = default);
    // ReSharper restore UnusedParameter.Global
}
