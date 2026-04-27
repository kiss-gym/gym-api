using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;

namespace GymApi.Application.SessionTracking;

public sealed class CurrentSessionService(
    IActiveSessionProvider sessionProvider,
    IActiveUserProvider userProvider,
    IUserContext userContext)
    : ICurrentSessionService
{
    public async Task<TrainingSession> CreateSessionAsync(
        Guid userId,
        Guid? inheritFromSessionId = null,
        CancellationToken ct = default)
    {
        var sessionId = Guid.NewGuid();
        var activeSession = sessionProvider.GetSession(sessionId);

        TrainingSession? previous = null;
        if (inheritFromSessionId.HasValue)
        {
            var previousActiveSession = sessionProvider.GetSession(inheritFromSessionId.Value);
            previous = await previousActiveSession.GetStateAsync();
        }

        var state = await activeSession.InitializeAsync(userId, previous);
        
        var activeUser = userProvider.GetUser(userId);
        await activeUser.SetLatestSessionAsync(sessionId);
        
        return state;
    }

    public async Task<TrainingSession> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var activeSession = sessionProvider.GetSession(sessionId);
        var session = await activeSession.GetStateAsync();

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
        var activeSession = sessionProvider.GetSession(sessionId);
        var (entry, _) = await activeSession.AddExerciseAsync(autoLabel, photoUrl, maxEndAt, properties);
        return entry;
    }

    public async Task<ExerciseEntry> StartExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        DateTimeOffset? maxEndAt = null,
        CancellationToken ct = default)
    {
        var activeSession = sessionProvider.GetSession(sessionId);
        var (entry, _) = await activeSession.StartExerciseAsync(exerciseId, maxEndAt);
        return entry;
    }

    public async Task<ExerciseEntry> FinishExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var activeSession = sessionProvider.GetSession(sessionId);
        var state = await activeSession.FinishExerciseAsync(exerciseId);
        return state.Exercises.First(e => e.Id == exerciseId);
    }

    public async Task RemoveExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var activeSession = sessionProvider.GetSession(sessionId);
        await activeSession.RemoveExerciseAsync(exerciseId);
    }

    public async Task<TrainingSession> FinishSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var activeSession = sessionProvider.GetSession(sessionId);
        return await activeSession.FinishAsync();
    }
}
