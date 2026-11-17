namespace TmsApi.Domain.Entities;

/// <summary>
/// What happened to one student in one training session.
/// Stored as text in the database (HasConversion&lt;string&gt;), so the column stays readable
/// in pgAdmin and reordering this enum can never silently change existing rows.
/// </summary>
public enum AttendanceStatus
{
    Present,
    Absent,
    Late,
    Excused
}
