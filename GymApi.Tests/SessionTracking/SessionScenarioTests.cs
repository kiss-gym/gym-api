using GymApi.Application.SessionTracking;
using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using GymApi.Infrastructure.SessionTracking;
using NSubstitute;
using NUnit.Framework;
using Orleans;

namespace GymApi.Tests.SessionTracking;

[TestFixture]
public sealed class SessionScenarioTests
{
    private IGrainFactory _grainFactory = null!;
    private InMemorySessionRepository _repository = null!;
    private IUserContext _userContext = null!;
    private CurrentSessionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _repository = new InMemorySessionRepository();
        _userContext = Substitute.For<IUserContext>();
        
        _grainFactory.GetGrain<ITrainingSessionGrain>(Arg.Any<Guid>())
            .Returns(x => 
            {
                var id = x.Arg<Guid>();
                return new FakeTrainingSessionGrain(id);
            });

        _service = new CurrentSessionService(_grainFactory, _repository, _userContext);
    }

    [Test]
    public async Task NewSession_WithMultipleExercises_PersistsCorrectly()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);
        _userContext.IsAuthenticated.Returns(true);

        var session = await _service.CreateSessionAsync(userId);
        await _service.AddExerciseAsync(session.Id, "Squat", null, null);
        await _service.AddExerciseAsync(session.Id, "Bench", null, null);
        await _service.FinishSessionAsync(session.Id);

        var saved = await _repository.FindAsync(session.Id);
        Assert.That(saved, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(saved!.Status, Is.EqualTo(SessionStatus.Finished));
            Assert.That(saved.Exercises, Has.Count.EqualTo(2));
        });
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

        public Task<TrainingSession> FinishExerciseAsync(Guid exerciseId)
        {
            _state.FinishExercise(exerciseId);
            return Task.FromResult(_state);
        }

        public Task<TrainingSession> RemoveExerciseAsync(Guid exerciseId)
        {
            _state.RemoveExercise(exerciseId);
            return Task.FromResult(_state);
        }

        public Task<TrainingSession> FinishAsync()
        {
            _state.Finish();
            return Task.FromResult(_state);
        }
    }
}
