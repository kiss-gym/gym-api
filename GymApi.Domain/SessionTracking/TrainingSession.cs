namespace GymApi.Domain.SessionTracking;

/// <summary>
/// Aggregate root for a training session.
/// Session invariants: only one exercise runs at a time.
/// New exercises auto-finish the previous running one.
/// </summary>
public sealed class TrainingSession
{
    public Guid Id { get; private init; }
    public Guid UserId { get; private set; }
    public string? Label { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public SessionStatus Status { get; private set; }
    public Guid? InheritedFromSessionId { get; private set; }

    private readonly List<ExerciseEntry> _exercises = [];
    public IReadOnlyList<ExerciseEntry> Exercises => _exercises.AsReadOnly();

    private TrainingSession()
    {
    }

    /// <summary>Creates a new, empty active session.</summary>
    public static TrainingSession Create(Guid userId, Guid? sessionId = null, string? label = null)
    {
        return new TrainingSession
        {
            Id = sessionId ?? Guid.NewGuid(),
            UserId = userId,
            Label = label,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = SessionStatus.Active
        };
    }

    /// <summary>Populates the session with exercises from a parent session.</summary>
    public void InheritFrom(TrainingSession parentSession)
    {
        EnsureSessionIsActive();

        if (_exercises.Count != 0)
        {
            throw new InvalidOperationException("Can only inherit exercises into an empty session.");
        }

        InheritedFromSessionId = parentSession.Id;

        foreach (var ex in parentSession.Exercises)
        {
            var exerciseEntry = ExerciseEntry.CreatePending(ex.AutoLabel, ex.PhotoUrl, ex.Properties);
            foreach(var st in ex.SortedSets)
            {
                exerciseEntry.AddSet(st.Weight, st.Repetitions);
            }
            _exercises.Add(exerciseEntry);
        }
    }

    /// <summary>Adds a new exercise, doesn't starts it immediately, and auto-finishes any running exercise.</summary>
    public ExerciseEntry AddExercise(
        string autoLabel,
        string? photoUrl,
        IEnumerable<ExerciseProperty>? properties = null)
    {
        EnsureSessionIsActive();
        AutoFinishRunningExercise();

        var exercise = ExerciseEntry.CreatePending(autoLabel, photoUrl, properties);
        _exercises.Add(exercise);
        return exercise;
    }

    /// <summary>Starts a pending exercise, auto-finishing any running exercise.</summary>
    public ExerciseEntry StartExercise(Guid exerciseId)
    {
        EnsureSessionIsActive();
        var exercise = FindExercise(exerciseId);

        if (!exercise.IsPending)
        {
            throw new InvalidOperationException($"Exercise {exerciseId} is not pending.");
        }

        AutoFinishRunningExercise();
        exercise.Start();
        return exercise;
    }

    /// <summary>Finishes a specific running exercise.</summary>
    public void FinishExercise(Guid exerciseId)
    {
        EnsureSessionIsActive();
        var exercise = FindExercise(exerciseId);

        if (!exercise.IsRunning)
        {
            throw new InvalidOperationException($"Exercise {exerciseId} is not running.");
        }

        exercise.Finish();
    }

    public void RemoveExercise(Guid exerciseId)
    {
        EnsureSessionIsActive();
        _exercises.Remove(FindExercise(exerciseId));
    }

    public void Rename(string label)
    {
        EnsureSessionIsActive();
        Label = label;
    }

    /// <summary>Finishes the session. Auto-finishes any running exercise first.</summary>
    public void Finish()
    {
        EnsureSessionIsActive();
        AutoFinishRunningExercise();
        Status = SessionStatus.Finished;
        FinishedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureSessionIsActive()
    {
        if (Status != SessionStatus.Active)
        {
            throw new InvalidOperationException("Session is already finished.");
        }
    }

    private void AutoFinishRunningExercise()
    {
        _exercises.FirstOrDefault(e => e.IsRunning)?.Finish();
    }

    private ExerciseEntry FindExercise(Guid exerciseId)
    {
        return _exercises.FirstOrDefault(e => e.Id == exerciseId)
               ?? throw new KeyNotFoundException($"Exercise {exerciseId} not found in session {Id}.");
    }
}
