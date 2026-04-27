namespace GymApi.Domain.SessionTracking;

/// <summary>
/// Domain-level abstraction for an active training session.
/// Implementation will likely be an Orleans Grain, but the Domain/Application layers don't need to know that.
/// </summary>
public interface IActiveSession
{
    Task<TrainingSession> GetStateAsync();
    Task<TrainingSession> InitializeAsync(Guid userId, TrainingSession? previousSession = null);
    Task<(ExerciseEntry Entry, TrainingSession State)> AddExerciseAsync(
        string autoLabel,
        string? photoUrl,
        DateTimeOffset? maxEndAt,
        IEnumerable<ExerciseProperty>? properties = null);
    Task<(ExerciseEntry Entry, TrainingSession State)> StartExerciseAsync(Guid exerciseId, DateTimeOffset? maxEndAt = null);
    Task<TrainingSession> FinishExerciseAsync(Guid exerciseId);
    Task<TrainingSession> RemoveExerciseAsync(Guid exerciseId);
    Task<TrainingSession> FinishAsync();
}
