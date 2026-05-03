using GymApi.Domain.UserManagement;

namespace GymApi.Infrastructure.UserManagement;

/// <summary>
/// This interface bridges the Domain IActiveUser with Orleans IGrain.
/// </summary>
public interface IUserGrain : IActiveUser, IGrainWithGuidKey;

public sealed class UserGrain(
    [PersistentState("user", "sessionStore")] IPersistentState<UserState> userState)
    : Grain, IUserGrain
{
    public async Task SetLatestSessionAsync(Guid sessionId)
    {
        userState.State.LatestSessionId = sessionId;
        await userState.WriteStateAsync();
    }

    public Task<Guid?> GetLatestSessionIdAsync() => Task.FromResult(userState.State.LatestSessionId);
}

[GenerateSerializer]
public record UserState
{
    [Id(0)] public Guid? LatestSessionId { get; set; }
}

/// <summary>
/// Implementation of the provider that Application uses.
/// </summary>
public sealed class OrleansActiveUserProvider(IGrainFactory grainFactory) : IActiveUserProvider
{
    public IActiveUser GetUser(Guid userId) => grainFactory.GetGrain<IUserGrain>(userId);
}
