namespace GymApi.Domain.SessionTracking;

/// <summary>
/// Domain-level abstraction for a training session service.
/// </summary>
public interface ICurrentSessionService
{
    Task<TrainingSession> CreateSessionAsync(
        Guid userId,
        Guid? inheritFromSessionId = null,
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

    Task<TrainingSession> FinishSessionAsync(
        Guid sessionId,
        CancellationToken ct = default);
}
