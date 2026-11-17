using TmsApi.Application.DTOs;

namespace TmsApi.Application.Interfaces;

public interface IGradebookService
{

    Task<CourseGradebookResponse> GetCourseGradebookAsync(int courseId, GradebookActor actor, CancellationToken ct);
    Task<IReadOnlyList<StudentGradeResponse>> GetStudentGradesAsync(int studentId, GradebookActor actor, CancellationToken ct);
    Task<IReadOnlyList<StudentGradeResponse>> GetMyGradesAsync(GradebookActor actor, CancellationToken ct);

    Task<GradeRecordResponse> SaveGradeAsync(SaveGradeRequest request, GradebookActor actor, CancellationToken ct);
    Task<GradeRecordResponse> UpdateGradeAsync(int gradeRecordId, UpdateGradeRequest request, GradebookActor actor, CancellationToken ct);
    Task PublishAssessmentGradeAsync(int gradeRecordId, GradebookActor actor, CancellationToken ct);
    Task<GradeRecordResponse> CorrectGradeAsync(int gradeRecordId, CorrectGradeRequest request, GradebookActor actor, CancellationToken ct);

    Task<decimal> PublishFinalGradeAsync(int enrollmentId, GradebookActor actor, CancellationToken ct);

    Task<int> SubmitAppealAsync(int enrollmentId, SubmitGradeAppealRequest request, GradebookActor actor, CancellationToken ct);
    Task<IReadOnlyList<GradeAppealResponse>> GetMyAppealsAsync(GradebookActor actor, CancellationToken ct);
    Task<IReadOnlyList<GradeAppealResponse>> GetAppealsAsync(string? status, int? courseId, GradebookActor actor, CancellationToken ct);
    Task ReviewAppealAsync(int appealId, ReviewGradeAppealRequest request, GradebookActor actor, CancellationToken ct);
}