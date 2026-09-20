using System.ComponentModel.DataAnnotations;

namespace Tms.Api.Dtos;

public record CreateAssessmentRequest
{
    [Required, MaxLength(200)]
    public required string Title { get; init; }

    [Range(0, 100, ErrorMessage = "Weight must be between 0 and 100.")]
    public decimal Weight { get; init; }

    [Range(0.01, 1000, ErrorMessage = "MaxScore must be a positive value.")]
    public decimal MaxScore { get; init; }
}