using TmsApi.Domain.Entities;

namespace TmsApi.Application.Attendance;

/// <summary>
/// The one place that turns a list of marks into a percentage.
/// It is a pure static function (no database, no clock) so the policy is easy to read and to unit test.
///
/// POLICY (change it here and nowhere else):
///   * Present and Late count as attended (Late is still participation).
///   * Absent counts against the student.
///   * Excused is neutral: it is removed from the denominator, so an excused absence
///     neither raises nor lowers the percentage. It is NOT silently converted to Present.
///   * Sessions with no mark yet are ignored, because "not recorded" is not "absent".
/// </summary>
public static class AttendanceCalculator
{
    public static AttendanceSummary Summarize(IEnumerable<AttendanceStatus> statuses)
    {
        int present = 0, late = 0, absent = 0, excused = 0;

        foreach (var status in statuses)
        {
            switch (status)
            {
                case AttendanceStatus.Present: present++; break;
                case AttendanceStatus.Late: late++; break;
                case AttendanceStatus.Absent: absent++; break;
                case AttendanceStatus.Excused: excused++; break;
            }
        }

        var counted = present + late + absent;
        decimal? percentage = counted == 0
            ? null
            : Math.Round((present + late) * 100m / counted, 1, MidpointRounding.AwayFromZero);

        return new AttendanceSummary(present, late, absent, excused, counted, percentage);
    }
}
