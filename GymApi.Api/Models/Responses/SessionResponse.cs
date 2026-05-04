using System.ComponentModel.DataAnnotations;
using GymApi.Domain.SessionTracking;

namespace GymApi.Api.Models.Responses;

public sealed record SessionResponse(
    Guid Id,
    Guid UserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FinishedAt,
    [Required] string Status,
    string? Label,
    Guid? InheritedFromSessionId,
    [Required] IReadOnlyList<ExerciseResponse> Exercises)
{
    public static SessionResponse From(TrainingSession s)
    {
        return new SessionResponse(
            s.Id,
            s.UserId,
            s.CreatedAt,
            s.FinishedAt,
            s.Status.ToString(),
            s.Label,
            s.InheritedFromSessionId,
            s.Exercises.Select(ExerciseResponse.From).ToList());
    }
}
