using System.ComponentModel.DataAnnotations;
using GymApi.Domain.SessionTracking;

namespace GymApi.Api.Models.Responses;

public sealed record ExercisePropertyResponse(string Name, string Value);

public sealed record ExerciseResponse(
    Guid Id,
    string AutoLabel,
    string? PhotoUrl,
    DateTimeOffset? StartedAt,
    DateTimeOffset? MaxEndAt,
    DateTimeOffset? RealEndAt,
    string Status,
    [Required] IReadOnlyList<ExercisePropertyResponse> Properties)
{
    public static ExerciseResponse From(ExerciseEntry e)
    {
        return new ExerciseResponse(
            e.Id,
            e.AutoLabel,
            e.PhotoUrl,
            e.StartedAt,
            e.MaxEndAt,
            e.RealEndAt,
            e.IsPending ? "Pending" : e.IsRunning ? "Running" : "Finished",
            e.Properties.Select(p => new ExercisePropertyResponse(p.Name, p.Value)).ToList());
    }
}
