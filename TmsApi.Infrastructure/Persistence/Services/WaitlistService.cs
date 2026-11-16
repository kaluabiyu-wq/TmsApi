using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Common;
using TmsApi.Application.DTOs;
using TmsApi.Application.Waitlists;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Services;

public class WaitlistService(
    TmsDbContext context,
    IWaitlistNotifier notifier,
    ILogger<WaitlistService> logger) : IWaitlistService
{
    public static readonly TimeSpan OfferWindow = TimeSpan.FromHours(24);

    private static readonly string[] LiveStatuses = [WaitlistStatus.Waiting, WaitlistStatus.Offered];
    private static readonly string[] SeatTakingEnrollment = ["Pending", "Approved"];

    private static Result<T, WaitlistError> Fail<T>(WaitlistError e) => Result<T, WaitlistError>.Failure(e);
    private static Result<T, WaitlistError> Ok<T>(T v) => Result<T, WaitlistError>.Success(v);

    
    public async Task<Result<WaitlistResponse, WaitlistError>> JoinWaitlistAsync(JoinWaitlistRequest request, CancellationToken ct)
    {
        int entryId;
        await using (var tx = await context.Database.BeginTransactionAsync(ct))
        {
            var course = await LockCourseAsync(request.CourseId, ct);
            if (course is null) return Fail<WaitlistResponse>(WaitlistError.CourseNotFound(request.CourseId));

            var studentOk = await context.Students.AsNoTracking()
                .AnyAsync(s => s.ID == request.StudentId && s.IsActive, ct);
            if (!studentOk) return Fail<WaitlistResponse>(WaitlistError.StudentNotFound(request.StudentId));

            var enrolled = await context.Enrollments.AnyAsync(e =>
                e.StudentId == request.StudentId && e.CourseId == request.CourseId &&
                SeatTakingEnrollment.Contains(e.Status), ct);
            if (enrolled) return Fail<WaitlistResponse>(WaitlistError.AlreadyEnrolled());

            var already = await context.WaitlistEntries.AnyAsync(w =>
                w.StudentId == request.StudentId && w.CourseId == request.CourseId &&
                LiveStatuses.Contains(w.Status), ct);
            if (already) return Fail<WaitlistResponse>(WaitlistError.AlreadyWaitlisted());

            if (await FreeSeatsAsync(course, ct) > 0)
                return Fail<WaitlistResponse>(WaitlistError.CourseHasSeats());

            var nextPosition = (await context.WaitlistEntries
                .Where(w => w.CourseId == course.Id)
                .MaxAsync(w => (int?)w.Position, ct) ?? 0) + 1;

            var entry = new WaitlistEntry
            {
                StudentId = request.StudentId,
                CourseId = course.Id,
                Position = nextPosition,
                Status = WaitlistStatus.Waiting,
                RequestedAt = DateTime.UtcNow
            };
            context.WaitlistEntries.Add(entry);
            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            entryId = entry.Id;
        }

        logger.LogInformation("Student {StudentId} joined waitlist of course {CourseId}", request.StudentId, request.CourseId);
        return Ok((await GetResponseAsync(entryId, ct))!);
    }

    // ------------------------------------------------------------- queries
    public async Task<PagedResponse<WaitlistResponse>> GetCourseWaitlistAsync(int courseId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.WaitlistEntries.AsNoTracking()
            .Where(w => w.CourseId == courseId && LiveStatuses.Contains(w.Status));

        var total = await query.CountAsync(ct);
        var items = await Project(query)
            .OrderBy(r => r.Status == WaitlistStatus.Offered ? 0 : 1) // offers first, then queue order
            .ThenBy(r => r.QueuePosition)
            .ThenBy(r => r.RequestedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return new PagedResponse<WaitlistResponse> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public Task<List<WaitlistResponse>> GetStudentWaitlistAsync(int studentId, CancellationToken ct) =>
        Project(context.WaitlistEntries.AsNoTracking()
                .Where(w => w.StudentId == studentId && LiveStatuses.Contains(w.Status)))
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);

    // -------------------------------------------------------------- offers
    public async Task<Result<IReadOnlyList<WaitlistResponse>, WaitlistError>> OfferNextSeatAsync(int courseId, CancellationToken ct)
    {
        var outcome = await ProcessCourseAsync(courseId, ct);
        if (outcome is null)
            return Fail<IReadOnlyList<WaitlistResponse>>(WaitlistError.CourseNotFound(courseId));

        await NotifyAsync(outcome, ct);
        var offered = await GetResponsesAsync(outcome.OfferedIds, ct);
        return Ok<IReadOnlyList<WaitlistResponse>>(offered);
    }

    public async Task<Result<WaitlistResponse, WaitlistError>> OfferSeatAsync(int entryId, CancellationToken ct)
    {
        var courseId = await context.WaitlistEntries.Where(w => w.Id == entryId)
            .Select(w => (int?)w.CourseId).FirstOrDefaultAsync(ct);
        if (courseId is null) return Fail<WaitlistResponse>(WaitlistError.EntryNotFound(entryId));

        await using (var tx = await context.Database.BeginTransactionAsync(ct))
        {
            var course = await LockCourseAsync(courseId.Value, ct);
            if (course is null) return Fail<WaitlistResponse>(WaitlistError.CourseNotFound(courseId.Value));

            var entry = await context.WaitlistEntries.FirstOrDefaultAsync(w => w.Id == entryId, ct);
            if (entry is null) return Fail<WaitlistResponse>(WaitlistError.EntryNotFound(entryId));
            if (entry.Status != WaitlistStatus.Waiting)
                return Fail<WaitlistResponse>(WaitlistError.InvalidStatus(entry.Status, "offer a seat"));
            if (await FreeSeatsAsync(course, ct) <= 0)
                return Fail<WaitlistResponse>(WaitlistError.NoSeatAvailable());

            MakeOffer(entry, DateTime.UtcNow);
            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        var response = (await GetResponseAsync(entryId, ct))!;
        await SafeAsync(() => notifier.NotifyOfferedAsync(response));
        return Ok(response);
    }

    // -------------------------------------------------------------- accept
    public async Task<Result<WaitlistResponse, WaitlistError>> AcceptOfferAsync(int entryId, CancellationToken ct)
    {
        var courseId = await context.WaitlistEntries.Where(w => w.Id == entryId)
            .Select(w => (int?)w.CourseId).FirstOrDefaultAsync(ct);
        if (courseId is null) return Fail<WaitlistResponse>(WaitlistError.EntryNotFound(entryId));

        var expired = false;
        await using (var tx = await context.Database.BeginTransactionAsync(ct))
        {
            var course = await LockCourseAsync(courseId.Value, ct);
            if (course is null) return Fail<WaitlistResponse>(WaitlistError.CourseNotFound(courseId.Value));

            var entry = await context.WaitlistEntries.FirstOrDefaultAsync(w => w.Id == entryId, ct);
            if (entry is null) return Fail<WaitlistResponse>(WaitlistError.EntryNotFound(entryId));
            if (entry.Status != WaitlistStatus.Offered)
                return Fail<WaitlistResponse>(WaitlistError.InvalidStatus(entry.Status, "accept the offer"));

            var now = DateTime.UtcNow;
            if (entry.ExpiresAt is null || entry.ExpiresAt <= now)
            {
                entry.Status = WaitlistStatus.Expired;
                expired = true;
            }
            else
            {
                // The seat was reserved when the offer was made, so no capacity re-check is needed.
                // Enrollment has a unique (StudentId, CourseId) index: reuse an old Rejected row if one exists.
                var enrollment = await context.Enrollments
                    .FirstOrDefaultAsync(e => e.StudentId == entry.StudentId && e.CourseId == entry.CourseId, ct);
                if (enrollment is null)
                {
                    context.Enrollments.Add(new Enrollment
                    {
                        StudentId = entry.StudentId,
                        CourseId = entry.CourseId,
                        Status = "Approved",
                        Year = now.Year,
                        EnrolledAt = now
                    });
                }
                else
                {
                    enrollment.Status = "Approved";
                    enrollment.IsArchived = false;
                    enrollment.EnrolledAt = now;
                    enrollment.Year = now.Year;
                }
                entry.Status = WaitlistStatus.Accepted;
            }

            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        if (expired)
        {
            await NotifyAsync(await ProcessCourseAsync(courseId.Value, ct), ct);
            return Fail<WaitlistResponse>(WaitlistError.OfferExpired());
        }

        logger.LogInformation("Waitlist entry {EntryId} accepted; enrollment approved", entryId);
        return Ok((await GetResponseAsync(entryId, ct))!);
    }

    // -------------------------------------------------------------- cancel
    public async Task<Result<WaitlistResponse, WaitlistError>> CancelWaitlistAsync(int entryId, CancellationToken ct)
    {
        var courseId = await context.WaitlistEntries.Where(w => w.Id == entryId)
            .Select(w => (int?)w.CourseId).FirstOrDefaultAsync(ct);
        if (courseId is null) return Fail<WaitlistResponse>(WaitlistError.EntryNotFound(entryId));

        var wasOffered = false;
        await using (var tx = await context.Database.BeginTransactionAsync(ct))
        {
            if (await LockCourseAsync(courseId.Value, ct) is null)
                return Fail<WaitlistResponse>(WaitlistError.CourseNotFound(courseId.Value));

            var entry = await context.WaitlistEntries.FirstOrDefaultAsync(w => w.Id == entryId, ct);
            if (entry is null) return Fail<WaitlistResponse>(WaitlistError.EntryNotFound(entryId));
            if (!LiveStatuses.Contains(entry.Status))
                return Fail<WaitlistResponse>(WaitlistError.InvalidStatus(entry.Status, "cancel"));

            wasOffered = entry.Status == WaitlistStatus.Offered;
            entry.Status = WaitlistStatus.Cancelled;
            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        // A declined offer frees a held seat: pass it on immediately.
        if (wasOffered) await NotifyAsync(await ProcessCourseAsync(courseId.Value, ct), ct);

        return Ok((await GetResponseAsync(entryId, ct))!);
    }

    // ------------------------------------------------- expiry / maintenance
    public async Task<int> ExpireOffersAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var courseIds = await context.WaitlistEntries.AsNoTracking()
            .Where(w => w.Status == WaitlistStatus.Offered && w.ExpiresAt <= now)
            .Select(w => w.CourseId).Distinct().ToListAsync(ct);

        var expiredTotal = 0;
        foreach (var id in courseIds)
        {
            var outcome = await ProcessCourseAsync(id, ct);
            if (outcome is null) continue;
            expiredTotal += outcome.Expired.Count;
            await NotifyAsync(outcome, ct);
        }
        return expiredTotal;
    }

    public async Task ProcessOpenSeatsAsync(CancellationToken ct)
    {
        var courseIds = await context.WaitlistEntries.AsNoTracking()
            .Where(w => w.Status == WaitlistStatus.Waiting)
            .Select(w => w.CourseId).Distinct().ToListAsync(ct);

        foreach (var id in courseIds)
        {
            try { await NotifyAsync(await ProcessCourseAsync(id, ct), ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Waitlist processing failed for course {CourseId}", id);
            }
        }
    }

    // ------------------------------------------------------------- helpers
    private sealed record Outcome(
        int CourseId, string CourseTitle,
        List<(int StudentId, int CourseId)> Expired, List<int> OfferedIds);

    /// <summary>One transaction: expire stale offers of the course, then hand every free seat to the queue.</summary>
    private async Task<Outcome?> ProcessCourseAsync(int courseId, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(ct);
        var course = await LockCourseAsync(courseId, ct);
        if (course is null) return null;

        var now = DateTime.UtcNow;

        var stale = await context.WaitlistEntries
            .Where(w => w.CourseId == courseId && w.Status == WaitlistStatus.Offered && w.ExpiresAt <= now)
            .ToListAsync(ct);
        foreach (var s in stale) s.Status = WaitlistStatus.Expired;
        await context.SaveChangesAsync(ct); // so FreeSeatsAsync sees the released seats

        var free = await FreeSeatsAsync(course, ct);
        var next = free <= 0
            ? new List<WaitlistEntry>()
            : await context.WaitlistEntries
                .Where(w => w.CourseId == courseId && w.Status == WaitlistStatus.Waiting && w.Student.IsActive)
                .OrderBy(w => w.Position)
                .Take(free)
                .ToListAsync(ct);
        foreach (var n in next) MakeOffer(n, now);

        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new Outcome(course.Id, course.Title,
            stale.Select(s => (s.StudentId, s.CourseId)).ToList(),
            next.Select(n => n.Id).ToList());
    }

    private async Task NotifyAsync(Outcome? outcome, CancellationToken ct)
    {
        if (outcome is null) return;
        foreach (var (studentId, courseId) in outcome.Expired)
            await SafeAsync(() => notifier.NotifyOfferExpiredAsync(studentId, courseId, outcome.CourseTitle));

        foreach (var offer in await GetResponsesAsync(outcome.OfferedIds, ct))
            await SafeAsync(() => notifier.NotifyOfferedAsync(offer));
    }

    private async Task SafeAsync(Func<Task> action)
    {
        // A failed push must never undo a committed seat allocation.
        try { await action(); }
        catch (Exception ex) { logger.LogWarning(ex, "Waitlist notification failed"); }
    }

    private static void MakeOffer(WaitlistEntry e, DateTime now)
    {
        e.Status = WaitlistStatus.Offered;
        e.OfferedAt = now;
        e.ExpiresAt = now + OfferWindow;
    }

    /// <summary>Row-locks the course (SELECT ... FOR UPDATE) so concurrent joins/offers/accepts
    /// for the same course run one after another. Must be called inside a transaction.</summary>
    private async Task<Course?> LockCourseAsync(int courseId, CancellationToken ct)
    {
        var rows = await context.Courses
            .FromSqlInterpolated($"SELECT * FROM \"Courses\" WHERE \"Id\" = {courseId} FOR UPDATE")
            .ToListAsync(ct);
        return rows.FirstOrDefault();
    }

    /// <summary>Free = capacity - (Pending+Approved enrollments) - (seats held by live offers).</summary>
    private async Task<int> FreeSeatsAsync(Course course, CancellationToken ct)
    {
        var enrolled = await context.Enrollments.CountAsync(e =>
            e.CourseId == course.Id && SeatTakingEnrollment.Contains(e.Status), ct);
        var held = await context.WaitlistEntries.CountAsync(w =>
            w.CourseId == course.Id && w.Status == WaitlistStatus.Offered, ct);
        return course.MaxCapacity - enrolled - held;
    }

    private IQueryable<WaitlistResponse> Project(IQueryable<WaitlistEntry> q) =>
        q.Select(w => new WaitlistResponse(
            w.Id, w.StudentId, w.Student.Name, w.Student.RegistrationNumber,
            w.CourseId, w.Course.Code, w.Course.Title,
            w.Status == WaitlistStatus.Waiting
                ? context.WaitlistEntries.Count(o => o.CourseId == w.CourseId
                      && o.Status == WaitlistStatus.Waiting && o.Position < w.Position) + 1
                : (int?)null,
            w.Status, w.RequestedAt, w.OfferedAt, w.ExpiresAt));

    private Task<WaitlistResponse?> GetResponseAsync(int id, CancellationToken ct) =>
        Project(context.WaitlistEntries.AsNoTracking().Where(w => w.Id == id)).FirstOrDefaultAsync(ct);

    private Task<List<WaitlistResponse>> GetResponsesAsync(List<int> ids, CancellationToken ct) =>
        ids.Count == 0
            ? Task.FromResult(new List<WaitlistResponse>())
            : Project(context.WaitlistEntries.AsNoTracking().Where(w => ids.Contains(w.Id))).ToListAsync(ct);
}