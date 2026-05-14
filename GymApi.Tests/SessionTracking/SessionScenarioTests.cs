using GymApi.Application.SessionTracking;
using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using GymApi.Tests.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace GymApi.Tests.SessionTracking;

[TestFixture]
public sealed class SessionScenarioTests
{
    private IUserContext _userContext = null!;
    private TrainingSessionService _service = null!;
    private ITrainingSessionRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _userContext = Substitute.For<IUserContext>();

        _userContext.UserId.Returns(Guid.NewGuid());
        _userContext.IsAuthenticated.Returns(true);

        _repository = new InMemoryTrainingSessionRepository();
        

        _service = new TrainingSessionService(_userContext, _repository);
    }

    [Test]
    public async Task CompleteTrainingWorkflow_WithInheritance_WorksCorrectly()
    {
        // 1. Create a previous session
        var prevSession = await _service.CreateSessionAsync();
        await _service.AddExerciseAsync(prevSession.Id, "Old Squat", null);
        await _service.FinishSessionAsync(prevSession.Id);

        // 2. Create a new session inheriting from previous
        var newSession = await _service.CreateSessionAsync(prevSession.Id);
        
        // 3. Verify user has the latest session index
        
        Assert.Multiple(() =>
        {
            Assert.That(newSession.Exercises, Has.Count.EqualTo(1));
            Assert.That(newSession.Exercises[0].AutoLabel, Is.EqualTo("Old Squat"));
        });
    }

    [Test]
    public async Task DeleteSessionAsync_DeletesSessionSuccessfully()
    {
        var session = await _service.CreateSessionAsync();
        var sessionId = session.Id;

        await _service.DeleteSessionAsync(sessionId);

        var foundSessions = await _service.GetSessionsAsync();
        Assert.That(foundSessions, Is.Empty);
    }


    [Test]
    public async Task GetSessionsAsync_ReturnsFilteredAndSortedSessions()
    {
        // Create 3 sessions
        var s1 = await _service.CreateSessionAsync();
        await _service.FinishSessionAsync(s1.Id);
        
        // Wait a bit to ensure different timestamps if needed, though they should be different enough
        await Task.Delay(10);
        var s2 = await _service.CreateSessionAsync();
        await _service.FinishSessionAsync(s2.Id);

        var s3 = await _service.CreateSessionAsync(); // Still active

        var user1Sessions = await _service.GetSessionsAsync();
        Assert.That(user1Sessions, Has.Count.EqualTo(3));

        // Test filtering by status Active
        var activeSessions = await _service.GetSessionsAsync(status: SessionStatus.Active);
        Assert.That(activeSessions, Has.Count.EqualTo(1));
        Assert.That(activeSessions[0].Id, Is.EqualTo(s3.Id));

        // Test filtering by status Finished
        var finishedSessions = await _service.GetSessionsAsync(status: SessionStatus.Finished);
        Assert.That(finishedSessions, Has.Count.EqualTo(2)); // s1, s2

        // Test sorting by finishedAt desc
        var sortedSessions = await _service.GetSessionsAsync(sort: "finishedAt:desc");
        // s3 is active so finishedAt is null. s2 finished after s1.
        // In LINQ to Objects, OrderByDescending puts nulls last.
        Assert.That(sortedSessions[0].Id, Is.EqualTo(s2.Id));
        Assert.That(sortedSessions[1].Id, Is.EqualTo(s1.Id));
        Assert.That(sortedSessions[2].Id, Is.EqualTo(s3.Id));

        // Test pagination
        var pagedSessions = await _service.GetSessionsAsync(page: 1, pageSize: 2);
        Assert.That(pagedSessions, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task CreateSession_WithLabel_SetsLabel()
    {
        _userContext.UserId.Returns(Guid.NewGuid());
        _userContext.IsAuthenticated.Returns(true);
        
        const string label = "Morning Workout";

        var session = await _service.CreateSessionAsync(label: label);

        Assert.That(session.Label, Is.EqualTo(label));
    }

    [Test]
    public async Task RenameSession_UpdatesLabel()
    {
        var session = await _service.CreateSessionAsync();
        const string newLabel = "Evening Session";

        var updatedSession = await _service.RenameSessionAsync(session.Id, newLabel);

        Assert.That(updatedSession.Label, Is.EqualTo(newLabel));
        
        var retrievedSession = await _service.GetSessionAsync(session.Id);
        Assert.That(retrievedSession.Label, Is.EqualTo(newLabel));
    }
}
