using GymApi.Domain.UserManagement;

namespace GymApi.Tests.Fakes;

/// <summary>
/// In-memory IUserContext for unit tests.
/// </summary>
public sealed class MockUserContext : IUserContext
{
    public Guid? UserId { get; set; }
}
