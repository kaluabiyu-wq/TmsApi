public interface IStudentService
{
    Task<StudentRecord> CreateAsync(string name, decimal gpa);
    Task<StudentRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<StudentRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
}