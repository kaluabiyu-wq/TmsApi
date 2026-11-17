using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Attendance;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Services;

public class AttendanceService(TmsDbContext context, ILogger<AttendanceService> logger) : IAttendanceService
{
    // Enrollment.Status is a free string in the existing model; "Approved" is the only value
    // that means "this student really sits in the class" (EnrollmentService uses the same literal).
    private const string ApprovedStatus = "Approved";

    private static readonly TimeSpan MaxSessionLength = TimeSpan.FromHours(24);

    // ---------------------------------------------------------------- sessions

    public async Task<SessionResponse?> CreateSessionAsync(
        AttendanceActor actor, CreateSessionRequest request, CancellationToken ct)
    {
        var course = await context.Courses.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);
        if (course is null)
        {
            return null;
        }

        EnsureCanManage(actor, course);

        var startAt = ToUtc(request.StartAt);
        var endAt = ToUtc(request.EndAt);

        if (endAt <= startAt)
        {
            throw new InvalidOperationException("Session end must be after session start.");
        }

        if (endAt - startAt > MaxSessionLength)
        {
            throw new InvalidOperationException("A session cannot be longer than 24 hours.");
        }

        // Two sessions of the same course cannot overlap in time.
        // Intervals [a,b) and [c,d) overlap exactly when a < d and c < b.
        var overlaps = await context.TrainingSessions.AnyAsync(
            s => s.CourseId == course.Id && s.StartAt < endAt && s.EndAt > startAt, ct);
        if (overlaps)
        {
            logger.LogWarning("Rejected overlapping session for course {CourseId}", course.Id);
            throw new InvalidOperationException("This course already has a session that overlaps that time.");
        }

        var session = new TrainingSession
        {
            CourseId = course.Id,
            Topic = request.Topic.Trim(),
            StartAt = startAt,
            EndAt = endAt,
            // The session belongs to the course's instructor, even when an admin creates it.
            InstructorId = string.IsNullOrEmpty(course.InstructorId) ? actor.UserId : course.InstructorId,
            CreatedAt = DateTime.UtcNow
        };

        context.TrainingSessions.Add(session);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created training session {SessionId} for course {CourseId}", session.Id, course.Id);

