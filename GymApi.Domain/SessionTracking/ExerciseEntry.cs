using Orleans;

namespace GymApi.Domain.SessionTracking;

/// <summary>
/// A single exercise within a training session.
/// Lifecycle: Pending → Running → Finished.
/// Pending state only exists for exercises inherited from a previous session.
/// </summary>
[GenerateSerializer]
[Alias("GymApi.Domain.SessionTracking.ExerciseEntry")]
public sealed class ExerciseEntry
{
    [Id(0)] public Guid Id { get; private set; }
    [Id(1)] public string AutoLabel { get; private init; } = "";
    [Id(2)] public string? PhotoUrl { get; private set; }
    [Id(3)] public DateTimeOffset? StartedAt { get; private set; }
    [Id(4)] public DateTimeOffset? MaxEndAt { get; private set; }
    [Id(5)] public DateTimeOffset? RealEndAt { get; private set; }
    [Id(6)] public IReadOnlyList<ExerciseProperty> Properties { get; private set; } = [];

    public bool IsPending => StartedAt is null;
    public bool IsRunning => StartedAt is not null && RealEndAt is null;
    public bool IsFinished => RealEndAt is not null;

    private ExerciseEntry()
    {
    }

    // Changed from internal to public to be accessible by test projects
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

    internal void Start(DateTimeOffset? maxEndAt = null)
    {
        if (!IsPending)
        {
            throw new InvalidOperationException($"Exercise '{AutoLabel}' is not pending.");
        }

        StartedAt = DateTimeOffset.UtcNow;
        MaxEndAt = maxEndAt;
    }

    internal void Finish()
    {
        if (IsRunning)
        {
            RealEndAt = DateTimeOffset.UtcNow;
        }
    }
}
