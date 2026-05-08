using GymApi.Domain.SessionTracking;
using NUnit.Framework;

namespace GymApi.Tests.SessionTracking;

[TestFixture]
public sealed class TrainingSessionTests
{
    private static readonly Guid _anyUser = Guid.NewGuid();

    [Test]
    public void AddExercise_AutoFinishesPreviousRunningExercise()
    {
        var session = TrainingSession.Create(_anyUser);
        session.AddExercise("Push-up", null);
        session.AddExercise("Pull-up", null);

        Assert.Multiple(() =>
        {
            Assert.That(session.Exercises[0].IsFinished, Is.True);
            Assert.That(session.Exercises[1].IsRunning, Is.True);
        });
    }

    [Test]
    public void AddExercise_OnFinishedSession_ThrowsInvalidOperationException()
    {
        var session = TrainingSession.Create(_anyUser);
        session.Finish();

        Assert.Throws<InvalidOperationException>(() => session.AddExercise("Squat", null));
    }

    [Test]
    public void CreateSession_SetsStatusStartedAndRecordsCreatedAtTimestamp()
    {
        
        var session = TrainingSession.Create(_anyUser);

        Assert.Multiple(() =>
        {
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Active));
            Assert.That(session.CreatedAt, Is.Not.Null);
        });
    }


    [Test]
    public void FinishSession_SetsStatusFinishedAndRecordsFinishAtTimestamp()
    {
        var session = TrainingSession.Create(_anyUser);
        session.Finish();

        Assert.Multiple(() =>
        {
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Finished));
            Assert.That(session.CreatedAt, Is.Not.Null);
            Assert.That(session.FinishedAt, Is.Not.Null);
        });
    }

    [Test]
    public void Finish_AutoFinishesRunningExercise()
    {
        var session = TrainingSession.Create(_anyUser);
        session.AddExercise("Deadlift", null);
        session.Finish();

        Assert.That(session.Exercises[0].IsFinished, Is.True);
    }

    [Test]
    public void Finish_OnAlreadyFinishedSession_ThrowsInvalidOperationException()
    {
        var session = TrainingSession.Create(_anyUser);
        session.Finish();

        Assert.Throws<InvalidOperationException>(session.Finish);
    }

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
    public void StartExercise_StartsPendingInheritedExercise_AutoFinishesPrevious()
    {
        var previous = TrainingSession.Create(_anyUser);
        previous.AddExercise("OHP", null);
        previous.AddExercise("Row", null);
        previous.Finish();

        var session = TrainingSession.Create(_anyUser);
        session.InheritFrom(previous);

        var first = session.Exercises[0];
        var second = session.Exercises[1];

        session.StartExercise(first.Id);
        session.StartExercise(second.Id); // should auto-finish first

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

        Assert.Throws<InvalidOperationException>(() => session.StartExercise(exercise.Id));
    }

    [Test]
    public void FinishExercise_FinishesRunningExercise()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Push-up", null);

        session.FinishExercise(exercise.Id);

        Assert.That(exercise.IsFinished, Is.True);
    }

    [Test]
    public void FinishExercise_OnNotRunningExercise_ThrowsInvalidOperationException()
    {
        var session = TrainingSession.Create(_anyUser);
        var exercise = session.AddExercise("Push-up", null);
        session.FinishExercise(exercise.Id);

        Assert.Throws<InvalidOperationException>(() => session.FinishExercise(exercise.Id));
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
}
