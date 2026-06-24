

public interface ICourseService
{
    Task<CourseRecord> CreateAsync(string title, int capacity);
    Task<CourseRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<CourseRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
}