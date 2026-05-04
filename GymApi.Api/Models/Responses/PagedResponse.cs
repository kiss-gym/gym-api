using System.ComponentModel.DataAnnotations;
namespace GymApi.Api.Models.Responses;

public record PagedResponse<T>(
    [Required] IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
