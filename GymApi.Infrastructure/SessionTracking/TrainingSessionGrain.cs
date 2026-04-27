using GymApi.Domain.SessionTracking;
using Orleans;
using Orleans.Runtime;

namespace GymApi.Infrastructure.SessionTracking;

/// <summary>
/// Infrastructure implementation of the TrainingSession Grain.
/// Handles persistence and mirrors the Domain logic by delegating to the TrainingSession aggregate.
/// </summary>
public sealed class TrainingSessionGrain(
    [PersistentState("session", "sessionStore")] IPersistentState<TrainingSession> sessionState)
    : Grain, ITrainingSessionGrain
{
    [Alias("GetState")]
    public Task<TrainingSession> GetStateAsync() => Task.FromResult(sessionState.State);

    [Alias("Initialize")]
    public async Task<TrainingSession> InitializeAsync(Guid userId, TrainingSession? previousSession = null)
    {
        var session = TrainingSession.Create(userId);

        if (previousSession != null)
        {
            session.InheritFrom(previousSession);
        }

        sessionState.State = session;
        await sessionState.WriteStateAsync();
        return sessionState.State;
    }

    [Alias("AddExercise")]
    public async Task<(ExerciseEntry Entry, TrainingSession State)> AddExerciseAsync(
        string autoLabel,
        string? photoUrl,
        DateTimeOffset? maxEndAt,
        IEnumerable<ExerciseProperty>? properties = null)
    {
        var entry = sessionState.State.AddExercise(autoLabel, photoUrl, maxEndAt, properties);
        await sessionState.WriteStateAsync();
        return (entry, sessionState.State);
    }

    [Alias("StartExercise")]
    public async Task<(ExerciseEntry Entry, TrainingSession State)> StartExerciseAsync(Guid exerciseId, DateTimeOffset? maxEndAt = null)
    {
        var entry = sessionState.State.StartExercise(exerciseId, maxEndAt);
        await sessionState.WriteStateAsync();
        return (entry, sessionState.State);
    }

    [Alias("FinishExercise")]
    public async Task<TrainingSession> FinishExerciseAsync(Guid exerciseId)
    {
        sessionState.State.FinishExercise(exerciseId);
        await sessionState.WriteStateAsync();
        return sessionState.State;
    }

    [Alias("RemoveExercise")]
    public async Task<TrainingSession> RemoveExerciseAsync(Guid exerciseId)
    {
        sessionState.State.RemoveExercise(exerciseId);
        await sessionState.WriteStateAsync();
        return sessionState.State;
    }

    [Alias("Finish")]
    public async Task<TrainingSession> FinishAsync()
    {
        sessionState.State.Finish();
        await sessionState.WriteStateAsync();
        return sessionState.State;
    }
}
