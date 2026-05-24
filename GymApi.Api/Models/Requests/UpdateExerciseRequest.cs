using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GymApi.Api.Models.Requests;

public sealed class UpdateExerciseRequest
{
    public string? AutoLabel { get; init; }
    public string? PhotoUrl { get; init; }
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<ExercisePropertyDto>? Properties { get; init; }
}
