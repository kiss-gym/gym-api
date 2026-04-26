using System.ComponentModel.DataAnnotations;

namespace GymApi.Api.Models.Requests;

public sealed record RegisterRequest(
    [Required] [EmailAddress] string Email,
    [Required] string Name);
