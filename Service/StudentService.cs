
public interface IStudentService
{
    Task<StudentRecord> CreateAsync(string name, double? gpa);
    Task<StudentRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<StudentRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
}


public class StudentService : IStudentService
{
    private readonly Dictionary<string, StudentRecord> _store = new();
    private readonly ILogger<StudentService> _logger;

    public StudentService(ILogger<StudentService> logger)
    {
        _logger = logger;
    }

    public Task<StudentRecord> CreateAsync(string name, double? gpa)
    {
      
        var existing = _store.Values
            .FirstOrDefault(s => s.Name == name);

        if (existing is not null)
        {
            _logger.LogWarning(
                "Duplicate student {StudentName} already exists (record {StudentId})",
                name,
                existing.Id);

            return Task.FromResult(existing);
        }

        var id = Guid.NewGuid().ToString("N")[..8];

        var student = new StudentRecord(
            id,
            name,
            DateTime.UtcNow,
            gpa);

        _store[id] = student;

        _logger.LogInformation(
            "Created student {StudentName} with id {StudentId}",
            name,
            id);

        return Task.FromResult(student);
    }

    public Task<StudentRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var student);

        if (student is null)
        {
            _logger.LogWarning(
                "Student {StudentId} not found",
                id);
        }

        return Task.FromResult(student);
    }

    public Task<IReadOnlyList<StudentRecord>> GetAllAsync()
    {
        IReadOnlyList<StudentRecord> students = _store.Values.ToList();

        return Task.FromResult(students);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);

        if (removed)
        {
            _logger.LogInformation(
                "Deleted student {StudentId}",
                id);
        }
        else
        {
            _logger.LogWarning(
                "Delete failed. Student {StudentId} not found",
                id);
        }

        return Task.FromResult(removed);
    }
}


public record StudentRecord(string Id,string Name, DateTime EnrollmentDate, double? Gpa);