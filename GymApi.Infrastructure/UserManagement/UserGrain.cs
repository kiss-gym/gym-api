using GymApi.Domain.UserManagement;
using Orleans;
using Orleans.Runtime;

namespace GymApi.Infrastructure.UserManagement;

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
