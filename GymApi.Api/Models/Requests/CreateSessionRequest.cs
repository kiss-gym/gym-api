using System.ComponentModel.DataAnnotations;

namespace GymApi.Api.Models.Requests;

public sealed record CreateSessionRequest(
    [Required] Guid UserId,
    string? Label = null,
    Guid? InheritFromSessionId = null);
