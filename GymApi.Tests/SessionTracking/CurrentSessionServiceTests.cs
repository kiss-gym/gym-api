using GymApi.Application.SessionTracking;
using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;
using Orleans;

namespace GymApi.Tests.SessionTracking;

[TestFixture]
public sealed class CurrentSessionServiceTests
{
    private IGrainFactory _grainFactory = null!;
    private IUserContext _userContext = null!;
    private CurrentSessionService _service = null!;
    private ITrainingSessionGrain _sessionGrain = null!;
    private IUserGrain _userGrain = null!;

    [SetUp]
    public void SetUp()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _userContext = Substitute.For<IUserContext>();
        _sessionGrain = Substitute.For<ITrainingSessionGrain>();
        _userGrain = Substitute.For<IUserGrain>();
        
        _grainFactory.GetGrain<ITrainingSessionGrain>(Arg.Any<Guid>()).Returns(_sessionGrain);
        _grainFactory.GetGrain<IUserGrain>(Arg.Any<Guid>()).Returns(_userGrain);
        
        _service = new CurrentSessionService(_grainFactory, _userContext);
    }

    [Test]
    public async Task CreateAsync_WithoutInheritance_InitializesGrainAndUpdatesUser()
    {
        var userId = Guid.NewGuid();
        var session = TrainingSession.Create(userId);
        _sessionGrain.InitializeAsync(userId, null).Returns(session);

        var result = await _service.CreateSessionAsync(userId);

        await _sessionGrain.Received(1).InitializeAsync(userId, null);
        await _userGrain.Received(1).SetLatestSessionAsync(result.Id);
        Assert.That(result.UserId, Is.EqualTo(userId));
    }

    [Test]
    public async Task CreateAsync_WithInheritance_FetchesPreviousGrainState()
    {
        var userId = Guid.NewGuid();
        var previousId = Guid.NewGuid();
        var previous = TrainingSession.Create(userId);
        var session = TrainingSession.Create(userId);
        session.InheritFrom(previous);

        var previousGrain = Substitute.For<ITrainingSessionGrain>();
        _grainFactory.GetGrain<ITrainingSessionGrain>(previousId).Returns(previousGrain);
        previousGrain.GetStateAsync().Returns(previous);
        _sessionGrain.InitializeAsync(userId, previous).Returns(session);

        var result = await _service.CreateSessionAsync(userId, previousId);

        await _sessionGrain.Received(1).InitializeAsync(userId, previous);
        Assert.That(result.InheritedFromSessionId, Is.EqualTo(previousId));
    }

    [Test]
    public async Task GetAsync_WhenAuthenticatedAsDifferentUser_ThrowsUnauthorizedAccessException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var session = TrainingSession.Create(userId);

        _sessionGrain.GetStateAsync().Returns(session);
        _userContext.UserId.Returns(otherUserId);
        _userContext.IsAuthenticated.Returns(true);

        Assert.That(async () => await _service.GetSessionAsync(session.Id), Throws.TypeOf<UnauthorizedAccessException>());
    }

    [Test]
    public async Task AddExerciseAsync_CallsGrainAndReturnsEntry()
    {
        var sessionId = Guid.NewGuid();
        var exercise = ExerciseEntry.CreatePending("Squat", null);
        var session = TrainingSession.Create(Guid.NewGuid());
        
        _sessionGrain.AddExerciseAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<IEnumerable<ExerciseProperty>?>())
            .Returns((exercise, session));

        var result = await _service.AddExerciseAsync(sessionId, "Squat", null, null);

        await _sessionGrain.Received(1).AddExerciseAsync("Squat", null, null, null);
        Assert.That(result.AutoLabel, Is.EqualTo("Squat"));
    }
}
