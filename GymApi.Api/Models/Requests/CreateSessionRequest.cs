namespace GymApi.Api.Models.Requests;

public sealed record CreateSessionRequest(
    string? Label = null,
    Guid? InheritFromSessionId = null);
