using Orleans;

namespace GymApi.Domain.SessionTracking;

[GenerateSerializer]
[Alias("GymApi.Domain.SessionTracking.SessionStatus")]
public enum SessionStatus
{
    Active,
    Finished
}
