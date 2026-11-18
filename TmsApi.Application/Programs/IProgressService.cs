namespace TmsApi.Application.Programs;

public interface IProgressService
{
    Task<IReadOnlyList<StudentProgramProgressDto>> GetStudentProgressAsync(int studentId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentProgramProgressDto>> GetProgramProgressAsync(int programId, CancellationToken ct = default);
    Task<StudentProgramProgressDto?> RecalculateProgressAsync(int studentProgramEnrollmentId, CancellationToken ct = default);
    Task<StudentProgramProgressDto?> CheckProgramCompletionAsync(int studentProgramEnrollmentId, CancellationToken ct = default);
}
