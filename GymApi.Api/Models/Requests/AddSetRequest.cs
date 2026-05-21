namespace GymApi.Api.Models.Requests;

public sealed record AddSetRequest(
    decimal? Weight = null,
    int? Repetitions = null);
