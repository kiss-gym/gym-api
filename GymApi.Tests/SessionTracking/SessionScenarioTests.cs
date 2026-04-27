using GymApi.Application.SessionTracking;
using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using NSubstitute;
using NUnit.Framework;
using Orleans;

namespace GymApi.Tests.SessionTracking;

[TestFixture]
public sealed class SessionScenarioTests
{
    private IGrainFactory _grainFactory = null!;
    private IUserContext _userContext = null!;
    private CurrentSessionService _service = null!;
    private Dictionary<Guid, ITrainingSessionGrain> _sessionGrains = new();
    private Dictionary<Guid, IUserGrain> _userGrains = new();

    [SetUp]
    public void SetUp()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _userContext = Substitute.For<IUserContext>();
        _sessionGrains.Clear();
        _userGrains.Clear();
        
        _grainFactory.GetGrain<ITrainingSessionGrain>(Arg.Any<Guid>())
            .Returns(x => 
            {
                var id = x.Arg<Guid>();
                if (!_sessionGrains.TryGetValue(id, out var grain))
                {
                    grain = new FakeTrainingSessionGrain(id);
                    _sessionGrains[id] = grain;
                }
                return grain;
            });

        _grainFactory.GetGrain<IUserGrain>(Arg.Any<Guid>())
            .Returns(x =>
            {
                var id = x.Arg<Guid>();
                if (!_userGrains.TryGetValue(id, out var grain))
                {
                    grain = new FakeUserGrain();
                    _userGrains[id] = grain;
                }
                return grain;
            });

        _service = new CurrentSessionService(_grainFactory, _userContext);
    }

    [Test]
    public async Task CompleteTrainingWorkflow_WithInheritance_WorksCorrectly()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);
        _userContext.IsAuthenticated.Returns(true);

        // 1. Create previous session
        var prevSession = await _service.CreateSessionAsync(userId);
        await _service.AddExerciseAsync(prevSession.Id, "Old Squat", null, null);
        await _service.FinishSessionAsync(prevSession.Id);

        // 2. Create new session inheriting from previous
        var newSession = await _service.CreateSessionAsync(userId, prevSession.Id);
        
        // 3. Verify user has latest session index
        var userGrain = _grainFactory.GetGrain<IUserGrain>(userId);
        var latestId = await userGrain.GetLatestSessionIdAsync();
        
        Assert.Multiple(() =>
        {
            Assert.That(latestId, Is.EqualTo(newSession.Id));
            Assert.That(newSession.Exercises, Has.Count.EqualTo(1));
            Assert.That(newSession.Exercises[0].AutoLabel, Is.EqualTo("Old Squat"));
        });
    }

    private class FakeUserGrain : IUserGrain
    {
        private Guid? _latestSessionId;
        public Task SetLatestSessionAsync(Guid sessionId) { _latestSessionId = sessionId; return Task.CompletedTask; }
        public Task<Guid?> GetLatestSessionIdAsync() => Task.FromResult(_latestSessionId);
    }

    private class FakeTrainingSessionGrain(Guid id) : ITrainingSessionGrain
    {
        private TrainingSession _state = null!;
        public Task<TrainingSession> GetStateAsync() => Task.FromResult(_state);
        public Task<TrainingSession> InitializeAsync(Guid userId, TrainingSession? previousSession = null)
        {
            _state = TrainingSession.Create(userId);
            if (previousSession != null) _state.InheritFrom(previousSession);
            return Task.FromResult(_state);
        }
        public Task<(ExerciseEntry Entry, TrainingSession State)> AddExerciseAsync(string autoLabel, string? photoUrl, DateTimeOffset? maxEndAt, IEnumerable<ExerciseProperty>? properties = null)
            => Task.FromResult((_state.AddExercise(autoLabel, photoUrl, maxEndAt, properties), _state));
        public Task<(ExerciseEntry Entry, TrainingSession State)> StartExerciseAsync(Guid exerciseId, DateTimeOffset? maxEndAt = null)
            => Task.FromResult((_state.StartExercise(exerciseId, maxEndAt), _state));
        public Task<TrainingSession> FinishExerciseAsync(Guid exerciseId) { _state.FinishExercise(exerciseId); return Task.FromResult(_state); }
        public Task<TrainingSession> RemoveExerciseAsync(Guid exerciseId) { _state.RemoveExercise(exerciseId); return Task.FromResult(_state); }
        public Task<TrainingSession> FinishAsync() { _state.Finish(); return Task.FromResult(_state); }
    }
}
