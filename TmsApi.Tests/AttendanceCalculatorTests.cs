using TmsApi.Application.Attendance;
using TmsApi.Domain.Entities;
using Xunit;

namespace TmsApi_Tests;

/// <summary>Pins down the attendance policy written in AttendanceCalculator.</summary>
public class AttendanceCalculatorTests
{
    private static AttendanceSummary Run(params AttendanceStatus[] statuses) =>
        AttendanceCalculator.Summarize(statuses);

    [Fact]
    public void EightPresentOutOfTen_Is80Percent()
    {
        var statuses = Enumerable.Repeat(AttendanceStatus.Present, 8)
            .Concat(Enumerable.Repeat(AttendanceStatus.Absent, 2))
            .ToArray();

        var result = Run(statuses);

        Assert.Equal(80.0m, result.Percentage);
        Assert.Equal(10, result.Counted);
    }

    [Fact]
    public void Late_CountsAsAttended()
    {
        var result = Run(AttendanceStatus.Present, AttendanceStatus.Late, AttendanceStatus.Absent, AttendanceStatus.Absent);

        Assert.Equal(50.0m, result.Percentage);
        Assert.Equal(1, result.Late);
    }

    [Fact]
    public void Excused_IsNeutral_NotPresentAndNotAbsent()
    {
        var result = Run(AttendanceStatus.Present, AttendanceStatus.Excused);

        Assert.Equal(100.0m, result.Percentage);
        Assert.Equal(1, result.Counted);
        Assert.Equal(1, result.Excused);
    }

    [Fact]
    public void OnlyExcused_HasNoPercentage()
    {
        var result = Run(AttendanceStatus.Excused, AttendanceStatus.Excused);

        Assert.Null(result.Percentage);
        Assert.Equal(0, result.Counted);
    }

    [Fact]
    public void NoMarks_HasNoPercentage()
    {
        Assert.Null(Run().Percentage);
    }

    [Fact]
    public void Percentage_IsRoundedToOneDecimal()
    {
        var result = Run(AttendanceStatus.Present, AttendanceStatus.Present, AttendanceStatus.Absent);

        Assert.Equal(66.7m, result.Percentage);
    }
}
