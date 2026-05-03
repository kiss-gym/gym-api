namespace GymApi.Domain.SessionTracking;

/// <summary>
/// Domain-level abstraction for a training session service.
/// </summary>
public interface ITrainingSessionLifecycleService
{
    Task<TrainingSession> CreateSessionAsync(
        Guid userId,
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
        DateTimeOffset? maxEndAt,
        IEnumerable<ExerciseProperty>? properties = null,
        CancellationToken ct = default);

    Task<ExerciseEntry> StartExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        DateTimeOffset? maxEndAt = null,
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
        Guid? userId = null,
        SessionStatus? status = null,
        string? sort = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default);

    Task<int> GetSessionsCountAsync(
        Guid? userId = null,
        SessionStatus? status = null,
        CancellationToken ct = default);

    Task DeleteSessionAsync(
        Guid sessionId,
        CancellationToken ct = default);
}
