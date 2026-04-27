using Orleans;

namespace GymApi.Domain.SessionTracking;

/// <summary>
/// Defines the actor-based contract for a training session.
/// The Grain acts as the host and guardian of the TrainingSession aggregate.
/// </summary>
[Alias("GymApi.Domain.SessionTracking.ITrainingSessionGrain")]
public interface ITrainingSessionGrain : IGrainWithGuidKey
{
    /// <summary>Returns the current state of the session.</summary>
    [Alias("GetState")]
    Task<TrainingSession> GetStateAsync();

    /// <summary>
    /// Initializes a new session. 
    /// If previousSession is provided, exercises are inherited as pending.
    /// </summary>
    [Alias("Initialize")]
    Task<TrainingSession> InitializeAsync(Guid userId, TrainingSession? previousSession = null);

    /// <summary>Adds and starts a new exercise, auto-finishing any currently running one.</summary>
    [Alias("AddExercise")]
    Task<(ExerciseEntry Entry, TrainingSession State)> AddExerciseAsync(
        string autoLabel,
        string? photoUrl,
        DateTimeOffset? maxEndAt,
        IEnumerable<ExerciseProperty>? properties = null);

    /// <summary>Starts a pending exercise.</summary>
    [Alias("StartExercise")]
    Task<(ExerciseEntry Entry, TrainingSession State)> StartExerciseAsync(Guid exerciseId, DateTimeOffset? maxEndAt = null);

    /// <summary>Finishes a specific running exercise.</summary>
    [Alias("FinishExercise")]
    Task<TrainingSession> FinishExerciseAsync(Guid exerciseId);

    /// <summary>Removes an exercise from the session.</summary>
    [Alias("RemoveExercise")]
    Task<TrainingSession> RemoveExerciseAsync(Guid exerciseId);

    /// <summary>Finishes the entire session.</summary>
    [Alias("Finish")]
    Task<TrainingSession> FinishAsync();
}
