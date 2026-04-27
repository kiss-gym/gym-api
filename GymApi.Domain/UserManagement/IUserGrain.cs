using Orleans;

namespace GymApi.Domain.UserManagement;

/// <summary>
/// Tracks user-specific state and indexes, such as the latest training session.
/// </summary>
[Alias("GymApi.Domain.UserManagement.IUserGrain")]
public interface IUserGrain : IGrainWithGuidKey
{
    [Alias("SetLatestSession")]
    Task SetLatestSessionAsync(Guid sessionId);

    [Alias("GetLatestSession")]
    Task<Guid?> GetLatestSessionIdAsync();
}
