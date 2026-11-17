namespace TmsApi.Application.Grading;

public class GradingService
{
    public const decimal DistinctionThreshold = 70m;
    public const decimal PassThreshold = 50m;

    public GradeLevel CalculateLetterGrade(decimal score, decimal maxScore)
    {
        if (maxScore <= 0m || score < 0m || score > maxScore)
            return GradeLevel.Invalid;

        var percentage = score / maxScore * 100m;

        return percentage >= DistinctionThreshold ? GradeLevel.Distinction
             : percentage >= PassThreshold ? GradeLevel.Pass
             : GradeLevel.Fail;
    }

    public GradeLevel CalculateFromEnrollmentGrade(decimal? enrollmentGradePercent)
        => enrollmentGradePercent is null
            ? GradeLevel.Invalid
            : CalculateLetterGrade(enrollmentGradePercent.Value, 100m);

    public decimal CalculateWeightedScore(decimal score, decimal maxScore, decimal weight)
        => Math.Round(
            Unrounded(score, maxScore, weight),
            2, MidpointRounding.AwayFromZero);

    // Final grade is computed from UNROUNDED contributions and rounded once,
    // so per-assessment rounding never accumulates.
    public decimal CalculateFinalGrade(
        IEnumerable<(decimal Score, decimal MaxScore, decimal Weight)> items)
        => Math.Round(
            items.Sum(i => Unrounded(i.Score, i.MaxScore, i.Weight)),
            2, MidpointRounding.AwayFromZero);

    public void ValidateWeights(IEnumerable<decimal> weights)
    {
        var total = weights.Sum();
        if (total != 100m)
            throw new InvalidOperationException(
                $"Assessment weights must total 100%. Current total: {total}%.");
    }

    private static decimal Unrounded(decimal score, decimal maxScore, decimal weight)
    {
        if (maxScore <= 0m)
            throw new ArgumentException("Maximum score must be greater than zero.");
        if (score < 0m || score > maxScore)
            throw new ArgumentException("Score must be between zero and the maximum score.");
        if (weight < 0m || weight > 100m)
            throw new ArgumentException("Weight must be between zero and 100.");

        return score / maxScore * 100m * (weight / 100m);
    }
}