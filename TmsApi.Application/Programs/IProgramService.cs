namespace TmsApi.Application.Programs;

public interface IProgramService
{
    Task<TrainingProgramDto> CreateProgramAsync(CreateProgramRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TrainingProgramDto>> GetProgramsAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<TrainingProgramDto?> AddCourseToProgramAsync(int programId, AddProgramCourseRequest request, CancellationToken ct = default);
    Task<StudentProgramProgressDto> RegisterStudentAsync(int programId, int studentId, CancellationToken ct = default);
}
