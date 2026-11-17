using System.ComponentModel.DataAnnotations;
using TmsApi.Application.DTOs;

namespace TmsApi.Application.Attendance;

// Naming standard: Create{Entity}Request / Update{Entity}Request / {Entity}Response.
// Request DTOs never carry Id, RecordedBy, RecordedAt or other server-owned fields.

/// <summary>The four values a record may hold. Used by validation attributes on the request DTOs.</summary>
internal static class AttendanceStatusNames
{
    public const string Pattern = "^(?i)(Present|Absent|Late|Excused)$";
    public const string Message = "Status must be Present, Absent, Late or Excused.";
}

public record CreateSessionRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "CourseId is required.")]
    public int CourseId { get; init; }

    [Required, MaxLength(200)]
    public required string Topic { get; init; }

    public DateTime StartAt { get; init; }

    public DateTime EndAt { get; init; }
}

public record SessionResponse(
    int Id,
    int CourseId,
    string CourseCode,
    string Topic,
    DateTime StartAt,
    DateTime EndAt,
    string InstructorId,
    int EnrolledCount,
    int RecordedCount);

public record AttendanceEntryRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "StudentId is required.")]
    public int StudentId { get; init; }

    [Required, RegularExpression(AttendanceStatusNames.Pattern, ErrorMessage = AttendanceStatusNames.Message)]
    public required string Status { get; init; }

    [MaxLength(500)]
    public string? Remarks { get; init; }

    public DateTime? CheckInAt { get; init; }
}

public record MarkAttendanceRequest
{
    [Required, MinLength(1, ErrorMessage = "Send at least one attendance entry.")]
    [MaxLength(500, ErrorMessage = "A session roster cannot exceed 500 entries.")]
    public required List<AttendanceEntryRequest> Entries { get; init; }
}

public record UpdateAttendanceRequest
{
    [Required, RegularExpression(AttendanceStatusNames.Pattern, ErrorMessage = AttendanceStatusNames.Message)]
    public required string Status { get; init; }

    [MaxLength(500)]
    public string? Remarks { get; init; }

    public DateTime? CheckInAt { get; init; }
}

public record AttendanceRecordResponse(
    int Id,
    int SessionId,
    int StudentId,
    string Status,
    DateTime? CheckInAt,
    string? Remarks,
    string RecordedBy,
    DateTime RecordedAt);

/// <summary>
/// One line of the attendance screen: an enrolled student plus the current mark, if any.
/// RecordId / Status are null until the instructor has saved this student for this session.
/// CoursePercentage is the student's running attendance for the whole course (null until at least one counted mark exists).
/// </summary>
public record SessionRosterRow(
    int StudentId,
    string RegistrationNumber,
    string StudentName,
    int? RecordId,
    string? Status,
    DateTime? CheckInAt,
    string? Remarks,
    decimal? CoursePercentage);

public record SessionAttendanceResponse(
    SessionResponse Session,
    IReadOnlyList<SessionRosterRow> Rows);

/// <summary>
/// Counts and percentage for one student (or one course-wide total).
/// Percentage = (Present + Late) / (Present + Late + Absent) x 100, rounded to 1 decimal.
/// Excused marks are excluded from the denominator; unmarked sessions are not counted at all.
/// Percentage is null when nothing countable exists yet (avoids a misleading 0% or a divide-by-zero).
/// </summary>
public record AttendanceSummary(
    int Present,
    int Late,
    int Absent,
    int Excused,
    int Counted,
    decimal? Percentage);

public record StudentAttendanceItem(
    int RecordId,
    int SessionId,
    int CourseId,
    string CourseCode,
    string Topic,
    DateTime SessionStartAt,
    string Status,
    string? Remarks);

public record StudentAttendanceResponse(
    int StudentId,
    string RegistrationNumber,
    string StudentName,
    AttendanceSummary Summary,
    PagedResponse<StudentAttendanceItem> Records);

public record StudentSummaryRow(
    int StudentId,
    string RegistrationNumber,
    string StudentName,
    AttendanceSummary Summary);

public record CourseAttendanceSummaryResponse(
    int CourseId,
    string CourseCode,
    string CourseTitle,
    int SessionsHeld,
    AttendanceSummary Overall,
    IReadOnlyList<StudentSummaryRow> Students);
