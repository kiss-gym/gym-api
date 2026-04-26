using GymApi.Application.UserManagement;
using GymApi.Domain.UserManagement;
using GymApi.Infrastructure.UserManagement;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
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

    [Test]
    public async Task Register_NewUser_SavesToRepo()
    {
        _repoMock.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var user = await _service.RegisterAsync("new@test.com", "New User");

        Assert.That(user.Email, Is.EqualTo("new@test.com"));
        await _repoMock.Received(1).SaveAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Register_ExistingUser_ThrowsException()
    {
        var existing = User.Create(Guid.NewGuid(), "exists@test.com", "Exists");
        _repoMock.GetByEmailAsync("exists@test.com", Arg.Any<CancellationToken>())
            .Returns(existing);

        Assert.That(async () => await _service.RegisterAsync("exists@test.com", "Any"), 
            Throws.TypeOf<InvalidOperationException>());
    }
}
