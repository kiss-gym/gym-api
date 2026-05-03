using GymApi.Application.SessionTracking;
using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using NSubstitute;
using NUnit.Framework;

namespace GymApi.Tests.SessionTracking;

[TestFixture]
public sealed class SessionScenarioTests
{
    private ITrainingSessionLifecycleProvider _sessionLifecycleProvider = null!;
    private IActiveUserProvider _userProvider = null!;
    private IUserContext _userContext = null!;
    private TrainingSessionLifecycleService _lifecycleService = null!;
    private ITrainingSessionRepository _repository = null!;
    private readonly Dictionary<Guid, ITrainingSessionLifecycle> _sessionFakes = new();
    private readonly Dictionary<Guid, IActiveUser> _userFakes = new();

    [SetUp]
    public void SetUp()
    {
        _sessionLifecycleProvider = Substitute.For<ITrainingSessionLifecycleProvider>();
        _userProvider = Substitute.For<IActiveUserProvider>();
        _userContext = Substitute.For<IUserContext>();
        _repository = new GymApi.Infrastructure.SessionTracking.InMemoryTrainingSessionRepository();
        _sessionFakes.Clear();
        _userFakes.Clear();
        
        _sessionLifecycleProvider.GetTrainingSessionLifecycle(Arg.Any<Guid>())
            .Returns(x => 
            {
                var id = x.Arg<Guid>();
                if (_sessionFakes.TryGetValue(id, out var fake))
                {
                    return fake;
                }

                fake = new FakeTrainingSessionLifecycle(id, _repository); // Pass the ID and repo to the Fake
                _sessionFakes[id] = fake;
                return fake;
            });

        _userProvider.GetUser(Arg.Any<Guid>())
            .Returns(x =>
            {
                var id = x.Arg<Guid>();
                if (_userFakes.TryGetValue(id, out var fake))
                {
                    return fake;
                }

                fake = new FakeActiveUser();
                _userFakes[id] = fake;
                return fake;
            });

        _lifecycleService = new TrainingSessionLifecycleService(_sessionLifecycleProvider, _userProvider, _userContext, _repository);
    }

