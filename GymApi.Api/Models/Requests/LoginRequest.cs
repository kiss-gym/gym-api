using System.ComponentModel.DataAnnotations;

namespace GymApi.Api.Models.Requests;

public sealed record LoginRequest(
    [Required] [EmailAddress] string Email);
