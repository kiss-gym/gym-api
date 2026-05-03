using GymApi.Domain.SessionTracking;

namespace GymApi.Infrastructure.SessionTracking;

/// <summary>
/// This interface bridges the Domain IActiveSession with Orleans IGrain.
/// </summary>
public interface ITrainingSessionLifecycleGrain : ITrainingSessionLifecycle, IGrainWithGuidKey;

public sealed class TrainingSessionLifecycleGrain(
    [PersistentState("session", "sessionStore")] IPersistentState<TrainingSession> sessionPersistentState,
    ITrainingSessionRepository repository)
    : Grain, ITrainingSessionLifecycleGrain
{
    public Task<TrainingSession> GetStateAsync() => Task.FromResult(sessionPersistentState.State);

    public async Task<TrainingSession> InitializeAsync(Guid userId, TrainingSession? parentSession = null, string? label = null)
    {
        // Use the Grain's Guid as the TrainingSession id to keep them in sync
        var sessionId = this.GetPrimaryKey();
        var session = TrainingSession.Create(userId, sessionId, label);
        
        if (parentSession != null)
        {
            session.InheritFrom(parentSession);
        }

        sessionPersistentState.State = session;
        await sessionPersistentState.WriteStateAsync();
        await repository.SaveAsync(sessionPersistentState.State);
        return sessionPersistentState.State;
    }

    public async Task<(ExerciseEntry Entry, TrainingSession State)> AddExerciseAsync(string autoLabel, string? photoUrl, DateTimeOffset? maxEndAt, IEnumerable<ExerciseProperty>? properties = null)
    {
        var entry = sessionPersistentState.State.AddExercise(autoLabel, photoUrl, maxEndAt, properties);
        await sessionPersistentState.WriteStateAsync();
        await repository.SaveAsync(sessionPersistentState.State);
        return (entry, sessionPersistentState.State);
    }

    public async Task<(ExerciseEntry Entry, TrainingSession State)> StartExerciseAsync(Guid exerciseId, DateTimeOffset? maxEndAt = null)
    {
        var entry = sessionPersistentState.State.StartExercise(exerciseId, maxEndAt);
        await sessionPersistentState.WriteStateAsync();
        await repository.SaveAsync(sessionPersistentState.State);
        return (entry, sessionPersistentState.State);
    }

    public async Task<TrainingSession> FinishExerciseAsync(Guid exerciseId)
    {
        sessionPersistentState.State.FinishExercise(exerciseId);
        await sessionPersistentState.WriteStateAsync();
        await repository.SaveAsync(sessionPersistentState.State);
        return sessionPersistentState.State;
    }

    public async Task<TrainingSession> RemoveExerciseAsync(Guid exerciseId)
    {
        sessionPersistentState.State.RemoveExercise(exerciseId);
        await sessionPersistentState.WriteStateAsync();
        await repository.SaveAsync(sessionPersistentState.State);
        return sessionPersistentState.State;
    }

    public async Task<TrainingSession> RenameAsync(string label)
    {
        sessionPersistentState.State.Rename(label);
        await sessionPersistentState.WriteStateAsync();
        await repository.SaveAsync(sessionPersistentState.State);
        return sessionPersistentState.State;
    }

    public async Task<TrainingSession> FinishAsync()
    {
        sessionPersistentState.State.Finish();
        await sessionPersistentState.WriteStateAsync();
        await repository.SaveAsync(sessionPersistentState.State);
        return sessionPersistentState.State;
    }

    public async Task DeleteAsync()
    {
        var sessionId = this.GetPrimaryKey();
        await sessionPersistentState.ClearStateAsync();
        await repository.DeleteAsync(sessionId);
    }
}

/// <summary>
/// Implementation of the provider that Application uses.
/// </summary>
public sealed class OrleansTrainingSessionLifecycleProvider(IGrainFactory grainFactory) : ITrainingSessionLifecycleProvider
{
    public ITrainingSessionLifecycle GetTrainingSessionLifecycle(Guid sessionId) => grainFactory.GetGrain<ITrainingSessionLifecycleGrain>(sessionId);
}
