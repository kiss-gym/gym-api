using GymApi.Application.SessionTracking;
using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using GymApi.Infrastructure.SessionTracking;
using NSubstitute;
using NUnit.Framework;

namespace GymApi.Tests.SessionTracking;

[TestFixture]
public sealed class SessionScenarioTests
{
    private InMemorySessionRepository _repository = null!;
    private IUserContext _userContext = null!;
    private CurrentSessionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemorySessionRepository();
        _userContext = Substitute.For<IUserContext>();
        _service = new CurrentSessionService(_repository, _userContext);
    }

    [Test]
    public async Task NewSession_WithMultipleExercises_PersistsCorrectly()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);
        _userContext.IsAuthenticated.Returns(true);

        // Create and add exercises
        var session = await _service.CreateSessionAsync(userId);
        await _service.AddExerciseAsync(session.Id, "Squat", null, null);
        await _service.AddExerciseAsync(session.Id, "Bench", null, null);
        await _service.FinishSessionAsync(session.Id);

        // Verify final state
        var saved = await _repository.FindAsync(session.Id);
        Assert.That(saved, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(saved!.Status, Is.EqualTo(SessionStatus.Finished));
            Assert.That(saved.Exercises, Has.Count.EqualTo(2));
            Assert.That(saved.Exercises.All(e => e.IsFinished), Is.True);
        });
    }

    [Test]
    public async Task InheritedSession_ExecutingPendingAndNewExercises_PersistsCorrectly()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);
        _userContext.IsAuthenticated.Returns(true);

        // 1. Setup previous session
        var previous = TrainingSession.Create(userId);
        previous.AddExercise("Deadlift", null, null);
        previous.Finish();
        await _repository.SaveAsync(previous);

        // 2. Inherit and perform
        var session = await _service.CreateSessionAsync(userId, previous.Id);
        var pendingId = session.Exercises[0].Id;
        
        await _service.StartExerciseAsync(session.Id, pendingId); // Start inherited
        await _service.AddExerciseAsync(session.Id, "Pull-up", null, null); // Add new
        await _service.FinishSessionAsync(session.Id);

        // 3. Verify
        var saved = await _repository.FindAsync(session.Id);
        Assert.Multiple(() =>
        {
            Assert.That(saved!.Exercises, Has.Count.EqualTo(2));
            Assert.That(saved.Exercises[0].AutoLabel, Is.EqualTo("Deadlift"));
            Assert.That(saved.Exercises[1].AutoLabel, Is.EqualTo("Pull-up"));
            Assert.That(saved.Status, Is.EqualTo(SessionStatus.Finished));
        });
    }
}
