using TmsApi.Application.Grading;
using Xunit;

public class GradingServiceTests
{
    private readonly GradingService service = new();

    [Theory]
    [InlineData(80, 100, 20, 16)]
    [InlineData(70, 100, 30, 21)]
    [InlineData(90, 100, 50, 45)]
    [InlineData(45, 50, 20, 18)]   // non-100 max score
    public void CalculatesWeightedScore(
        decimal score, decimal max, decimal weight, decimal expected)
        => Assert.Equal(expected, service.CalculateWeightedScore(score, max, weight));

    [Fact]
    public void CalculatesFinalCourseGrade()
    {
        var result = service.CalculateFinalGrade(
        [
            (80m, 100m, 20m),
            (70m, 100m, 30m),
            (90m, 100m, 50m)
        ]);

        Assert.Equal(82m, result);
    }

    [Fact]
    public void FinalGradeDoesNotAccumulateRoundingError()
    {
        // 3 equal assessments at ~33.33% weight; 100/100 each must not produce 99.99.
        var result = service.CalculateFinalGrade(
        [
            (100m, 100m, 100m / 3m),
            (100m, 100m, 100m / 3m),
            (100m, 100m, 100m / 3m)
        ]);

        Assert.Equal(100m, result);
    }

    [Theory]
    [InlineData(120, 100, 20)]   // score above max
    [InlineData(-1, 100, 20)]    // negative score
    [InlineData(80, 0, 20)]      // zero max
    [InlineData(80, 100, 120)]   // weight above 100
    [InlineData(80, 100, -5)]    // negative weight
    public void RejectsInvalidInputs(decimal score, decimal max, decimal weight)
        => Assert.Throws<ArgumentException>(
            () => service.CalculateWeightedScore(score, max, weight));

    [Fact]
    public void ValidatesWeightsEqualOneHundred()
    {
        service.ValidateWeights([20m, 30m, 50m]); // does not throw

        Assert.Throws<InvalidOperationException>(
            () => service.ValidateWeights([20m, 30m, 40m]));
    }

    [Theory]
    [InlineData(80, GradeLevel.Distinction)]
    [InlineData(70, GradeLevel.Distinction)]
    [InlineData(69.99, GradeLevel.Pass)]
    [InlineData(50, GradeLevel.Pass)]
    [InlineData(49.99, GradeLevel.Fail)]
    public void CalculatesGradeLevel(double score, GradeLevel expected)
        => Assert.Equal(expected, service.CalculateLetterGrade((decimal)score, 100m));

    [Fact]
    public void NullEnrollmentGradeIsInvalid()
        => Assert.Equal(GradeLevel.Invalid, service.CalculateFromEnrollmentGrade(null));
}