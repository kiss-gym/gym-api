using GymApi.Domain.SessionTracking;
using GymApi.Infrastructure.SessionTracking;
using GymApi.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace GymApi.Tests.SessionTracking;

/// <summary>
/// Integration tests for SupabaseTrainingSessionRepository against a real Supabase database.
/// </summary>
[TestFixture]
[Category("Integration")]
public sealed class SupabaseTrainingSessionRepositoryTests : RepositoryIntegrationTestBase
{
    private SupabaseTrainingSessionRepository _sut = null!;
    private static readonly Guid _userId = Guid.NewGuid();
    private static readonly Guid _otherUserId = Guid.NewGuid();

    [SetUp]
    public void SetUp() => _sut = new SupabaseTrainingSessionRepository(DbContext);

    // ── SaveAsync + GetByIdAsync ────────────────────────────────────────────

    [Test]
    public async Task SaveAsync_NewSession_CanBeRetrievedById()
    {
        var session = TrainingSession.Create(_userId, label: "Leg Day");

        await _sut.SaveAsync(session);
        var retrieved = await _sut.GetByIdAsync(session.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(retrieved!.Id, Is.EqualTo(session.Id));
            Assert.That(retrieved.UserId, Is.EqualTo(_userId));
            Assert.That(retrieved.Label, Is.EqualTo("Leg Day"));
            Assert.That(retrieved.Status, Is.EqualTo(SessionStatus.Active));
        });
    }

    [Test]
    public async Task SaveAsync_SessionWithExercisesAndProperties_RoundTripsCorrectly()
    {
        var session = TrainingSession.Create(_userId);
        session.AddExercise("Bench Press", "http://img/bench.jpg",
            [new ExerciseProperty("Weight", "80kg"), new ExerciseProperty("Reps", "8")]);

        await _sut.SaveAsync(session);
        var retrieved = await _sut.GetByIdAsync(session.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(retrieved!.Exercises, Has.Count.EqualTo(1));
            Assert.That(retrieved.Exercises[0].AutoLabel, Is.EqualTo("Bench Press"));
            Assert.That(retrieved.Exercises[0].Properties, Has.Count.EqualTo(2));
            Assert.That(retrieved.Exercises[0].Properties[0].Name, Is.EqualTo("Weight"));
            Assert.That(retrieved.Exercises[0].Properties[0].Value, Is.EqualTo("80kg"));
        });
    }

    [Test]
    public async Task SaveAsync_UpdatedSession_PersistsChanges()
    {
        var session = TrainingSession.Create(_userId);
        await _sut.SaveAsync(session);

        session.Rename("Updated Label");
        session.Finish();
        await _sut.SaveAsync(session);

        var retrieved = await _sut.GetByIdAsync(session.Id);

        Assert.Multiple(() =>
        {
            Assert.That(retrieved!.Label, Is.EqualTo("Updated Label"));
            Assert.That(retrieved.Status, Is.EqualTo(SessionStatus.Finished));
            Assert.That(retrieved.FinishedAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    // ── GetSessionsAsync ────────────────────────────────────────────────────

    [Test]
    public async Task GetSessionsAsync_ReturnsOnlySessionsForUser()
    {
        var mine = TrainingSession.Create(_userId);
        var theirs = TrainingSession.Create(_otherUserId);
        await _sut.SaveAsync(mine);
        await _sut.SaveAsync(theirs);

        var result = await _sut.GetSessionsAsync(_userId);

        Assert.That(result.Select(s => s.Id), Does.Contain(mine.Id));
        Assert.That(result.Select(s => s.Id), Does.Not.Contain(theirs.Id));
    }

    [Test]
    public async Task GetSessionsAsync_FiltersByStatus()
    {
        var active = TrainingSession.Create(_userId);
        var finished = TrainingSession.Create(_userId);
        finished.Finish();
        await _sut.SaveAsync(active);
        await _sut.SaveAsync(finished);

        var result = await _sut.GetSessionsAsync(_userId, status: SessionStatus.Active);

        Assert.That(result.All(s => s.Status == SessionStatus.Active), Is.True);
    }

    [Test]
    public async Task GetSessionsAsync_RespectsPageSize()
    {
        for (var i = 0; i < 5; i++)
            await _sut.SaveAsync(TrainingSession.Create(_userId));

        var result = await _sut.GetSessionsAsync(_userId, page: 1, pageSize: 3);

        Assert.That(result, Has.Count.EqualTo(3));
    }

    // ── CountSessionsAsync ──────────────────────────────────────────────────

    [Test]
    public async Task CountSessionsAsync_ReturnsCorrectCount()
    {
        await _sut.SaveAsync(TrainingSession.Create(_userId));
        await _sut.SaveAsync(TrainingSession.Create(_userId));
        await _sut.SaveAsync(TrainingSession.Create(_otherUserId));

        var count = await _sut.CountSessionsAsync(_userId);

        Assert.That(count, Is.EqualTo(2));
    }

    // ── DeleteAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_ExistingSession_CanNoLongerBeRetrieved()
    {
        var session = TrainingSession.Create(_userId);
        await _sut.SaveAsync(session);

        await _sut.DeleteAsync(session.Id);

        Assert.That(await _sut.GetByIdAsync(session.Id), Is.Null);
    }

    [Test]
    public async Task DeleteAsync_SessionWithExercises_CascadesDelete()
    {
        var session = TrainingSession.Create(_userId);
        session.AddExercise("Squat", null);
        await _sut.SaveAsync(session);

        await _sut.DeleteAsync(session.Id);

        var exercises = DbContext.ExerciseEntries
            .Where(e => EF.Property<Guid>(e, "session_id") == session.Id);
        Assert.That(exercises, Is.Empty);
    }

    [Test]
    public void DeleteAsync_UnknownId_DoesNotThrow()
    {
        Assert.DoesNotThrowAsync(() => _sut.DeleteAsync(Guid.NewGuid()));
    }
}
