using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using Orleans;

namespace GymApi.Application.SessionTracking;

public sealed class CurrentSessionService(
    IGrainFactory grainFactory, 
    ISessionRepository repository,
    IUserContext userContext)
    : ICurrentSessionService
{
    public async Task<TrainingSession> CreateSessionAsync(
        Guid userId,
        Guid? inheritFromSessionId = null,
        CancellationToken ct = default)
    {
        var sessionId = Guid.NewGuid();
        var grain = grainFactory.GetGrain<ITrainingSessionGrain>(sessionId);

        TrainingSession? previous = null;
        if (inheritFromSessionId.HasValue)
        {
            previous = await repository.FindAsync(inheritFromSessionId.Value, ct);
        }

        var state = await grain.InitializeAsync(userId, previous);
        await repository.SaveAsync(state, ct); 
        
        return state;
    }

    public async Task<TrainingSession> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var grain = grainFactory.GetGrain<ITrainingSessionGrain>(sessionId);
        var session = await grain.GetStateAsync();

        if (userContext.IsAuthenticated && session.UserId != userContext.UserId)
        {
            throw new UnauthorizedAccessException("You do not have access to this session.");
        }

        return session;
    }

    public async Task<ExerciseEntry> AddExerciseAsync(
        Guid sessionId,
        string autoLabel,
        string? photoUrl,
        DateTimeOffset? maxEndAt,
        IEnumerable<ExerciseProperty>? properties = null,
        CancellationToken ct = default)
    {
        var grain = grainFactory.GetGrain<ITrainingSessionGrain>(sessionId);
        var (entry, state) = await grain.AddExerciseAsync(autoLabel, photoUrl, maxEndAt, properties);
        
        await repository.SaveAsync(state, ct);
        return entry;
    }

    public async Task<ExerciseEntry> StartExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        DateTimeOffset? maxEndAt = null,
        CancellationToken ct = default)
    {
        var grain = grainFactory.GetGrain<ITrainingSessionGrain>(sessionId);
        var (entry, state) = await grain.StartExerciseAsync(exerciseId, maxEndAt);
        
        await repository.SaveAsync(state, ct);
        return entry;
    }

    public async Task<ExerciseEntry> FinishExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var grain = grainFactory.GetGrain<ITrainingSessionGrain>(sessionId);
        var state = await grain.FinishExerciseAsync(exerciseId);
        
        await repository.SaveAsync(state, ct);
        return state.Exercises.First(e => e.Id == exerciseId);
    }

    public async Task RemoveExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var grain = grainFactory.GetGrain<ITrainingSessionGrain>(sessionId);
        var state = await grain.RemoveExerciseAsync(exerciseId);
        await repository.SaveAsync(state, ct);
    }

    public async Task<TrainingSession> FinishSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var grain = grainFactory.GetGrain<ITrainingSessionGrain>(sessionId);
        var state = await grain.FinishAsync();
        await repository.SaveAsync(state, ct);
        return state;
    }
}
