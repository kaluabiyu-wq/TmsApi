public interface IAssessmentService
{
    Task<AssessmentRecord> CreateAsync(string title,string kind,double score);

    Task<AssessmentRecord?> GetByIdAsync(string id);

    Task<IReadOnlyList<AssessmentRecord>> GetAllAsync();

    Task<bool> DeleteAsync(string id);
}
