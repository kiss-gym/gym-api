using GymApi.Application.SessionTracking;
using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;

namespace GymApi.Tests.SessionTracking;

[TestFixture]
public sealed class CurrentSessionServiceTests
{
    private ISessionRepository _repository = null!;
    private IUserContext _userContext = null!;
    private CurrentSessionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<ISessionRepository>();
        _userContext = Substitute.For<IUserContext>();
        _service = new CurrentSessionService(_repository, _userContext);
    }

    [Test]
    public async Task CreateAsync_WithoutInheritance_CreatesAndSavesNewSession()
    {
        var userId = Guid.NewGuid();

        var result = await _service.CreateSessionAsync(userId);

        await _repository.Received(1).SaveAsync(
            Arg.Is<TrainingSession>(s => s.UserId == userId && s.Status == SessionStatus.Active),
            Arg.Any<CancellationToken>());

        Assert.Multiple(() =>
        {
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.InheritedFromSessionId, Is.Null);
            Assert.That(result.Exercises, Is.Empty);
        });
    }

    [Test]
    public async Task CreateAsync_WithInheritance_ClonesExercisesFromPreviousSession()
    {
        var userId = Guid.NewGuid();
        var previous = TrainingSession.Create(userId);
        previous.AddExercise("Squat", null, null);
        previous.Finish();

        _repository.FindAsync(previous.Id, Arg.Any<CancellationToken>()).Returns(previous);

        var result = await _service.CreateSessionAsync(userId, previous.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.InheritedFromSessionId, Is.EqualTo(previous.Id));
            Assert.That(result.Exercises.Count, Is.EqualTo(1));
            Assert.That(result.Exercises[0].AutoLabel, Is.EqualTo("Squat"));
            Assert.That(result.Exercises[0].IsPending, Is.True);
        });
    }

    [Test]
    public void GetAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        _repository.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).ReturnsNull();

        Assert.That(async () => await _service.GetSessionAsync(Guid.NewGuid()), Throws.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public void GetAsync_WhenAuthenticatedAsDifferentUser_ThrowsUnauthorizedAccessException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var session = TrainingSession.Create(userId);

        _repository.FindAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _userContext.UserId.Returns(otherUserId);
        _userContext.IsAuthenticated.Returns(true);

        Assert.That(async () => await _service.GetSessionAsync(session.Id), Throws.TypeOf<UnauthorizedAccessException>());
    }

    [Test]
    public async Task AddExerciseAsync_AddsExerciseAndPersists()
    {
        var userId = Guid.NewGuid();
        var session = TrainingSession.Create(userId);
        _repository.FindAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _userContext.UserId.Returns(userId);
        _userContext.IsAuthenticated.Returns(true);

        var result = await _service.AddExerciseAsync(session.Id, "Bench Press", null, null);

        await _repository.Received(1).SaveAsync(session, Arg.Any<CancellationToken>());
        Assert.Multiple(() =>
        {
            Assert.That(result.AutoLabel, Is.EqualTo("Bench Press"));
            Assert.That(result.IsRunning, Is.True);
        });
    }

    [Test]
    public async Task RemoveExerciseAsync_RemovesAndPersists()
    {
        var userId = Guid.NewGuid();
        var session = TrainingSession.Create(userId);
        var exercise = session.AddExercise("Dip", null, null);
        _repository.FindAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _userContext.UserId.Returns(userId);
        _userContext.IsAuthenticated.Returns(true);

        await _service.RemoveExerciseAsync(session.Id, exercise.Id);

        await _repository.Received(1).SaveAsync(session, Arg.Any<CancellationToken>());
        Assert.That(session.Exercises, Is.Empty);
    }

    [Test]
    public async Task FinishAsync_FinishesSessionAndPersists()
    {
        var userId = Guid.NewGuid();
        var session = TrainingSession.Create(userId);
        _repository.FindAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _userContext.UserId.Returns(userId);
        _userContext.IsAuthenticated.Returns(true);

        var result = await _service.FinishSessionAsync(session.Id);

        await _repository.Received(1).SaveAsync(session, Arg.Any<CancellationToken>());
        Assert.That(result.Status, Is.EqualTo(SessionStatus.Finished));
    }

    [Test]
    public async Task StartExerciseAsync_StartsPendingAndPersists()
    {
        var userId = Guid.NewGuid();
        var previous = TrainingSession.Create(userId);
        previous.AddExercise("Romanian DL", null, null);
        previous.Finish();

        var session = TrainingSession.Create(previous.UserId);
        session.InheritFrom(previous);

        var pendingId = session.Exercises[0].Id;
        _repository.FindAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _userContext.UserId.Returns(userId);
        _userContext.IsAuthenticated.Returns(true);

        var result = await _service.StartExerciseAsync(session.Id, pendingId);

        await _repository.Received(1).SaveAsync(session, Arg.Any<CancellationToken>());
        Assert.That(result.IsRunning, Is.True);
    }
}
