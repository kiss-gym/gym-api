using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;

namespace GymApi.Application.SessionTracking;

public sealed class TrainingSessionService(
    IUserContext userContext,
    ITrainingSessionRepository repository)
    : ITrainingSessionService
{
    public async Task<TrainingSession> CreateSessionAsync(
        Guid userId,
        Guid? inheritFromSessionId = null,
        string? label = null,
        CancellationToken ct = default)
    {
        // For now, we allow passing userId, but in a real app we'd likely validate it against userContext
        var session = TrainingSession.Create(userId);
        if (label != null)
        {
            session.Rename(label);
        }

        if (inheritFromSessionId.HasValue)
        {
            var parent = await repository.GetByIdAsync(inheritFromSessionId.Value, ct);
            if (parent != null)
            {
                session.InheritFrom(parent);
            }
        }

        await repository.SaveAsync(session, ct);
        return session;
    }

    public async Task<TrainingSession> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await repository.GetByIdAsync(sessionId, ct)
                      ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        _ = userContext;
        // // Authorization check: User can only access their own sessions
        // if (userContext.IsAuthenticated && session.UserId != userContext.UserId)
        // {
        //     throw new UnauthorizedAccessException("You do not have access to this session.");
        // }

        return session;
    }

    public async Task<ExerciseEntry> AddExerciseAsync(
        Guid sessionId,
        string autoLabel,
        string? photoUrl,
        IEnumerable<ExerciseProperty>? properties = null,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        var exercise = session.AddExercise(autoLabel, photoUrl, properties);
        await repository.SaveAsync(session, ct);
        return exercise;
    }

    public async Task<ExerciseEntry> StartExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        var exercise = session.StartExercise(exerciseId);
        await repository.SaveAsync(session, ct);
        return exercise;
    }

    public async Task<ExerciseEntry> FinishExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.FinishExercise(exerciseId);
        var exercise = session.Exercises.First(e => e.Id == exerciseId);
        await repository.SaveAsync(session, ct);
        return exercise;
    }

    public async Task RemoveExerciseAsync(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.RemoveExercise(exerciseId);
        await repository.SaveAsync(session, ct);
    }

    public async Task<TrainingSession> RenameSessionAsync(
        Guid sessionId,
        string label,
        CancellationToken ct = default)
    {
        
        var session = await GetSessionAsync(sessionId, ct);
        session.Rename(label);
        await repository.SaveAsync(session, ct);
        return session;
    }

    public async Task<TrainingSession> FinishSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, ct);
        session.Finish();
        await repository.SaveAsync(session, ct);
        return session;
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
        await repository.DeleteAsync(sessionId, ct);
    }
}
