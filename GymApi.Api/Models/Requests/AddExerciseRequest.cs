using System.ComponentModel.DataAnnotations;

namespace GymApi.Api.Models.Requests;

public sealed record ExercisePropertyDto(
    [Required] string Name,
    [Required] string Value);

public sealed record AddExerciseRequest(
    [Required] string AutoLabel,
    string? PhotoUrl = null,
    DateTimeOffset? MaxEndAt = null,
    IReadOnlyList<ExercisePropertyDto>? Properties = null);