using GymApi.Domain.SessionTracking;

namespace GymApi.Domain.SessionTracking;

public interface ITrainingSessionRepository
{
    Task SaveAsync(TrainingSession session, CancellationToken ct = default);
    Task<TrainingSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TrainingSession>> GetAllAsync(CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