        var enrolled = await ActiveEnrollments(course.Id).CountAsync(ct);
        return ToSessionResponse(session, course.Code, enrolled, recorded: 0);
    }

    public async Task<PagedResponse<SessionResponse>?> GetCourseSessionsAsync(
        AttendanceActor actor, int courseId, int page, int pageSize, CancellationToken ct)
    {
        var course = await context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
        {
            return null;
        }

        EnsureCanManage(actor, course);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = context.TrainingSessions.AsNoTracking().Where(s => s.CourseId == courseId);

        var totalCount = await query.CountAsync(ct);
        var enrolled = await ActiveEnrollments(courseId).CountAsync(ct);

        var rows = await query
            .OrderByDescending(s => s.StartAt)
            .ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.Topic,
                s.StartAt,
                s.EndAt,
                s.InstructorId,
                Recorded = s.AttendanceRecords.Count()
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new SessionResponse(
                r.Id, courseId, course.Code, r.Topic, r.StartAt, r.EndAt, r.InstructorId, enrolled, r.Recorded))
            .ToList();

        return new PagedResponse<SessionResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    // -------------------------------------------------------------- attendance

    public async Task<SessionAttendanceResponse?> GetSessionAttendanceAsync(
        AttendanceActor actor, int sessionId, CancellationToken ct)
    {
        var session = await context.TrainingSessions.AsNoTracking()
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is null)
        {
            return null;
        }

        EnsureCanManage(actor, session.Course);
        return await BuildRosterAsync(session, ct);
    }

    public async Task<SessionAttendanceResponse?> MarkAttendanceAsync(
        AttendanceActor actor, int sessionId, MarkAttendanceRequest request, CancellationToken ct)
    {
        var session = await context.TrainingSessions
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is null)
        {
            return null;
        }

        EnsureCanManage(actor, session.Course);

        var now = DateTime.UtcNow;
        if (session.StartAt > now)
        {
            throw new InvalidOperationException("Attendance cannot be recorded before the session starts.");
        }

        var requestedIds = request.Entries.Select(e => e.StudentId).ToList();

        var duplicates = requestedIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                $"Student(s) {string.Join(", ", duplicates)} appear more than once in the request.");
        }

        // Only students with an approved, non-archived enrollment may be marked.
        var enrolledIds = (await ActiveEnrollments(session.CourseId)
                .Select(e => e.StudentId)
                .ToListAsync(ct))
            .ToHashSet();

        var notEnrolled = requestedIds.Where(id => !enrolledIds.Contains(id)).ToList();
        if (notEnrolled.Count > 0)
        {
            logger.LogWarning(
                "Rejected attendance for session {SessionId}: {Count} student(s) not enrolled", sessionId, notEnrolled.Count);
            throw new InvalidOperationException(
                $"Student(s) {string.Join(", ", notEnrolled)} do not have an approved enrollment in this course.");
        }

        // UPSERT: load the rows that already exist, update them, insert the rest.
        // This is what makes "Save" idempotent: sending the same roster twice gives the same database state.
        var existing = await context.AttendanceRecords
            .Where(a => a.SessionId == sessionId && requestedIds.Contains(a.StudentId))
            .ToDictionaryAsync(a => a.StudentId, ct);

        foreach (var entry in request.Entries)
        {
            var status = ParseStatus(entry.Status);
            var checkIn = ResolveCheckIn(status, entry.CheckInAt);
            var remarks = string.IsNullOrWhiteSpace(entry.Remarks) ? null : entry.Remarks.Trim();

            if (existing.TryGetValue(entry.StudentId, out var record))
            {
                record.Status = status;
                record.CheckInAt = checkIn;
                record.Remarks = remarks;
                record.RecordedBy = actor.UserId;
                record.RecordedAt = now;
            }
            else
            {
                context.AttendanceRecords.Add(new AttendanceRecord
                {
                    SessionId = sessionId,
                    StudentId = entry.StudentId,
                    Status = status,
                    CheckInAt = checkIn,
                    Remarks = remarks,
                    RecordedBy = actor.UserId,
                    RecordedAt = now
                });
            }
        }

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Two requests inserted the same (SessionId, StudentId) at the same moment;
            // the unique index rejected the loser.
            logger.LogWarning(ex, "Concurrent attendance save for session {SessionId}", sessionId);
            throw new InvalidOperationException(
                "Attendance for this session was changed at the same time by someone else. Reload and save again.", ex);
        }

        logger.LogInformation(
            "Saved {Count} attendance entries for session {SessionId}", request.Entries.Count, sessionId);

        // Re-read through the same code path as GET so the screen always shows what is really stored.
        context.ChangeTracker.Clear();
        var fresh = await context.TrainingSessions.AsNoTracking()
            .Include(s => s.Course)
            .FirstAsync(s => s.Id == sessionId, ct);
        return await BuildRosterAsync(fresh, ct);
    }

    public async Task<AttendanceRecordResponse?> UpdateAttendanceAsync(
        AttendanceActor actor, int recordId, UpdateAttendanceRequest request, CancellationToken ct)
    {
        var record = await context.AttendanceRecords
            .Include(a => a.Session).ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(a => a.Id == recordId, ct);
        if (record is null)
        {
            return null;
        }

        EnsureCanManage(actor, record.Session.Course);

        var status = ParseStatus(request.Status);
        record.Status = status;
        record.CheckInAt = ResolveCheckIn(status, request.CheckInAt);
        record.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();
        record.RecordedBy = actor.UserId;
        record.RecordedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Updated attendance record {RecordId} to {Status}", record.Id, status);

        return ToRecordResponse(record);
    }

    // ----------------------------------------------------------------- reports

    public async Task<StudentAttendanceResponse?> GetStudentAttendanceAsync(
        AttendanceActor actor, int studentId, int? courseId, int page, int pageSize, CancellationToken ct)
    {
        var student = await context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.ID == studentId, ct);
        if (student is null)
        {
            return null;
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = context.AttendanceRecords.AsNoTracking().Where(a => a.StudentId == studentId);

        if (courseId.HasValue)
        {
            var course = await context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId.Value, ct);
            if (course is null)
            {
                return null;
            }

            EnsureCanManage(actor, course);
            query = query.Where(a => a.Session.CourseId == courseId.Value);
        }
        else if (!actor.IsAdmin)
        {
            // No course given: an instructor only ever sees marks from courses they teach.
            query = query.Where(a => a.Session.Course.InstructorId == actor.UserId);
        }

        var allStatuses = await query.Select(a => a.Status).ToListAsync(ct);
        var summary = AttendanceCalculator.Summarize(allStatuses);

        var rows = await query
            .OrderByDescending(a => a.Session.StartAt)
            .ThenBy(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.SessionId,
                a.Session.CourseId,
                CourseCode = a.Session.Course.Code,
                a.Session.Topic,
                a.Session.StartAt,
                a.Status,
                a.Remarks
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new StudentAttendanceItem(
                r.Id, r.SessionId, r.CourseId, r.CourseCode, r.Topic, r.StartAt, r.Status.ToString(), r.Remarks))
            .ToList();

        return new StudentAttendanceResponse(
            student.ID,
            student.RegistrationNumber,
            student.Name,
            summary,
            new PagedResponse<StudentAttendanceItem>
            {
                Items = items,
                TotalCount = allStatuses.Count,
                Page = page,
                PageSize = pageSize
            });
    }

    public async Task<CourseAttendanceSummaryResponse?> GetAttendanceSummaryAsync(
        AttendanceActor actor, int courseId, CancellationToken ct)
    {
        var course = await context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null)
        {
            return null;
        }

        EnsureCanManage(actor, course);

        var now = DateTime.UtcNow;
        var sessionsHeld = await context.TrainingSessions
            .CountAsync(s => s.CourseId == courseId && s.StartAt <= now, ct);

        var students = await ActiveEnrollments(courseId)
            .Select(e => new { e.StudentId, e.Student.RegistrationNumber, e.Student.Name })
            .OrderBy(x => x.RegistrationNumber)
            .ToListAsync(ct);

        var marks = await context.AttendanceRecords.AsNoTracking()
            .Where(a => a.Session.CourseId == courseId)
            .Select(a => new { a.StudentId, a.Status })
            .ToListAsync(ct);

        var marksByStudent = marks
            .GroupBy(m => m.StudentId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.Status).ToList());

        var rows = students
            .Select(s => new StudentSummaryRow(
                s.StudentId,
                s.RegistrationNumber,
                s.Name,
                AttendanceCalculator.Summarize(
                    marksByStudent.GetValueOrDefault(s.StudentId) ?? new List<AttendanceStatus>())))
            .ToList();

        // The course-wide figure only includes students who are still enrolled.
        var rosterIds = students.Select(s => s.StudentId).ToHashSet();
        var overall = AttendanceCalculator.Summarize(
            marks.Where(m => rosterIds.Contains(m.StudentId)).Select(m => m.Status));

        return new CourseAttendanceSummaryResponse(
            course.Id, course.Code, course.Title, sessionsHeld, overall, rows);
    }

    // ----------------------------------------------------------------- helpers

    private IQueryable<Enrollment> ActiveEnrollments(int courseId) =>
        context.Enrollments.AsNoTracking()
            .Where(e => e.CourseId == courseId && e.Status == ApprovedStatus && !e.IsArchived);

    /// <summary>
    /// Role checks alone are not enough: an instructor must only touch courses assigned to them.
    /// Admins may touch every course. Mapped to HTTP 403 by GlobalExceptionHandler.
    /// </summary>
    private void EnsureCanManage(AttendanceActor actor, Course course)
    {
        if (actor.IsAdmin || course.InstructorId == actor.UserId)
        {
            return;
        }

        logger.LogWarning(
            "User {UserId} attempted to manage attendance for course {CourseId} they do not teach",
            actor.UserId, course.Id);
        throw new UnauthorizedAccessException("You are not the instructor of this course.");
    }

    private async Task<SessionAttendanceResponse> BuildRosterAsync(TrainingSession session, CancellationToken ct)
    {
        var students = await ActiveEnrollments(session.CourseId)
            .Select(e => new { e.StudentId, e.Student.RegistrationNumber, e.Student.Name })
            .OrderBy(x => x.RegistrationNumber)
            .ToListAsync(ct);

        var records = await context.AttendanceRecords.AsNoTracking()
            .Where(a => a.SessionId == session.Id)
            .ToListAsync(ct);
        var recordByStudent = records.ToDictionary(r => r.StudentId);

        // One query for the whole course, grouped in memory: gives each row its running percentage.
        var courseMarks = await context.AttendanceRecords.AsNoTracking()
            .Where(a => a.Session.CourseId == session.CourseId)
            .Select(a => new { a.StudentId, a.Status })
            .ToListAsync(ct);
        var percentageByStudent = courseMarks
            .GroupBy(m => m.StudentId)
            .ToDictionary(g => g.Key, g => AttendanceCalculator.Summarize(g.Select(m => m.Status)).Percentage);

        var rows = students.Select(s =>
        {
            recordByStudent.TryGetValue(s.StudentId, out var record);
            percentageByStudent.TryGetValue(s.StudentId, out var percentage);

            return new SessionRosterRow(
                s.StudentId,
                s.RegistrationNumber,
                s.Name,
                record?.Id,
                record?.Status.ToString(),
                record?.CheckInAt,
                record?.Remarks,
                percentage);
        }).ToList();

        var sessionResponse = ToSessionResponse(session, session.Course.Code, students.Count, records.Count);
        return new SessionAttendanceResponse(sessionResponse, rows);
    }

    private static AttendanceStatus ParseStatus(string value) =>
        Enum.TryParse<AttendanceStatus>(value, ignoreCase: true, out var status)
            ? status
            : throw new InvalidOperationException("Status must be Present, Absent, Late or Excused.");

    /// <summary>A check-in time only makes sense for someone who actually came.</summary>
    private static DateTime? ResolveCheckIn(AttendanceStatus status, DateTime? checkInAt) =>
        (status is AttendanceStatus.Present or AttendanceStatus.Late) && checkInAt.HasValue
            ? ToUtc(checkInAt.Value)
            : null;

    /// <summary>
    /// Npgsql refuses to write a Local DateTime into timestamptz, and JSON without a trailing "Z"
    /// arrives as Unspecified. Normalise everything to UTC before it reaches EF.
    /// </summary>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static SessionResponse ToSessionResponse(TrainingSession s, string courseCode, int enrolled, int recorded) =>
        new(s.Id, s.CourseId, courseCode, s.Topic, s.StartAt, s.EndAt, s.InstructorId, enrolled, recorded);

    private static AttendanceRecordResponse ToRecordResponse(AttendanceRecord a) =>
        new(a.Id, a.SessionId, a.StudentId, a.Status.ToString(), a.CheckInAt, a.Remarks, a.RecordedBy, a.RecordedAt);
}
