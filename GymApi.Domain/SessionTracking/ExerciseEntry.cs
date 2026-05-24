namespace GymApi.Domain.SessionTracking;

/// <summary>
/// A single exercise within a training session.
/// Lifecycle: Pending → Running → Finished.
/// </summary>
public sealed class ExerciseEntry
{
    public Guid Id { get; private init; }
    public string AutoLabel { get; private set; } = "";
    public string? PhotoUrl { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? RealEndAt { get; private set; }
    public IReadOnlyList<ExerciseProperty> Properties { get; private set; } = [];

    private readonly List<ExerciseSet> _sets = [];
    public IReadOnlyList<ExerciseSet> SortedSets => _sets.OrderBy(s => s.SetNumber).ToList().AsReadOnly();

    public bool IsPending => StartedAt is null;
    public bool IsRunning => StartedAt is not null && RealEndAt is null;
    public bool IsFinished => RealEndAt is not null;

    private ExerciseEntry()
    {
    }

    public static ExerciseEntry CreatePending(
        string autoLabel,
        string? photoUrl,
        IEnumerable<ExerciseProperty>? properties = null)
    {
        return new ExerciseEntry
        {
            Id = Guid.NewGuid(),
            AutoLabel = autoLabel,
            PhotoUrl = photoUrl,
            Properties = properties?.ToList() ?? []
        };
    }

    internal void Start()
    {
        if (!IsPending)
        {
            throw new InvalidOperationException($"Exercise '{AutoLabel}' is not pending.");
        }

        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string? autoLabel, string? photoUrl, IEnumerable<ExerciseProperty>? properties)
    {
        EnsureExerciseIsNotFinished();

        if (autoLabel != null)
        {
            AutoLabel = autoLabel;
        }

        if (photoUrl != null)
        {
            PhotoUrl = photoUrl;
        }

        if (properties != null)
        {
            Properties = properties.ToList();
        }
    }

    public void AddSet(decimal? weight, int? repetitions)
    {
        EnsureExerciseIsNotFinished();

        var setNumber = _sets.Count == 0 ? 1 : _sets.Max(s => s.SetNumber) + 1;
        var newSet = ExerciseSet.Create(setNumber, weight, repetitions);
        _sets.Add(newSet);
    }
    
    public void AddCopyOfLastSet()
    {
        EnsureExerciseIsNotFinished();
        if (_sets.Count == 0)
        {
            throw new InvalidOperationException("Cannot copy from last set — no sets exist yet.");
        }

        var latestSet = _sets.OrderByDescending(s => s.SetNumber).First();
        var newSet = ExerciseSet.Create(latestSet.SetNumber + 1, latestSet.Weight, latestSet.Repetitions);
        _sets.Add(newSet);
    }

    public void UpdateSet(Guid setId, decimal? weight, int? repetitions)
    {
        EnsureExerciseIsNotFinished();

        var set = FindSet(setId);

        set.Update(weight, repetitions);
    }

    public void CompleteSet(Guid setId)
    {
        EnsureExerciseIsNotFinished();
        var set = FindSet(setId);
        set.Complete();
    }

    public void UnCompleteSet(Guid setId)
    {
        EnsureExerciseIsNotFinished();
        var set = FindSet(setId);
        set.UnComplete();
    }


    public void RemoveSet(Guid setId)
    {
        EnsureExerciseIsNotFinished();
        
        var set = FindSet(setId);
        _sets.Remove(set);
    }

    internal void Finish()
    {
        if (IsRunning)
        {
            RealEndAt = DateTimeOffset.UtcNow;
        }
        _sets.ForEach(s => s.Complete());
    }
    
    private void EnsureExerciseIsNotFinished()
    {
        if (IsFinished)
        {
            throw new InvalidOperationException("The exercise is already finished.");
        }
    }
    
    private ExerciseSet FindSet(Guid setId)  
    { 
        return _sets.FirstOrDefault(s => s.Id == setId)
               ?? throw new KeyNotFoundException($"Set with ID '{setId}' not found in exercise '{Id}' ('{AutoLabel}').");
    }

}
