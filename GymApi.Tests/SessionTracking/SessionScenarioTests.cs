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
    private Dictionary<Guid, ITrainingSessionLifecycle> _sessionFakes = new(); // Renamed for clarity
    private Dictionary<Guid, IActiveUser> _userFakes = new(); // Renamed for clarity

    [SetUp]
    public void SetUp()
    {
        _sessionLifecycleProvider = Substitute.For<ITrainingSessionLifecycleProvider>();
        _userProvider = Substitute.For<IActiveUserProvider>();
        _userContext = Substitute.For<IUserContext>();
        _sessionFakes.Clear();
        _userFakes.Clear();
        
        _sessionLifecycleProvider.GetTrainingSessionLifecycle(Arg.Any<Guid>())
            .Returns(x => 
            {
                var id = x.Arg<Guid>();
                if (!_sessionFakes.TryGetValue(id, out var fake))
                {
                    fake = new FakeTrainingSessionLifecycle(id); // Pass the ID to the Fake
                    _sessionFakes[id] = fake;
                }
                return fake;
            });

        _userProvider.GetUser(Arg.Any<Guid>())
            .Returns(x =>
            {
                var id = x.Arg<Guid>();
                if (!_userFakes.TryGetValue(id, out var fake))
                {
                    fake = new FakeActiveUser();
                    _userFakes[id] = fake;
                }
                return fake;
            });

        _lifecycleService = new TrainingSessionLifecycleService(_sessionLifecycleProvider, _userProvider, _userContext);
    }

    [Test]
    public async Task CompleteTrainingWorkflow_WithInheritance_WorksCorrectly()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);
        _userContext.IsAuthenticated.Returns(true);

        // 1. Create previous session
        var prevSession = await _lifecycleService.CreateSessionAsync(userId);
        await _lifecycleService.AddExerciseAsync(prevSession.Id, "Old Squat", null, null);
        await _lifecycleService.FinishSessionAsync(prevSession.Id);

        // 2. Create new session inheriting from previous
        var newSession = await _lifecycleService.CreateSessionAsync(userId, prevSession.Id);
        
        // 3. Verify user has latest session index
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

        public FakeTrainingSessionLifecycle(Guid id) // Constructor takes the ID
        {
            _id = id;
            // Initialize _state to a dummy session using its ID to prevent NREs before InitializeAsync
            _state = TrainingSession.Create(Guid.Empty, _id); 
        }

        public Task<TrainingSession> GetStateAsync() => Task.FromResult(_state!); // Use null-forgiving operator as it might be null after deletion
        public Task<TrainingSession> InitializeAsync(Guid userId, TrainingSession? parentSession = null)
        {
            // When initialized, create the real session using the correct ID
            _state = TrainingSession.Create(userId, _id);
            if (parentSession != null) _state.InheritFrom(parentSession);
            return Task.FromResult(_state);
        }
        public Task<(ExerciseEntry Entry, TrainingSession State)> AddExerciseAsync(string autoLabel, string? photoUrl, DateTimeOffset? maxEndAt, IEnumerable<ExerciseProperty>? properties = null)
            => Task.FromResult((_state!.AddExercise(autoLabel, photoUrl, maxEndAt, properties), _state));
        public Task<(ExerciseEntry Entry, TrainingSession State)> StartExerciseAsync(Guid exerciseId, DateTimeOffset? maxEndAt = null)
            => Task.FromResult((_state!.StartExercise(exerciseId, maxEndAt), _state));
        public Task<TrainingSession> FinishExerciseAsync(Guid exerciseId) { _state!.FinishExercise(exerciseId); return Task.FromResult(_state); }
        public Task<TrainingSession> RemoveExerciseAsync(Guid exerciseId) { _state!.RemoveExercise(exerciseId); return Task.FromResult(_state); }
        public Task<TrainingSession> FinishAsync() { _state!.Finish(); return Task.FromResult(_state); }
        public Task DeleteAsync()
        {
            _state = null; // Simulate deletion by clearing the state
            return Task.CompletedTask;
        }
    }
}
