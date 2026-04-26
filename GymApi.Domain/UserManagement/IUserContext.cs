namespace GymApi.Domain.UserManagement;

/// <summary>
/// Provides access to the current authenticated user's information.
/// </summary>
public interface IUserContext
{
    Guid? UserId { get; }
    bool IsAuthenticated => UserId.HasValue;
}
