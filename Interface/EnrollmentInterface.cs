public interface IEnrollmentService
{
    Task<EnrollmentRecord> EnrollAsync(string studentId,string courseCode);

    Task<EnrollmentRecord?> GetByIdAsync(string id);
    Task<bool> DeleteAsync(string id);
     Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync();
}

