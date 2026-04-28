namespace GymApi.Domain.SessionTracking;

public interface ITrainingSessionLifecycleProvider
{
    ITrainingSessionLifecycle GetTrainingSessionLifecycle(Guid sessionId);
}