    [Test]
    public async Task CompleteTrainingWorkflow_WithInheritance_WorksCorrectly()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);
        _userContext.IsAuthenticated.Returns(true);

        // 1. Create a previous session
        var prevSession = await _lifecycleService.CreateSessionAsync(userId);
        await _lifecycleService.AddExerciseAsync(prevSession.Id, "Old Squat", null, null);
        await _lifecycleService.FinishSessionAsync(prevSession.Id);

        // 2. Create a new session inheriting from previous
        var newSession = await _lifecycleService.CreateSessionAsync(userId, prevSession.Id);
        
        // 3. Verify user has the latest session index
        var activeUser = _userProvider.GetUser(userId);
        var latestId = await activeUser.GetLatestSessionIdAsync();
        
        Assert.Multiple(() =>
        {
            Assert.That(latestId, Is.EqualTo(newSession.Id));
            Assert.That(newSession.Exercises, Has.Count.EqualTo(1));
            Assert.That(newSession.Exercises[0].AutoLabel, Is.EqualTo("Old Squat"));
        });
    }

    [Test]
    public async Task DeleteSessionAsync_DeletesSessionSuccessfully()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);
        _userContext.IsAuthenticated.Returns(true);

        var session = await _lifecycleService.CreateSessionAsync(userId);
        var sessionId = session.Id;

        await _lifecycleService.DeleteSessionAsync(sessionId);

        // Attempting to get the session should now fail or return null/default
        // The FakeTrainingSessionLifecycle will return null for _state after deletion
        var deletedSessionState = await _sessionFakes[sessionId].GetStateAsync();
        Assert.That(deletedSessionState, Is.Null, "Session state should be null after deletion.");
    }

    [Test]
    public void DeleteSessionAsync_ThrowsUnauthorizedAccessException_WhenUserIsNotOwner()
    {
        var ownerUserId = Guid.NewGuid();
        var unauthorizedUserId = Guid.NewGuid();
        _userContext.UserId.Returns(ownerUserId);
        _userContext.IsAuthenticated.Returns(true);

        // Create a session by the owner
        var session = _lifecycleService.CreateSessionAsync(ownerUserId).Result;
        var sessionId = session.Id;

        // Change user context to an unauthorized user
        _userContext.UserId.Returns(unauthorizedUserId);

        // Attempt to delete the session as the unauthorized user
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _lifecycleService.DeleteSessionAsync(sessionId);
        });
    }

    [Test]
    public async Task CreateSessionAsync_ThrowsInvalidOperationException_WhenActiveSessionExists()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);
        _userContext.IsAuthenticated.Returns(true);

        // Create an initial active session
        var unused = await _lifecycleService.CreateSessionAsync(userId);
        
        // Attempt to create a second session for the same user
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _lifecycleService.CreateSessionAsync(userId);
        }, "Should throw InvalidOperationException when trying to create a new session while one is active.");
    }

    [Test]
    public async Task GetSessionsAsync_ReturnsFilteredAndSortedSessions()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        _userContext.UserId.Returns(userId1);
        _userContext.IsAuthenticated.Returns(true);

        // Create 3 sessions for user1
        var s1 = await _lifecycleService.CreateSessionAsync(userId1);
        await _lifecycleService.FinishSessionAsync(s1.Id);
        
        // Wait a bit to ensure different timestamps if needed, though they should be different enough
        await Task.Delay(10);
        var s2 = await _lifecycleService.CreateSessionAsync(userId1);
        await _lifecycleService.FinishSessionAsync(s2.Id);

        var s3 = await _lifecycleService.CreateSessionAsync(userId1); // Still active

        // Create 1 session for user2
        var s4 = await _lifecycleService.CreateSessionAsync(userId2);
        await _lifecycleService.FinishSessionAsync(s4.Id);

        // Test filtering by userId1
        var user1Sessions = await _lifecycleService.GetSessionsAsync(userId: userId1);
        Assert.That(user1Sessions, Has.Count.EqualTo(3));

        // Test filtering by status Active
        var activeSessions = await _lifecycleService.GetSessionsAsync(status: SessionStatus.Active);
        Assert.That(activeSessions, Has.Count.EqualTo(1));
        Assert.That(activeSessions[0].Id, Is.EqualTo(s3.Id));

        // Test filtering by status Finished
        var finishedSessions = await _lifecycleService.GetSessionsAsync(status: SessionStatus.Finished);
        Assert.That(finishedSessions, Has.Count.EqualTo(3)); // s1, s2, s4

        // Test sorting by finishedAt desc
        var sortedSessions = await _lifecycleService.GetSessionsAsync(userId: userId1, sort: "finishedAt:desc");
        // s3 is active so finishedAt is null. s2 finished after s1.
        // In LINQ to Objects, OrderByDescending puts nulls last.
        Assert.That(sortedSessions[0].Id, Is.EqualTo(s2.Id));
        Assert.That(sortedSessions[1].Id, Is.EqualTo(s1.Id));
        Assert.That(sortedSessions[2].Id, Is.EqualTo(s3.Id));

        // Test pagination
        var pagedSessions = await _lifecycleService.GetSessionsAsync(userId: userId1, page: 1, pageSize: 2);
        Assert.That(pagedSessions, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task CreateSession_WithLabel_SetsLabel()
    {
        var userId = Guid.NewGuid();
        const string label = "Morning Workout";

        var session = await _lifecycleService.CreateSessionAsync(userId, label: label);

        Assert.That(session.Label, Is.EqualTo(label));
    }

    [Test]
    public async Task RenameSession_UpdatesLabel()
    {
        var userId = Guid.NewGuid();
        var session = await _lifecycleService.CreateSessionAsync(userId);
        const string newLabel = "Evening Session";

        var updatedSession = await _lifecycleService.RenameSessionAsync(session.Id, newLabel);

        Assert.That(updatedSession.Label, Is.EqualTo(newLabel));
        
        var retrievedSession = await _lifecycleService.GetSessionAsync(session.Id);
        Assert.That(retrievedSession.Label, Is.EqualTo(newLabel));
    }

    private class FakeActiveUser : IActiveUser
    {
        private Guid? _latestSessionId;
        public Task SetLatestSessionAsync(Guid sessionId) { _latestSessionId = sessionId; return Task.CompletedTask; }
        public Task<Guid?> GetLatestSessionIdAsync() => Task.FromResult(_latestSessionId);
    }

    private class FakeTrainingSessionLifecycle : ITrainingSessionLifecycle
    {
        private TrainingSession? _state; // Now nullable
        private readonly Guid _id; // Store the ID for this fake session
        private readonly ITrainingSessionRepository _repository;

        public FakeTrainingSessionLifecycle(Guid id, ITrainingSessionRepository repository) // Constructor takes the ID
        {
            _id = id;
            _repository = repository;
            // Initialize _state to a dummy session using its ID to prevent NREs before InitializeAsync
            _state = TrainingSession.Create(Guid.Empty, _id); 
        }

        public Task<TrainingSession> GetStateAsync() => Task.FromResult(_state!); // Use null-forgiving operator as it might be null after deletion
        public async Task<TrainingSession> InitializeAsync(Guid userId, TrainingSession? parentSession = null, string? label = null)
        {
            // When initialized, create the real session using the correct ID
            _state = TrainingSession.Create(userId, _id, label);
            if (parentSession != null)
            {
                _state.InheritFrom(parentSession);
            }

            await _repository.SaveAsync(_state);
            return _state;
        }
        public async Task<(ExerciseEntry Entry, TrainingSession State)> AddExerciseAsync(string autoLabel, string? photoUrl, DateTimeOffset? maxEndAt, IEnumerable<ExerciseProperty>? properties = null)
        {
            var entry = _state!.AddExercise(autoLabel, photoUrl, maxEndAt, properties);
            await _repository.SaveAsync(_state);
            return (entry, _state);
        }
        public async Task<(ExerciseEntry Entry, TrainingSession State)> StartExerciseAsync(Guid exerciseId, DateTimeOffset? maxEndAt = null)
        {
            var entry = _state!.StartExercise(exerciseId, maxEndAt);
            await _repository.SaveAsync(_state);
            return (entry, _state);
        }
        public async Task<TrainingSession> FinishExerciseAsync(Guid exerciseId)
        {
            _state!.FinishExercise(exerciseId);
            await _repository.SaveAsync(_state);
            return _state;
        }
        public async Task<TrainingSession> RemoveExerciseAsync(Guid exerciseId)
        {
            _state!.RemoveExercise(exerciseId);
            await _repository.SaveAsync(_state);
            return _state;
        }
        public async Task<TrainingSession> RenameAsync(string label)
        {
            _state!.Rename(label);
            await _repository.SaveAsync(_state);
            return _state;
        }
        public async Task<TrainingSession> FinishAsync()
        {
            _state!.Finish();
            await _repository.SaveAsync(_state);
            return _state;
        }
        public async Task DeleteAsync()
        {
            _state = null; // Simulate deletion by clearing the state
            await _repository.DeleteAsync(_id);
        }
    }
}
