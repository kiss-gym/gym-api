using System.ComponentModel.DataAnnotations;

namespace GymApi.Api.Models.Requests;

public sealed record RenameSessionRequest(
    [Required] string Label);
