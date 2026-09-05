using TmsApi.Application.DTOs;

namespace TmsApi.Application.DTOs; 

public record CourseDetailEnrollmentDto( 
    int Id, 
    string Code, 
    string Title, 
    string? Description, 
    int MaxCapacity, 
    int EnrollmentCount, 
    IReadOnlyList<EnrolledStudentDto> Enrollments);