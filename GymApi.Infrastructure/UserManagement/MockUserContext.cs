using GymApi.Domain.UserManagement;

namespace GymApi.Infrastructure.UserManagement;

/// <summary>
/// A mock implementation of IUserContext for development and testing.
/// In a real app, this would be backed by HttpContext/JWT.
/// </summary>
public sealed class MockUserContext : IUserContext
{
    public Guid? UserId { get; set; }
}