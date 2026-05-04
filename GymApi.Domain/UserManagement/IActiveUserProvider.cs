namespace GymApi.Domain.UserManagement;

public interface IActiveUserProvider
{
    IActiveUser GetUser(Guid userId);
}
