using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;

namespace GymApi.Application.SessionTracking;

public sealed class TrainingSessionLifecycleService(
    ITrainingSessionLifecycleProvider sessionLifecycleProvider,
    IActiveUserProvider userProvider,
    IUserContext userContext)
    : ITrainingSessionLifecycleService
{
    public async Task<TrainingSession> CreateSessionAsync(
        Guid userId,
        Guid? inheritFromSessionId = null,
        CancellationToken ct = default)
    {
        var sessionId = Guid.NewGuid();
        var newSession = sessionLifecycleProvider.GetTrainingSessionLifecycle(sessionId);

        TrainingSession? parentSession = null;
        if (inheritFromSessionId.HasValue)
        {
            var parentSessionLifecycle = sessionLifecycleProvider.GetTrainingSessionLifecycle(inheritFromSessionId.Value);
            parentSession = await parentSessionLifecycle.GetStateAsync();
        }

        var state = await newSession.InitializeAsync(userId, parentSession);
        
        var activeUser = userProvider.GetUser(userId);
        await activeUser.SetLatestSessionAsync(sessionId);
        
        return state;
    }

    public async Task<TrainingSession> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var sessionLifecycle = sessionLifecycleProvider.GetTrainingSessionLifecycle(sessionId);
        var session = await sessionLifecycle.GetStateAsync();

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
        var sessionLifecycle = sessionLifecycleProvider.GetTrainingSessionLifecycle(sessionId);
        var (entry, _) = await sessionLifecycle.AddExerciseAsync(autoLabel, photoUrl, maxEndAt, properties);
        return entry;
    }

    public async Task<ExerciseEntry> StartExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        DateTimeOffset? maxEndAt = null,
        CancellationToken ct = default)
    {
        var sessionLifecycle = sessionLifecycleProvider.GetTrainingSessionLifecycle(sessionId);
        var (entry, _) = await sessionLifecycle.StartExerciseAsync(exerciseId, maxEndAt);
        return entry;
    }

    public async Task<ExerciseEntry> FinishExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var sessionLifecycle = sessionLifecycleProvider.GetTrainingSessionLifecycle(sessionId);
        var state = await sessionLifecycle.FinishExerciseAsync(exerciseId);
        return state.Exercises.First(e => e.Id == exerciseId);
    }

    public async Task RemoveExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var sessionLifecycle = sessionLifecycleProvider.GetTrainingSessionLifecycle(sessionId);
        await sessionLifecycle.RemoveExerciseAsync(exerciseId);
    }

    public async Task<TrainingSession> FinishSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var sessionLifecycle = sessionLifecycleProvider.GetTrainingSessionLifecycle(sessionId);
        return await sessionLifecycle.FinishAsync();
    }

    public async Task DeleteSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var sessionLifecycle = sessionLifecycleProvider.GetTrainingSessionLifecycle(sessionId);
        var session = await sessionLifecycle.GetStateAsync(); // Get state for auth check

        if (userContext.IsAuthenticated && session.UserId != userContext.UserId)
        {
            throw new UnauthorizedAccessException("You do not have access to delete this session.");
        }

        await sessionLifecycle.DeleteAsync();
    }
}
