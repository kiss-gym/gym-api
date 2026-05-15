using GymApi.Application.UserManagement;
using GymApi.Domain.UserManagement;
using GymApi.Tests.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace GymApi.Tests.UserManagement;

[TestFixture]
public class UserServiceTests
{
    private IUserRepository _repoMock = null!;
    private MockUserContext _userContext = null!;
    private UserService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock = Substitute.For<IUserRepository>();
        _userContext = new MockUserContext();
        _service = new UserService(_repoMock, _userContext);
    }

    [Test]
    public async Task GetCurrentUser_WhenNotAuthenticated_ReturnsNull()
    {
        _userContext.UserId = null;

        var result = await _service.GetCurrentUserAsync();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetCurrentUser_WhenAuthenticated_ReturnsUser()
    {
        var userId = Guid.NewGuid();
        var user = User.Create(userId, "test@test.com", "Test");
        _userContext.UserId = userId;
        _repoMock.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _service.GetCurrentUserAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo(userId));
        });
    }
}
