using System;
using System.Collections.Generic;
using System.Linq;

namespace GymApi.Domain.SessionTracking;

/// <summary>
/// A single exercise within a training session.
/// Lifecycle: Pending → Running → Finished.
/// </summary>
public sealed class ExerciseEntry
{
    public Guid Id { get; private set; }
    public string AutoLabel { get; private init; } = "";
    public string? PhotoUrl { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? RealEndAt { get; private set; }
    public IReadOnlyList<ExerciseProperty> Properties { get; private set; } = [];

    private readonly List<ExerciseSet> _sets = [];
    public IReadOnlyList<ExerciseSet> Sets => _sets.AsReadOnly();

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

    public void UpdateSet(Guid setId, bool? isFinished, decimal? weight, int? repetitions)
    {
        EnsureExerciseIsNotFinished();

        var set = _sets.FirstOrDefault(s => s.Id == setId);
        if (set == null)
        {
            throw new ArgumentException($"Set with ID '{setId}' not found.", nameof(setId));
        }

        set.Update(weight, repetitions);

        if (isFinished == true)
        {
            set.Finish();
        }
        else if (isFinished == false)
        {
            set.UnFinish();
        }
    }

    public void RemoveSet(Guid setId)
    {
        EnsureExerciseIsNotFinished();
        
        var set = _sets.FirstOrDefault(s => s.Id == setId);
        if (set == null)
        {
            throw new ArgumentException($"Set with ID '{setId}' not found.", nameof(setId));
        }

        _sets.Remove(set);
    }

    internal void Finish()
    {
        if (IsRunning)
        {
            RealEndAt = DateTimeOffset.UtcNow;
        }
        _sets.ForEach(s => s.Finish());
    }
    
    private void EnsureExerciseIsNotFinished()
    {
        if (IsFinished)
        {
            throw new InvalidOperationException("The exercise is already finished.");
        }
    }
}
