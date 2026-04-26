namespace GymApi.Domain.SessionTracking;

/// <summary>
/// Application port for the Current Session Tracking bounded context.
/// All mutation operations persist via ISessionRepository.
/// </summary>
public interface ICurrentSessionService
{
    Task<TrainingSession> CreateAsync(
        Guid userId,
        Guid? inheritFromSessionId = null,
        CancellationToken ct = default);

    Task<TrainingSession> GetAsync(
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

    Task RemoveExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default);

    Task<TrainingSession> FinishAsync(
        Guid sessionId,
        CancellationToken ct = default);
}
