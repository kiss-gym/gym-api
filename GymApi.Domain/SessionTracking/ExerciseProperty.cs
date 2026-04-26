namespace GymApi.Domain.SessionTracking;

/// <summary>Ad-hoc key/value pair attached to an exercise (e.g. "Weight"/"80kg", "Reps"/"12").</summary>
public sealed record ExerciseProperty(string Name, string Value);
