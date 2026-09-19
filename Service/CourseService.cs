

public interface ICourseService
{
    Task<CourseRecord> CreateAsync(string title, int capacity);
    Task<CourseRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<CourseRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
}

public class CourseService : ICourseService
{
    private readonly Dictionary<string, CourseRecord> _store = new();
    private readonly ILogger<CourseService> _logger;

    public CourseService(ILogger<CourseService> logger)
    {
        _logger = logger;
    }

    public Task<CourseRecord> CreateAsync(string title, int capacity)
    {
        var existing = _store.Values
            .FirstOrDefault(c => c.Title == title);

        if (existing is not null)
        {
            _logger.LogWarning(
                "Duplicate course {CourseTitle} already exists (record {CourseId})",
                title,
                existing.Id);

            return Task.FromResult(existing);
        }

        var id = Guid.NewGuid().ToString("N")[..8];

        var course = new CourseRecord(
            id,
            title,
            capacity,
            DateTime.UtcNow);

        _store[id] = course;

        _logger.LogInformation(
            "Created course {CourseTitle} with id {CourseId}",
            title,
            id);

        return Task.FromResult(course);
    }

    public Task<CourseRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var course);

        if (course is null)
        {
            _logger.LogWarning(
                "Course {CourseId} not found",
                id);
        }

        return Task.FromResult(course);
    }

    public Task<IReadOnlyList<CourseRecord>> GetAllAsync()
    {
        IReadOnlyList<CourseRecord> courses = _store.Values.ToList();
        return Task.FromResult(courses);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);

        if (removed)
        {
            _logger.LogInformation(
                "Deleted course {CourseId}",
                id);
        }
        else
        {
            _logger.LogWarning(
                "Delete failed. Course {CourseId} not found",
                id);
        }

        return Task.FromResult(removed);
    }
}


public record CourseRecord(string Id,string Title,int Capacity, DateTime CreatedAt);