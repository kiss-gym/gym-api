namespace GymApi.Domain.SessionTracking;

public interface ISessionRepository
{
    Task<TrainingSession?> FindAsync(Guid sessionId, CancellationToken ct = default);
    // ReSharper disable once UnusedMemberInSuper.Global
    Task<TrainingSession?> FindLatestByUserAsync(Guid userId, CancellationToken ct = default);
    Task SaveAsync(TrainingSession session, CancellationToken ct = default);
}
