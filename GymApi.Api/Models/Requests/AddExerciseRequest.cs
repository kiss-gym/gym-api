using System.ComponentModel.DataAnnotations;

namespace GymApi.Api.Models.Requests;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed record ExercisePropertyDto(
    [Required] string Name,
    [Required] string Value);

public sealed record AddExerciseRequest(
    [Required] string AutoLabel,
    string? PhotoUrl = null,
    DateTimeOffset? MaxEndAt = null,
    IReadOnlyList<ExercisePropertyDto>? Properties = null);
