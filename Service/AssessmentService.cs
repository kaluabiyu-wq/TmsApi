public interface IAssessmentService
{
    Task<AssessmentRecord> CreateAsync(string title,string kind,double score);

    Task<AssessmentRecord?> GetByIdAsync(string id);

    Task<IReadOnlyList<AssessmentRecord>> GetAllAsync();

    Task<bool> DeleteAsync(string id);
}


public class AssessmentService : IAssessmentService
{
    private readonly Dictionary<string, AssessmentRecord> _store = new();
    private readonly ILogger<AssessmentService> _logger;

    public AssessmentService(ILogger<AssessmentService> logger)
    {
        _logger = logger;
    }

    public Task<AssessmentRecord> CreateAsync(
        string title,
        string kind,
        double score)
    {
        var existing = _store.Values
            .FirstOrDefault(a =>
                a.Title == title &&
                a.Kind == kind);

        if (existing is not null)
        {
            _logger.LogWarning(
                "Duplicate assessment {Title} ({Kind}) already exists (record {AssessmentId})",
                title,
                kind,
                existing.Id);

            return Task.FromResult(existing);
        }

        var id = Guid.NewGuid().ToString("N")[..8];

        var assessment = new AssessmentRecord(
            id,
            title,
            kind,
            score,
            DateTime.UtcNow);

        _store[id] = assessment;

        _logger.LogInformation(
            "Created assessment {Title} ({Kind}) record {AssessmentId}",
            title,
            kind,
            id);

        return Task.FromResult(assessment);
    }

    public Task<AssessmentRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var assessment);

        if (assessment is null)
        {
            _logger.LogWarning(
                "Assessment {AssessmentId} not found",
                id);
        }

        return Task.FromResult(assessment);
    }

    public Task<IReadOnlyList<AssessmentRecord>> GetAllAsync()
    {
        IReadOnlyList<AssessmentRecord> all = _store.Values.ToList();

        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);

        if (removed)
        {
            _logger.LogInformation(
                "Deleted assessment {AssessmentId}",
                id);
        }
        else
        {
            _logger.LogWarning(
                "Delete failed. Assessment {AssessmentId} not found",
                id);
        }

        return Task.FromResult(removed);
    }
}


public record AssessmentRecord(string Id,string Title,string Kind,double Score,DateTime CreatedAt);