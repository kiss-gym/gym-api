namespace GymApi.Api.Models.Requests;

public sealed record UpdateSetRequest(
    decimal? Weight = null,
    int? Repetitions = null);
