namespace GymApi.Api.Models.Requests;

public sealed record StartExerciseRequest(DateTimeOffset? MaxEndAt = null);
