using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using Orleans;

namespace GymApi.Application.SessionTracking;

public sealed class CurrentSessionService(
    IGrainFactory grainFactory, 
    IUserContext userContext)
    : ICurrentSessionService
{
    public async Task<TrainingSession> CreateSessionAsync(
        Guid userId,
        Guid? inheritFromSessionId = null,
        CancellationToken ct = default)
    {
        var sessionId = Guid.NewGuid();
        var sessionGrain = grainFactory.GetGrain<ITrainingSessionGrain>(sessionId);

        TrainingSession? previous = null;
        if (inheritFromSessionId.HasValue)
        {
            var previousGrain = grainFactory.GetGrain<ITrainingSessionGrain>(inheritFromSessionId.Value);
            previous = await previousGrain.GetStateAsync();
        }

        var state = await sessionGrain.InitializeAsync(userId, previous);
        
        // Update User index
        var userGrain = grainFactory.GetGrain<IUserGrain>(userId);
        await userGrain.SetLatestSessionAsync(sessionId);
        
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
        var (entry, _) = await grain.AddExerciseAsync(autoLabel, photoUrl, maxEndAt, properties);
        return entry;
    }

    public async Task<ExerciseEntry> StartExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        DateTimeOffset? maxEndAt = null,
        CancellationToken ct = default)
    {
        var grain = grainFactory.GetGrain<ITrainingSessionGrain>(sessionId);
        var (entry, _) = await grain.StartExerciseAsync(exerciseId, maxEndAt);
        return entry;
    }

    public async Task<ExerciseEntry> FinishExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var grain = grainFactory.GetGrain<ITrainingSessionGrain>(sessionId);
        var state = await grain.FinishExerciseAsync(exerciseId);
        return state.Exercises.First(e => e.Id == exerciseId);
    }

    public async Task RemoveExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var grain = grainFactory.GetGrain<ITrainingSessionGrain>(sessionId);
        await grain.RemoveExerciseAsync(exerciseId);
    }

    public async Task<TrainingSession> FinishSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var grain = grainFactory.GetGrain<ITrainingSessionGrain>(sessionId);
        return await grain.FinishAsync();
    }
}
