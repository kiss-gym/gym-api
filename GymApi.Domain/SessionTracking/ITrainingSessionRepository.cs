namespace GymApi.Domain.SessionTracking;

public interface ITrainingSessionRepository
{
    // ReSharper disable UnusedParameter.Global
    Task SaveAsync(TrainingSession session, CancellationToken ct = default);
    Task<TrainingSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TrainingSession>> GetAllAsync(CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    // ReSharper restore UnusedParameter.Global
}
