using GymApi.Domain.SessionTracking;
using Orleans;
using Orleans.Runtime;

namespace GymApi.Infrastructure.SessionTracking;

/// <summary>
/// This interface is internal to Infrastructure. 
/// It bridges the Domain IActiveSession with Orleans IGrain.
/// </summary>
public interface ITrainingSessionGrain : IActiveSession, IGrainWithGuidKey 
{ 
}

public sealed class TrainingSessionGrain(
    [PersistentState("session", "sessionStore")] IPersistentState<TrainingSession> sessionState)
    : Grain, ITrainingSessionGrain
{
    public Task<TrainingSession> GetStateAsync() => Task.FromResult(sessionState.State);

    public async Task<TrainingSession> InitializeAsync(Guid userId, TrainingSession? previousSession = null)
    {
        // Use the Grain's Guid as the TrainingSession Id to keep them in sync
        var sessionId = this.GetPrimaryKey();
        var session = TrainingSession.Create(userId, sessionId);
        
        if (previousSession != null) session.InheritFrom(previousSession);

        sessionState.State = session;
        await sessionState.WriteStateAsync();
        return sessionState.State;
    }

    public async Task<(ExerciseEntry Entry, TrainingSession State)> AddExerciseAsync(string autoLabel, string? photoUrl, DateTimeOffset? maxEndAt, IEnumerable<ExerciseProperty>? properties = null)
    {
        var entry = sessionState.State.AddExercise(autoLabel, photoUrl, maxEndAt, properties);
        await sessionState.WriteStateAsync();
        return (entry, sessionState.State);
    }

    public async Task<(ExerciseEntry Entry, TrainingSession State)> StartExerciseAsync(Guid exerciseId, DateTimeOffset? maxEndAt = null)
    {
        var entry = sessionState.State.StartExercise(exerciseId, maxEndAt);
        await sessionState.WriteStateAsync();
        return (entry, sessionState.State);
    }

    public async Task<TrainingSession> FinishExerciseAsync(Guid exerciseId)
    {
        sessionState.State.FinishExercise(exerciseId);
        await sessionState.WriteStateAsync();
        return sessionState.State;
    }

    public async Task<TrainingSession> RemoveExerciseAsync(Guid exerciseId)
    {
        sessionState.State.RemoveExercise(exerciseId);
        await sessionState.WriteStateAsync();
        return sessionState.State;
    }

    public async Task<TrainingSession> FinishAsync()
    {
        sessionState.State.Finish();
        await sessionState.WriteStateAsync();
        return sessionState.State;
    }
}

/// <summary>
/// Implementation of the provider that Application uses.
/// </summary>
public sealed class OrleansActiveSessionProvider(IGrainFactory grainFactory) : IActiveSessionProvider
{
    public IActiveSession GetSession(Guid sessionId) => grainFactory.GetGrain<ITrainingSessionGrain>(sessionId);
}
