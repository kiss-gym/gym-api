using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;

namespace GymApi.Application.SessionTracking;

public sealed class TrainingSessionLifecycleService(
    ITrainingSessionLifecycleProvider sessionLifecycleProvider,
    IActiveUserProvider userProvider,
    IUserContext userContext,
    ITrainingSessionRepository repository)
    : ITrainingSessionLifecycleService
{
    public async Task<TrainingSession> CreateSessionAsync(
        Guid userId,
        Guid? inheritFromSessionId = null,
        CancellationToken ct = default)
    {
        var activeUser = userProvider.GetUser(userId);
        var latestSessionId = await activeUser.GetLatestSessionIdAsync();

        if (latestSessionId.HasValue)
        {
            var latestSessionLifecycle = sessionLifecycleProvider.GetTrainingSessionLifecycle(latestSessionId.Value);
            var latestSessionState = await latestSessionLifecycle.GetStateAsync();

            if (latestSessionState.Status == SessionStatus.Active)
            {
                throw new InvalidOperationException("Cannot create a new session while an existing session is still active. Please finish the current session first.");
            }
        }

        var sessionId = Guid.NewGuid();
        var newSession = sessionLifecycleProvider.GetTrainingSessionLifecycle(sessionId);

        TrainingSession? parentSession = null;
        if (inheritFromSessionId.HasValue)
        {
            var parentSessionLifecycle = sessionLifecycleProvider.GetTrainingSessionLifecycle(inheritFromSessionId.Value);
            parentSession = await parentSessionLifecycle.GetStateAsync();
        }

        var state = await newSession.InitializeAsync(userId, parentSession);
        
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

    public async Task<IReadOnlyList<TrainingSession>> GetSessionsAsync(
        Guid? userId = null,
        SessionStatus? status = null,
        string? sort = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default)
    {
        var sessions = await repository.GetAllAsync(ct);
        var query = sessions.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(s => s.UserId == userId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(sort))
        {
            var parts = sort.Split(':');
            var propertyName = parts[0];
            var descending = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

            query = propertyName.ToLowerInvariant() switch
            {
                "finishedat" => descending ? query.OrderByDescending(s => s.FinishedAt) : query.OrderBy(s => s.FinishedAt),
                "createdat" => descending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt),
                _ => query
            };
        }

        if (page.HasValue && pageSize.HasValue)
        {
            query = query.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
        }

        return query.ToList().AsReadOnly();
    }

    public async Task<int> GetSessionsCountAsync(
        Guid? userId = null,
        SessionStatus? status = null,
        CancellationToken ct = default)
    {
        var sessions = await repository.GetAllAsync(ct);
        var query = sessions.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(s => s.UserId == userId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        return query.Count();
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
