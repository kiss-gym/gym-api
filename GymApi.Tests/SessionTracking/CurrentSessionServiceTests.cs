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
    private ISessionRepository _repository = null!;
    private IUserContext _userContext = null!;
    private CurrentSessionService _service = null!;
    private ITrainingSessionGrain _grain = null!;

    [SetUp]
    public void SetUp()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _repository = Substitute.For<ISessionRepository>();
        _userContext = Substitute.For<IUserContext>();
        _grain = Substitute.For<ITrainingSessionGrain>();
        
        _grainFactory.GetGrain<ITrainingSessionGrain>(Arg.Any<Guid>()).Returns(_grain);
        
        _service = new CurrentSessionService(_grainFactory, _repository, _userContext);
    }

    [Test]
    public async Task CreateAsync_WithoutInheritance_CreatesAndSavesNewSession()
    {
        var userId = Guid.NewGuid();
        var session = TrainingSession.Create(userId);
        _grain.InitializeAsync(userId, null).Returns(session);
        _grain.GetStateAsync().Returns(session);

        var result = await _service.CreateSessionAsync(userId);

        await _grain.Received(1).InitializeAsync(userId, null);
        await _repository.Received(1).SaveAsync(
            Arg.Is<TrainingSession>(s => s.UserId == userId),
            Arg.Any<CancellationToken>());

        Assert.Multiple(() =>
        {
            Assert.That(result.UserId, Is.EqualTo(userId));
        });
    }

    [Test]
    public async Task CreateAsync_WithInheritance_ClonesExercisesFromPreviousSession()
    {
        var userId = Guid.NewGuid();
        var previous = TrainingSession.Create(userId);
        previous.AddExercise("Squat", null, null);
        previous.Finish();

        var session = TrainingSession.Create(userId);
        session.InheritFrom(previous);
        
        _repository.FindAsync(previous.Id, Arg.Any<CancellationToken>()).Returns(previous);
        _grain.InitializeAsync(userId, previous).Returns(session);
        _grain.GetStateAsync().Returns(session);

        var result = await _service.CreateSessionAsync(userId, previous.Id);

        await _grain.Received(1).InitializeAsync(userId, previous);
        Assert.That(result.InheritedFromSessionId, Is.EqualTo(previous.Id));
    }

    [Test]
    public async Task AddExerciseAsync_AddsExerciseAndPersists()
    {
        var userId = Guid.NewGuid();
        var session = TrainingSession.Create(userId);
        var exercise = ExerciseEntry.CreatePending("Bench Press", null);
        
        _grain.GetStateAsync().Returns(session);
        _grain.AddExerciseAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<IEnumerable<ExerciseProperty>?>())
            .Returns((exercise, session));

        var result = await _service.AddExerciseAsync(session.Id, "Bench Press", null, null);

        await _grain.Received(1).AddExerciseAsync("Bench Press", null, null, null);
        await _repository.Received(1).SaveAsync(session, Arg.Any<CancellationToken>());
        Assert.That(result.AutoLabel, Is.EqualTo("Bench Press"));
    }

    [Test]
    public async Task FinishAsync_FinishesSessionAndPersists()
    {
        var userId = Guid.NewGuid();
        var session = TrainingSession.Create(userId);
        session.Finish();
        
        _grain.GetStateAsync().Returns(session);
        _grain.FinishAsync().Returns(session);

        var result = await _service.FinishSessionAsync(session.Id);

        await _grain.Received(1).FinishAsync();
        await _repository.Received(1).SaveAsync(session, Arg.Any<CancellationToken>());
        Assert.That(result.Status, Is.EqualTo(SessionStatus.Finished));
    }
}
