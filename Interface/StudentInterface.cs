using TmsApi.Services;

public interface IStudentService
{
    Task<StudentRecord> CreateAsync(string name, decimal gpa, CancellationToken ct);
    Task<StudentRecord?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<StudentRecord>> GetAllAsync(CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}