namespace Tms.Api.Dtos;

public record AssessmentResponseDto(
    int Id,
    string Title,
    decimal Weight,
    decimal MaxScore,
    int CourseId);