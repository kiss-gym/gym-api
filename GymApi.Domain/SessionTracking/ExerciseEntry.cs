namespace GymApi.Domain.SessionTracking;

/// <summary>
/// A single exercise within a training session.
/// Lifecycle: Pending → Running → Finished.
/// Pending state only exists for exercises inherited from a previous session.
/// </summary>
public sealed class ExerciseEntry
{
    public Guid Id { get; private set; }
    public string AutoLabel { get; private set; } = "";
    public string? PhotoUrl { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? MaxEndAt { get; private set; }
    public DateTimeOffset? RealEndAt { get; private set; }
    public IReadOnlyList<ExerciseProperty> Properties { get; private set; } = [];

    public bool IsPending => StartedAt is null;
    public bool IsRunning => StartedAt is not null && RealEndAt is null;
    public bool IsFinished => RealEndAt is not null;

    private ExerciseEntry()
    {
    }

    internal static ExerciseEntry CreatePending(
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