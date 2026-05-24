using GymApi.Domain.SessionTracking;
using NUnit.Framework;

namespace GymApi.Tests.SessionTracking;

[TestFixture]
public sealed class TrainingSessionTests
{
    private static readonly Guid _anyUser = Guid.NewGuid();

    // ── Session lifecycle ───────────────────────────────────────────────────

    [Test]
    public void CreateSession_SetsStatusActiveAndRecordsCreatedAtTimestamp()
    {
        var session = TrainingSession.Create(_anyUser);

        Assert.Multiple(() =>
        {
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Active));
            Assert.That(session.CreatedAt, Is.Not.EqualTo(default(DateTimeOffset)));
        });
    }

    [Test]
    public void FinishSession_SetsStatusFinishedAndRecordsFinishedAt()
    {
        var session = TrainingSession.Create(_anyUser);
        session.Finish();

        Assert.Multiple(() =>
        {
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Finished));
            Assert.That(session.FinishedAt, Is.Not.Null);
        });
    }

    [Test]
    public void Finish_OnAlreadyFinishedSession_ThrowsInvalidOperationException()
    {
        var session = TrainingSession.Create(_anyUser);
        session.Finish();

        Assert.Throws<InvalidOperationException>(session.Finish);
    }

    [Test]
    public void Rename_UpdatesLabel()
    {
        var session = TrainingSession.Create(_anyUser);
        session.Rename("New Name");

        Assert.That(session.Label, Is.EqualTo("New Name"));
    }

    [Test]
    public void Rename_OnFinishedSession_ThrowsInvalidOperationException()
    {
        var session = TrainingSession.Create(_anyUser);
        session.Finish();

        Assert.Throws<InvalidOperationException>(() => session.Rename("New Name"));
    }

    // ── AddExercise ─────────────────────────────────────────────────────────

    [Test]
    public void AddExercise_CreatesExerciseAsPending()
    {
        // New requirement: AddExercise no longer auto-starts the exercise
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Push-up", null);

        Assert.That(exercise.IsPending, Is.True);
    }

    [Test]
    public void AddExercise_AutoFinishesPreviousRunningExercise()
    {
        // A running exercise (started manually) is auto-finished when a new one is added
        var session = TrainingSession.Create(_anyUser);
        var first = session.AddExercise("Push-up", null);
        session.StartExercise(first.Id);

        session.AddExercise("Pull-up", null);

        Assert.That(first.IsFinished, Is.True);
    }

    [Test]
    public void AddExercise_WhenNoPreviousRunning_DoesNotThrow()
    {
        var session = TrainingSession.Create(_anyUser);
        session.AddExercise("Push-up", null);

        // Second add with no running exercise — should not throw
        Assert.DoesNotThrow(() => session.AddExercise("Pull-up", null));
    }

    [Test]
    public void AddExercise_OnFinishedSession_ThrowsInvalidOperationException()
    {
        var session = TrainingSession.Create(_anyUser);
        session.Finish();

        Assert.Throws<InvalidOperationException>(() => session.AddExercise("Squat", null));
    }

    // ── StartExercise ───────────────────────────────────────────────────────

    [Test]
    public void StartExercise_StartsPendingExercise()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);

        session.StartExercise(exercise.Id);

        Assert.That(exercise.IsRunning, Is.True);
    }

    [Test]
    public void StartExercise_AutoFinishesPreviousRunningExercise()
    {
        var session = TrainingSession.Create(_anyUser);
        var first = session.AddExercise("OHP", null);
        var second = session.AddExercise("Row", null);

        session.StartExercise(first.Id);
        session.StartExercise(second.Id);

        Assert.Multiple(() =>
        {
            Assert.That(first.IsFinished, Is.True);
            Assert.That(second.IsRunning, Is.True);
        });
    }

    [Test]
    public void StartExercise_OnAlreadyRunningExercise_ThrowsInvalidOperationException()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);
        session.StartExercise(exercise.Id);

        Assert.Throws<InvalidOperationException>(() => session.StartExercise(exercise.Id));
    }

    [Test]
    public void StartExercise_OnFinishedExercise_ThrowsInvalidOperationException()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);
        session.StartExercise(exercise.Id);
        session.FinishExercise(exercise.Id);

        Assert.Throws<InvalidOperationException>(() => session.StartExercise(exercise.Id));
    }

    // ── FinishExercise ──────────────────────────────────────────────────────

    [Test]
    public void FinishExercise_FinishesRunningExercise()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Push-up", null);
        session.StartExercise(exercise.Id);

        session.FinishExercise(exercise.Id);

        Assert.That(exercise.IsFinished, Is.True);
    }

    [Test]
    public void FinishExercise_OnNotRunningExercise_ThrowsInvalidOperationException()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Push-up", null);
        // exercise is pending, not running

        Assert.Throws<InvalidOperationException>(() => session.FinishExercise(exercise.Id));
    }

    [Test]
    public void Finish_AutoFinishesRunningExercise()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Deadlift", null);
        session.StartExercise(exercise.Id);

        session.Finish();

        Assert.That(exercise.IsFinished, Is.True);
    }

    [Test]
    public void Finish_CompletesAllSetsOnRunningExercise()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);
        session.StartExercise(exercise.Id);
        exercise.AddSet(100m, 5);
        exercise.AddSet(100m, 5);

        session.Finish();

        Assert.That(exercise.SortedSets.All(s => s.IsCompleted), Is.True);
    }

    // ── RemoveExercise ──────────────────────────────────────────────────────

    [Test]
    public void RemoveExercise_RemovesItFromList()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Lunge", null);
        session.RemoveExercise(exercise.Id);

        Assert.That(session.Exercises, Is.Empty);
    }

    [Test]
    public void RemoveExercise_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var session = TrainingSession.Create(_anyUser);

        Assert.Throws<KeyNotFoundException>(() => session.RemoveExercise(Guid.NewGuid()));
    }

    // ── InheritFrom ─────────────────────────────────────────────────────────

    [Test]
    public void InheritFrom_CopiesExercisesAsPending()
    {
        var previous = TrainingSession.Create(_anyUser);
        previous.AddExercise("Bench Press", "http://img/bench.jpg",
            [new ExerciseProperty("Weight", "80kg"), new ExerciseProperty("Reps", "8")]);
        previous.Finish();

        var next = TrainingSession.Create(_anyUser);
        next.InheritFrom(previous);

        Assert.Multiple(() =>
        {
            Assert.That(next.InheritedFromSessionId, Is.EqualTo(previous.Id));
            Assert.That(next.Exercises.Count, Is.EqualTo(1));
            Assert.That(next.Exercises[0].IsPending, Is.True);
            Assert.That(next.Exercises[0].AutoLabel, Is.EqualTo("Bench Press"));
            Assert.That(next.Exercises[0].PhotoUrl, Is.EqualTo("http://img/bench.jpg"));
            Assert.That(next.Exercises[0].Properties.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public void InheritFrom_DeepCopiesSets()
    {
        // Sets from previous session must be copied to the new exercise
        var previous = TrainingSession.Create(_anyUser);
        var exercise = previous.AddExercise("Squat", null);
        exercise.AddSet(100m, 5);
        exercise.AddSet(120m, 3);
        previous.Finish();

        var next = TrainingSession.Create(_anyUser);
        next.InheritFrom(previous);

        var copiedExercise = next.Exercises[0];
        Assert.Multiple(() =>
        {
            Assert.That(copiedExercise.SortedSets, Has.Count.EqualTo(2));
            Assert.That(copiedExercise.SortedSets[0].Weight, Is.EqualTo(100m));
            Assert.That(copiedExercise.SortedSets[0].Repetitions, Is.EqualTo(5));
            Assert.That(copiedExercise.SortedSets[1].Weight, Is.EqualTo(120m));
            Assert.That(copiedExercise.SortedSets[1].Repetitions, Is.EqualTo(3));
        });
    }

    [Test]
    public void InheritFrom_CopiedSetsAreNotCompleted()
    {
        // Inherited sets start fresh — not carrying finished state
        var previous = TrainingSession.Create(_anyUser);
        var exercise = previous.AddExercise("Deadlift", null);
        exercise.AddSet(150m, 1);
        previous.Finish(); // finishes the set too

        var next = TrainingSession.Create(_anyUser);
        next.InheritFrom(previous);

        Assert.That(next.Exercises[0].SortedSets[0].IsCompleted, Is.False);
    }

    [Test]
    public void InheritFrom_OnNonEmptySession_ThrowsInvalidOperationException()
    {
        var previous = TrainingSession.Create(_anyUser);
        previous.AddExercise("Squat", null);
        previous.Finish();

        var next = TrainingSession.Create(_anyUser);
        next.AddExercise("Bench", null);

        Assert.Throws<InvalidOperationException>(() => next.InheritFrom(previous));
    }

    // ── ExerciseSet via ExerciseEntry ───────────────────────────────────────

    [Test]
    public void AddSet_WithWeightAndReps_AddsSetWithSetNumber1()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);

        exercise.AddSet(100m, 5);

        Assert.Multiple(() =>
        {
            Assert.That(exercise.SortedSets, Has.Count.EqualTo(1));
            Assert.That(exercise.SortedSets[0].SetNumber, Is.EqualTo(1));
            Assert.That(exercise.SortedSets[0].Weight, Is.EqualTo(100m));
            Assert.That(exercise.SortedSets[0].Repetitions, Is.EqualTo(5));
            Assert.That(exercise.SortedSets[0].IsCompleted, Is.False);
        });
    }

    [Test]
    public void AddSet_SecondSet_AutoIncrementsSetNumber()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);
        exercise.AddSet(100m, 5);
        exercise.AddSet(120m, 3);

        Assert.Multiple(() =>
        {
            Assert.That(exercise.SortedSets[0].SetNumber, Is.EqualTo(1));
            Assert.That(exercise.SortedSets[1].SetNumber, Is.EqualTo(2));
        });
    }

    [Test]
    public void AddSet_AfterDelete_SetNumberHasGap()
    {
        // Gaps are allowed — setNumber is not renumbered after delete
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);
        exercise.AddSet(100m, 5);
        exercise.AddSet(120m, 3);
        exercise.RemoveSet(exercise.SortedSets[0].Id); // remove set 1

        exercise.AddSet(140m, 1); // should be set 3, not 2

        Assert.That(exercise.SortedSets.Last().SetNumber, Is.EqualTo(3));
    }

    [Test]
    public void AddCopyOfLastSet_NoParams_CopiesWeightAndRepsFromLastSet()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Bench", null);
        exercise.AddSet(80m, 8);

        exercise.AddCopyOfLastSet(); // should copy from previous

        Assert.Multiple(() =>
        {
            Assert.That(exercise.SortedSets[1].Weight, Is.EqualTo(80m));
            Assert.That(exercise.SortedSets[1].Repetitions, Is.EqualTo(8));
            Assert.That(exercise.SortedSets[1].SetNumber, Is.EqualTo(2));
        });
    }

    [Test]
    public void AddCopyOfLastSet_OnEmptySets_ThrowsInvalidOperationException()
    {
        // AddSet() with no params on empty set list should throw — nothing to copy from
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Bench", null);

        Assert.Throws<InvalidOperationException>(exercise.AddCopyOfLastSet);
    }

    [Test]
    public void AddSet_OnFinishedSession_ThrowsInvalidOperationException()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);
        session.StartExercise(exercise.Id);
        session.Finish();

        Assert.Throws<InvalidOperationException>(() => session.AddSet(exercise.Id, 100m, 5));
    }

    [Test]
    public void CompleteSet_CompletesSet()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);
        exercise.AddSet(100m, 5);
        var setId = exercise.SortedSets[0].Id;

        exercise.CompleteSet(setId);

        Assert.That(exercise.SortedSets[0].IsCompleted, Is.True);
    }

    [Test]
    public void UnCompleteSet_UnCompletesSet()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);
        exercise.AddSet(100m, 5);
        var setId = exercise.SortedSets[0].Id;
        
        exercise.CompleteSet(setId);
        exercise.UnCompleteSet(setId);

        Assert.That(exercise.SortedSets[0].IsCompleted, Is.False);
    }

    [Test]
    public void UpdateSet_UpdatesWeightAndReps()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);
        exercise.AddSet(100m, 5);
        var setId = exercise.SortedSets[0].Id;

        exercise.UpdateSet(setId, weight: 120m, repetitions: 3);

        Assert.Multiple(() =>
        {
            Assert.That(exercise.SortedSets[0].Weight, Is.EqualTo(120m));
            Assert.That(exercise.SortedSets[0].Repetitions, Is.EqualTo(3));
        });
    }

    [Test]
    public void UpdateSet_OnFinishedSession_ThrowsInvalidOperationException()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);
        session.AddSet(exercise.Id, 100m, 5);
        var setId = exercise.SortedSets[0].Id;
        session.StartExercise(exercise.Id);
        session.Finish();

        Assert.Throws<InvalidOperationException>(() =>
            session.UpdateSet(exercise.Id, setId, weight: null, repetitions: null));
    }

    [Test]
    public void UpdateSet_WithUnknownSetId_ThrowsKeyNotFoundException()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);

        Assert.Throws<KeyNotFoundException>(() =>
            session.UpdateSet(exercise.Id, Guid.NewGuid(), weight: null, repetitions: null));
    }

    [Test]
    public void RemoveSet_RemovesSetFromList()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);
        session.AddSet(exercise.Id, 100m, 5);
        var setId = exercise.SortedSets[0].Id;

        session.RemoveSet(exercise.Id, setId);

        Assert.That(exercise.SortedSets, Is.Empty);
    }

    [Test]
    public void RemoveSet_OnFinishedExercise_Ok()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);
        session.AddSet(exercise.Id, 100m, 5);
        var setId = exercise.SortedSets[0].Id;
        session.StartExercise(exercise.Id);
        session.FinishExercise(exercise.Id);

        session.RemoveSet(exercise.Id, setId);
        Assert.That(exercise.SortedSets, Is.Empty);
    }

    [Test]
    public void RemoveSet_OnFinishedSession_ThrowsInvalidOperationException()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);
        session.AddSet(exercise.Id, 100m, 5);
        var setId = exercise.SortedSets[0].Id;
        session.StartExercise(exercise.Id);
        session.Finish();

        Assert.Throws<InvalidOperationException>(() => session.RemoveSet(exercise.Id, setId));
    }

    [Test]
    public void RemoveSet_WithUnknownSetId_ThrowsKeyNotFoundException()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);

        Assert.Throws<KeyNotFoundException>(() => session.RemoveSet(exercise.Id, Guid.NewGuid()));
    }

    [Test]
    public void FinishExercise_CompletesAllContainedSets()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Deadlift", null);
        exercise.AddSet(150m, 1);
        exercise.AddSet(150m, 1);
        session.StartExercise(exercise.Id);

        session.FinishExercise(exercise.Id);

        Assert.That(exercise.SortedSets.All(s => s.IsCompleted), Is.True);
    }
    // ── Bug exposure tests ──────────────────────────────────────────────────

    [Test]
    public void AddCopyOfLastSet_NoParams_OnEmptySets_ThrowsInvalidOperationException_WithMeaningfulMessage()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Bench", null);

        var ex = Assert.Throws<InvalidOperationException>(exercise.AddCopyOfLastSet);

        // Fails if no explicit guard — LINQ throws "Sequence contains no elements" not our message
        Assert.That(ex!.Message, Does.Contain("no sets"));
    }

    [Test]
    public void UpdateSet_NullWeight_OverwritesExistingWeight()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Squat", null);
        exercise.AddSet(100m, 5);
        var setId = exercise.SortedSets[0].Id;

        // Pass null weight — must clear the existing 100m value
        session.UpdateSet(exercise.Id, setId, weight: null, repetitions: null);

        Assert.That(exercise.SortedSets[0].Weight, Is.Null);
        Assert.That(exercise.SortedSets[0].Repetitions, Is.Null);
    }
}
