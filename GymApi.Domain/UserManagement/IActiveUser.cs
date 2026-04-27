namespace GymApi.Domain.UserManagement;

/// <summary>
/// Domain-level abstraction for an active user's state.
/// Implementation will likely be an Orleans UserGrain.
/// </summary>
public interface IActiveUser
{
    Task SetLatestSessionAsync(Guid sessionId);
    Task<Guid?> GetLatestSessionIdAsync();
}
