namespace GymApi.Domain.SessionTracking;

public interface IActiveSessionProvider
{
    IActiveSession GetSession(Guid sessionId);
}
