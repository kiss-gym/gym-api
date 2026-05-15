namespace GymApi.Domain.SessionTracking;

public interface ITrainingSessionRepository
{
    // ReSharper disable UnusedParameter.Global
    Task SaveAsync(TrainingSession session, CancellationToken ct = default);
    Task<TrainingSession?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<TrainingSession>> GetSessionsAsync(
        Guid userId,
        SessionStatus? status = null,
        string? sort = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    Task<int> CountSessionsAsync(
        Guid userId,
        SessionStatus? status = null,
        CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
    // ReSharper restore UnusedParameter.Global
}
