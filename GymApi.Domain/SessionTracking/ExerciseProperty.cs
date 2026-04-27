using Orleans;

namespace GymApi.Domain.SessionTracking;

/// <summary>Ad-hoc key/value pair attached to an exercise (e.g. "Weight"/"80kg", "Reps"/"12").</summary>
[GenerateSerializer]
[Alias("GymApi.Domain.SessionTracking.ExerciseProperty")]
public sealed record ExerciseProperty(
    [property: Id(0)] string Name,
    [property: Id(1)] string Value);
