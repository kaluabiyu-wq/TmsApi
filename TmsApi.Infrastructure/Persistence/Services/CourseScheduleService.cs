using System.Data;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Scheduling;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Services;

public sealed class CourseScheduleService(TmsDbContext db) : ICourseScheduleService
{
    public async Task<CourseScheduleResponse> CreateAsync(CreateCourseScheduleRequest request, CancellationToken ct)
    {
        ValidateTimes(request.StartAt, request.EndAt);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var entity = new CourseSchedule
        {
            CourseId = request.CourseId,
            InstructorId = request.InstructorId.Trim(),
            ClassroomId = request.ClassroomId,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Topic = CleanTopic(request.Topic),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await ValidateBookingAsync(entity, null, ct);
        db.CourseSchedules.Add(entity);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetByIdAsync(entity.Id, ct) ?? throw new InvalidOperationException("Created schedule could not be loaded.");
    }

    public async Task<CourseScheduleResponse?> UpdateAsync(int id, UpdateCourseScheduleRequest request, CancellationToken ct)
    {
        ValidateTimes(request.StartAt, request.EndAt);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var entity = await db.CourseSchedules.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;

        entity.CourseId = request.CourseId;
        entity.InstructorId = request.InstructorId.Trim();
        entity.ClassroomId = request.ClassroomId;
        entity.StartAt = request.StartAt;
        entity.EndAt = request.EndAt;
        entity.Topic = CleanTopic(request.Topic);
        entity.IsCancelled = request.IsCancelled;
        entity.UpdatedAt = DateTime.UtcNow;

        if (!entity.IsCancelled)
            await ValidateBookingAsync(entity, id, ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public Task<CourseScheduleResponse?> GetByIdAsync(int id, CancellationToken ct) =>
        Project(db.CourseSchedules.AsNoTracking().Where(x => x.Id == id)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<CourseScheduleResponse>> GetCalendarAsync(ScheduleCalendarQuery query, CancellationToken ct)
    {
        ValidateRange(query.From, query.To);
        var q = db.CourseSchedules.AsNoTracking().Where(x => !x.IsCancelled && x.StartAt < query.To && x.EndAt > query.From);
        if (!string.IsNullOrWhiteSpace(query.InstructorId)) q = q.Where(x => x.InstructorId == query.InstructorId);
        if (query.ClassroomId.HasValue) q = q.Where(x => x.ClassroomId == query.ClassroomId.Value);
        if (query.CourseId.HasValue) q = q.Where(x => x.CourseId == query.CourseId.Value);
        return await Project(q).OrderBy(x => x.StartAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CourseScheduleResponse>> GetInstructorScheduleAsync(string instructorId, DateTime from, DateTime to, CancellationToken ct)
    {
        ValidateRange(from, to);
        return await Project(db.CourseSchedules.AsNoTracking()
            .Where(x => !x.IsCancelled && x.InstructorId == instructorId && x.StartAt < to && x.EndAt > from))
            .OrderBy(x => x.StartAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CourseScheduleResponse>> GetCourseScheduleAsync(int courseId, DateTime from, DateTime to, string? instructorId, CancellationToken ct)
    {
        ValidateRange(from, to);
        var q = db.CourseSchedules.AsNoTracking()
            .Where(x => !x.IsCancelled && x.CourseId == courseId && x.StartAt < to && x.EndAt > from);
        if (!string.IsNullOrWhiteSpace(instructorId))
            q = q.Where(x => x.InstructorId == instructorId);
        return await Project(q).OrderBy(x => x.StartAt).ToListAsync(ct);
    }

    private async Task ValidateBookingAsync(CourseSchedule s, int? excludeId, CancellationToken ct)
    {
        if (s.StartAt.Kind != DateTimeKind.Utc || s.EndAt.Kind != DateTimeKind.Utc)
            throw new InvalidOperationException("StartAt and EndAt must be UTC timestamps (ISO-8601 ending in Z).");
        if (s.EndAt <= s.StartAt)
            throw new InvalidOperationException("End time must be later than start time.");

        var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == s.CourseId, ct)
            ?? throw new KeyNotFoundException("Course was not found.");
        if (string.IsNullOrWhiteSpace(course.InstructorId) || course.InstructorId != s.InstructorId)
            throw new InvalidOperationException("The selected instructor must be the instructor assigned to this course.");

        var instructorExists = await (
            from user in db.Users
            join userRole in db.UserRoles on user.Id equals userRole.UserId
            join role in db.Roles on userRole.RoleId equals role.Id
            where user.Id == s.InstructorId && role.Name == "Instructor"
            select user.Id).AnyAsync(ct);
        if (!instructorExists) throw new KeyNotFoundException("The selected user does not exist or does not have the Instructor role.");

        var room = await db.Classrooms.FirstOrDefaultAsync(x => x.Id == s.ClassroomId, ct)
            ?? throw new KeyNotFoundException("Classroom was not found.");
        if (!room.IsActive) throw new InvalidOperationException("An inactive classroom cannot be scheduled.");

        var enrolled = await db.Enrollments.CountAsync(x => x.CourseId == s.CourseId && x.Status == "Approved" && !x.IsArchived, ct);
        if (enrolled > room.Capacity)
            throw new InvalidOperationException($"Classroom capacity ({room.Capacity}) is below the approved enrollment count ({enrolled}).");

        var overlap = db.CourseSchedules.Where(x => !x.IsCancelled
            && (!excludeId.HasValue || x.Id != excludeId.Value)
            && x.StartAt < s.EndAt && x.EndAt > s.StartAt);

        if (await overlap.AnyAsync(x => x.InstructorId == s.InstructorId, ct))
            throw new InvalidOperationException("Instructor scheduling conflict: this instructor already has a class during the selected time.");
        if (await overlap.AnyAsync(x => x.ClassroomId == s.ClassroomId, ct))
            throw new InvalidOperationException("Classroom scheduling conflict: this classroom is already booked during the selected time.");
    }

    private static IQueryable<CourseScheduleResponse> Project(IQueryable<CourseSchedule> q) =>
        q.Select(x => new CourseScheduleResponse(
            x.Id, x.CourseId, x.Course.Code, x.Course.Title,
            x.InstructorId, x.Instructor.FirstName + " " + x.Instructor.LastName,
            x.ClassroomId, x.Classroom.Name, x.Classroom.Location,
            x.StartAt, x.EndAt, x.Topic, x.IsCancelled));

    private static void ValidateTimes(DateTime start, DateTime end)
    {
        if (start.Kind != DateTimeKind.Utc || end.Kind != DateTimeKind.Utc)
            throw new InvalidOperationException("Use UTC timestamps (ISO-8601 ending in Z).");
        if (end <= start) throw new InvalidOperationException("End time must be later than start time.");
    }

    private static void ValidateRange(DateTime from, DateTime to)
    {
        if (from.Kind != DateTimeKind.Utc || to.Kind != DateTimeKind.Utc || to <= from)
            throw new InvalidOperationException("A valid UTC From/To range is required.");
        if ((to - from).TotalDays > 93)
            throw new InvalidOperationException("Calendar range cannot exceed 93 days.");
    }

    private static string? CleanTopic(string? topic) =>
        string.IsNullOrWhiteSpace(topic) ? null : topic.Trim();
}
