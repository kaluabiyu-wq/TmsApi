namespace TmsApi.Application.DTOs;

public record AssessmentResponseDto(
    int Id,
    string Title,
    decimal Weight,
    decimal MaxScore,
    int CourseId);