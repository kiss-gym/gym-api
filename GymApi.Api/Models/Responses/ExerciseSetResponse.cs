using GymApi.Domain.SessionTracking;

namespace GymApi.Api.Models.Responses;

public sealed record ExerciseSetResponse(
    Guid Id,
    int SetNumber,
    bool IsCompleted,
    decimal? Weight,
    int? Repetitions)
{
    public static ExerciseSetResponse From(ExerciseSet s) =>
        new(s.Id, s.SetNumber, s.IsCompleted, s.Weight, s.Repetitions);
}
