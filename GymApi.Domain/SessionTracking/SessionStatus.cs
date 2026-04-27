using Orleans;

namespace GymApi.Domain.SessionTracking;

[GenerateSerializer]
public enum SessionStatus
{
    Active,
    Finished
}
