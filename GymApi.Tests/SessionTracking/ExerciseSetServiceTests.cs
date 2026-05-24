using GymApi.Application.SessionTracking;
using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using GymApi.Tests.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace GymApi.Tests.SessionTracking;

/// <summary>
/// Unit tests for ExerciseSet service methods in TrainingSessionService.
/// RED: fail until service methods are implemented.
/// </summary>
[TestFixture]
public sealed class ExerciseSetServiceTests
{
    private IUserContext _userContext = null!;
    private ITrainingSessionRepository _repository = null!;
    private TrainingSessionService _service = null!;
    private Guid _userId;

    [SetUp]
    public void SetUp()
    {
        _userId = Guid.NewGuid();
        _userContext = Substitute.For<IUserContext>();
        _userContext.UserId.Returns(_userId);
        _userContext.IsAuthenticated.Returns(true);
        _repository = new InMemoryTrainingSessionRepository();
        _service = new TrainingSessionService(_userContext, _repository);
    }

    // ── AddSetAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task AddSetAsync_ValidExercise_ReturnsSet()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Squat", null);

        var set = await _service.AddSetAsync(session.Id, exercise.Id, 100m, 5);

        Assert.Multiple(() =>
        {
            Assert.That(set, Is.Not.Null);
            Assert.That(set.SetNumber, Is.EqualTo(1));
            Assert.That(set.Weight, Is.EqualTo(100m));
            Assert.That(set.Repetitions, Is.EqualTo(5));
            Assert.That(set.IsCompleted, Is.False);
        });
    }

    [Test]
    public async Task AddSetAsync_SecondSet_AutoIncrementsSetNumber()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Squat", null);
        await _service.AddSetAsync(session.Id, exercise.Id, 100m, 5);

        var secondSet = await _service.AddSetAsync(session.Id, exercise.Id, 120m, 3);

        Assert.That(secondSet.SetNumber, Is.EqualTo(2));
    }

    [Test]
    public async Task AddSetAsync_PersistsToRepository()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Bench", null);
        await _service.AddSetAsync(session.Id, exercise.Id, 80m, 8);

        var retrieved = await _service.GetSessionAsync(session.Id);

        Assert.That(retrieved.Exercises[0].SortedSets, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AddSetAsync_UnknownSession_ThrowsKeyNotFoundException()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.AddSetAsync(Guid.NewGuid(), Guid.NewGuid(), 100m, 5));
    }

    [Test]
    public async Task AddSetAsync_UnknownExercise_ThrowsKeyNotFoundException()
    {
        var session = await _service.CreateSessionAsync();

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.AddSetAsync(session.Id, Guid.NewGuid(), 100m, 5));
    }

    [Test]
    public async Task AddSetAsync_FinishedExercise_Ok()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Squat", null);
        await _service.StartExerciseAsync(session.Id, exercise.Id);
        await _service.FinishExerciseAsync(session.Id, exercise.Id);

        var set= await _service.AddSetAsync(session.Id, exercise.Id, 100m, 5);
        Assert.That(set, Is.Not.Null);
    }


    [Test]
    public async Task AddSetAsync_FinishedSession_ThrowsInvalidOperationException()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Squat", null);
        await _service.StartExerciseAsync(session.Id, exercise.Id);
        await _service.FinishSessionAsync(session.Id);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AddSetAsync(session.Id, exercise.Id, 100m, 5));
    }

    // ── AddCopyOfLastSetAsync ───────────────────────────────────────────────

    [Test]
    public async Task AddCopyOfLastSetAsync_CopiesWeightAndRepsFromLastSet()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Squat", null);
        await _service.AddSetAsync(session.Id, exercise.Id, 100m, 5);

        var copy = await _service.AddCopyOfLastSetAsync(session.Id, exercise.Id);

        Assert.Multiple(() =>
        {
            Assert.That(copy.Weight, Is.EqualTo(100m));
            Assert.That(copy.Repetitions, Is.EqualTo(5));
            Assert.That(copy.SetNumber, Is.EqualTo(2));
            Assert.That(copy.IsCompleted, Is.False);
        });
    }

    [Test]
    public async Task AddCopyOfLastSetAsync_NoExistingSets_ThrowsInvalidOperationException()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Squat", null);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AddCopyOfLastSetAsync(session.Id, exercise.Id));
    }

    // ── UpdateSetAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task UpdateSetAsync_UpdatesWeightAndReps()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "OHP", null);
        var set = await _service.AddSetAsync(session.Id, exercise.Id, 60m, 8);

        var updated = await _service.UpdateSetAsync(session.Id, exercise.Id, set.Id, 70m, 6);

        Assert.Multiple(() =>
        {
            Assert.That(updated.Weight, Is.EqualTo(70m));
            Assert.That(updated.Repetitions, Is.EqualTo(6));
        });
    }

    [Test]
    public async Task UpdateSetAsync_PersistsChanges()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Row", null);
        var set = await _service.AddSetAsync(session.Id, exercise.Id, 70m, 10);

        await _service.UpdateSetAsync(session.Id, exercise.Id, set.Id, 80m, 8);

        var retrieved = await _service.GetSessionAsync(session.Id);
        var retrievedSet = retrieved.Exercises[0].SortedSets[0];
        Assert.Multiple(() =>
        {
            Assert.That(retrievedSet.Weight, Is.EqualTo(80m));
            Assert.That(retrievedSet.Repetitions, Is.EqualTo(8));
        });
    }

    [Test]
    public async Task UpdateSetAsync_UnknownSet_ThrowsKeyNotFoundException()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Squat", null);

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateSetAsync(session.Id, exercise.Id, Guid.NewGuid(), 100m, 5));
    }

    // ── DeleteSetAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task DeleteSetAsync_RemovesSet()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Squat", null);
        var set = await _service.AddSetAsync(session.Id, exercise.Id, 100m, 5);

        await _service.DeleteSetAsync(session.Id, exercise.Id, set.Id);

        var retrieved = await _service.GetSessionAsync(session.Id);
        Assert.That(retrieved.Exercises[0].SortedSets, Is.Empty);
    }

    [Test]
    public async Task DeleteSetAsync_UnknownSet_ThrowsKeyNotFoundException()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Squat", null);

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.DeleteSetAsync(session.Id, exercise.Id, Guid.NewGuid()));
    }

    // ── CompleteSetAsync ────────────────────────────────────────────────────

    [Test]
    public async Task CompleteSetAsync_MarksSetCompleted()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Squat", null);
        var set = await _service.AddSetAsync(session.Id, exercise.Id, 100m, 5);

        var completed = await _service.CompleteSetAsync(session.Id, exercise.Id, set.Id);

        Assert.That(completed.IsCompleted, Is.True);
    }

    [Test]
    public async Task CompleteSetAsync_PersistsCompleted()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Squat", null);
        var set = await _service.AddSetAsync(session.Id, exercise.Id, 100m, 5);

        await _service.CompleteSetAsync(session.Id, exercise.Id, set.Id);

        var retrieved = await _service.GetSessionAsync(session.Id);
        Assert.That(retrieved.Exercises[0].SortedSets[0].IsCompleted, Is.True);
    }

    [Test]
    public async Task CompleteSetAsync_FinishedSession_ThrowsInvalidOperationException()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Squat", null);
        var set = await _service.AddSetAsync(session.Id, exercise.Id, 100m, 5);
        await _service.StartExerciseAsync(session.Id, exercise.Id);
        await _service.FinishSessionAsync(session.Id);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CompleteSetAsync(session.Id, exercise.Id, set.Id));
    }

    // ── UnCompleteSetAsync ──────────────────────────────────────────────────

    [Test]
    public async Task UnCompleteSetAsync_MarksSetUncompleted()
    {
        var session = await _service.CreateSessionAsync();
        var exercise = await _service.AddExerciseAsync(session.Id, "Squat", null);
        var set = await _service.AddSetAsync(session.Id, exercise.Id, 100m, 5);
        await _service.CompleteSetAsync(session.Id, exercise.Id, set.Id);

        var uncompleted = await _service.UnCompleteSetAsync(session.Id, exercise.Id, set.Id);

        Assert.That(uncompleted.IsCompleted, Is.False);
    }

}
