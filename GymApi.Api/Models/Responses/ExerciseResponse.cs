using System.ComponentModel.DataAnnotations;
using GymApi.Domain.SessionTracking;

namespace GymApi.Api.Models.Responses;

public sealed record ExercisePropertyResponse([Required] string Name, [Required] string Value);

public sealed record ExerciseResponse(
    Guid Id,
    [Required] string AutoLabel,
    string? PhotoUrl,
    DateTimeOffset? StartedAt,
    DateTimeOffset? RealEndAt,
    [Required] string Status,
    [Required] IReadOnlyList<ExercisePropertyResponse> Properties,
    [Required] IReadOnlyList<ExerciseSetResponse> Sets)
{
    public static ExerciseResponse From(ExerciseEntry e) =>
        new(e.Id,
            e.AutoLabel,
            e.PhotoUrl,
            e.StartedAt,
            e.RealEndAt,
            e.IsPending ? "Pending" : e.IsRunning ? "Running" : "Finished",
            e.Properties.Select(p => new ExercisePropertyResponse(p.Name, p.Value)).ToList(),
            e.SortedSets.Select(ExerciseSetResponse.From).ToList());
}
