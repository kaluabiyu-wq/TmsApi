namespace TmsApi.Domain.Entities;
public class AssessmentQuestion
{
    public int Id { get; set; }
    public int AssessmentId { get; set; }
    public Assessment Assessment { get; set; } = null!;
    public required string Prompt { get; set; }
    public string QuestionType { get; set; } = "MultipleChoice";
    // JSON array of choice strings. CorrectAnswer is never serialized to student DTOs.
    public string? OptionsJson { get; set; }
    public string? CorrectAnswer { get; set; }
    public decimal Points { get; set; }
    public int SortOrder { get; set; }
}
