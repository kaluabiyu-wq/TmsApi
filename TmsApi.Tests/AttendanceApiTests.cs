using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using Xunit;

namespace TmsApi_Tests;

public class AttendanceApiTests : IClassFixture<CustomWebApplicatonFactory>
{
    private readonly CustomWebApplicatonFactory _factory;
    private readonly HttpClient _client;

    public AttendanceApiTests(CustomWebApplicatonFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ---------------------------------------------------------------- seeding

    private sealed record Seed(int CourseId, string InstructorId, int StudentA, int StudentB, int Outsider);

    /// <summary>
    /// One course with an instructor, two APPROVED students, and one student who is
    /// not enrolled at all (to prove the enrollment rule). Unique values keep tests independent,
    /// because every test class shares the same in-memory database.
    /// </summary>
    private async Task<Seed> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TmsDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var instructorId = $"instr-{suffix}";

        var course = new Course
        {
            Code = $"ATT-{suffix}",
            Title = "Attendance test course",
            MaxCapacity = 30,
            InstructorId = instructorId
        };
        var a = new Student { RegistrationNumber = $"A-{suffix}", Name = "Alice Test" };
        var b = new Student { RegistrationNumber = $"B-{suffix}", Name = "Bob Test" };
        var outsider = new Student { RegistrationNumber = $"C-{suffix}", Name = "Carol Outsider" };

        db.Courses.Add(course);
        db.Students.AddRange(a, b, outsider);
        await db.SaveChangesAsync();

        db.Enrollments.AddRange(
            new Enrollment { StudentId = a.ID, CourseId = course.Id, Status = "Approved", Year = 2026 },
            new Enrollment { StudentId = b.ID, CourseId = course.Id, Status = "Approved", Year = 2026 });
        await db.SaveChangesAsync();

        return new Seed(course.Id, instructorId, a.ID, b.ID, outsider.ID);
    }

    private async Task<int> CreatePastSessionAsync(Seed seed, int hoursAgo, string topic = "Intro")
    {
        _client.SignInAs("Instructor", seed.InstructorId);
        var start = DateTime.UtcNow.AddHours(-hoursAgo);

        var response = await _client.PostAsJsonAsync("/api/v2.0/sessions", new
        {
            courseId = seed.CourseId,
            topic,
            startAt = start,
            endAt = start.AddHours(1)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SessionJson>();
        return created!.Id;
    }

    private static object Entry(int studentId, string status) => new { studentId, status };

    // ------------------------------------------------------- create session

    [Fact]
    public async Task CreateSession_WithoutToken_ReturnsUnauthorized()
    {
        _client.SignOut();

        var response = await _client.PostAsJsonAsync("/api/v2.0/sessions",
            new { courseId = 1, topic = "x", startAt = DateTime.UtcNow, endAt = DateTime.UtcNow.AddHours(1) });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_AsStudent_ReturnsForbidden()
    {
        _client.SignInAs("Student");

        var response = await _client.PostAsJsonAsync("/api/v2.0/sessions",
            new { courseId = 1, topic = "x", startAt = DateTime.UtcNow, endAt = DateTime.UtcNow.AddHours(1) });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_AsInstructorOfAnotherCourse_ReturnsForbidden()
    {
        var seed = await SeedAsync();
        _client.SignInAs("Instructor", "someone-else");

        var response = await _client.PostAsJsonAsync("/api/v2.0/sessions", new
        {
            courseId = seed.CourseId,
            topic = "Hijack",
            startAt = DateTime.UtcNow,
            endAt = DateTime.UtcNow.AddHours(1)
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_AsOwner_ReturnsCreatedWithLocation()
    {
        var seed = await SeedAsync();
        _client.SignInAs("Instructor", seed.InstructorId);
        var start = DateTime.UtcNow.AddDays(1);

        var response = await _client.PostAsJsonAsync("/api/v2.0/sessions", new
        {
            courseId = seed.CourseId,
            topic = "  Variables  ",
            startAt = start,
            endAt = start.AddHours(2)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<SessionJson>();
        Assert.Equal("Variables", created!.Topic);
        Assert.Equal(2, created.EnrolledCount);
        Assert.Equal(0, created.RecordedCount);
    }

    [Fact]
    public async Task CreateSession_AsAdmin_ForAnyCourse_Succeeds()
    {
        var seed = await SeedAsync();
        _client.SignInAs("Admin", "admin-user");
        var start = DateTime.UtcNow.AddDays(2);

        var response = await _client.PostAsJsonAsync("/api/v2.0/sessions", new
        {
            courseId = seed.CourseId,
            topic = "Admin created",
            startAt = start,
            endAt = start.AddHours(1)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SessionJson>();
        // The session is owned by the course's instructor, not by the admin who clicked Save.
        Assert.Equal(seed.InstructorId, created!.InstructorId);
    }

    [Fact]
    public async Task CreateSession_MissingTopic_ReturnsValidationProblem()
    {
        var seed = await SeedAsync();
        _client.SignInAs("Instructor", seed.InstructorId);

        var response = await _client.PostAsJsonAsync("/api/v2.0/sessions", new
        {
            courseId = seed.CourseId,
            topic = "",
            startAt = DateTime.UtcNow,
            endAt = DateTime.UtcNow.AddHours(1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreateSession_UnknownCourse_ReturnsNotFound()
    {
        _client.SignInAs("Admin");

        var response = await _client.PostAsJsonAsync("/api/v2.0/sessions", new
        {
            courseId = 999999,
            topic = "Ghost",
            startAt = DateTime.UtcNow,
            endAt = DateTime.UtcNow.AddHours(1)
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_EndBeforeStart_ReturnsConflict()
    {
        var seed = await SeedAsync();
        _client.SignInAs("Instructor", seed.InstructorId);
        var start = DateTime.UtcNow.AddDays(1);

        var response = await _client.PostAsJsonAsync("/api/v2.0/sessions", new
        {
            courseId = seed.CourseId,
            topic = "Backwards",
            startAt = start,
            endAt = start.AddHours(-1)
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_OverlappingExistingSession_ReturnsConflict()
    {
        var seed = await SeedAsync();
        _client.SignInAs("Instructor", seed.InstructorId);
        var start = DateTime.UtcNow.AddDays(3);

        var first = await _client.PostAsJsonAsync("/api/v2.0/sessions",
            new { courseId = seed.CourseId, topic = "One", startAt = start, endAt = start.AddHours(2) });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var overlapping = await _client.PostAsJsonAsync("/api/v2.0/sessions",
            new { courseId = seed.CourseId, topic = "Two", startAt = start.AddHours(1), endAt = start.AddHours(3) });

        Assert.Equal(HttpStatusCode.Conflict, overlapping.StatusCode);
    }

    // --------------------------------------------------------- mark attendance

    [Fact]
    public async Task Mark_SavesRoster_AndSavingTwiceDoesNotDuplicate()
    {
        var seed = await SeedAsync();
        var sessionId = await CreatePastSessionAsync(seed, hoursAgo: 3);
        var url = $"/api/v2.0/sessions/{sessionId}/attendance";

        var first = await _client.PostAsJsonAsync(url,
            new { entries = new[] { Entry(seed.StudentA, "Present"), Entry(seed.StudentB, "Absent") } });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Change one mark and save the SAME roster again: still exactly two rows.
        var second = await _client.PostAsJsonAsync(url,
            new { entries = new[] { Entry(seed.StudentA, "Late"), Entry(seed.StudentB, "Absent") } });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var roster = await second.Content.ReadFromJsonAsync<RosterJson>();
        Assert.Equal(2, roster!.Session.RecordedCount);
        Assert.Equal("Late", roster.Rows.Single(r => r.StudentId == seed.StudentA).Status);
        Assert.Equal("Absent", roster.Rows.Single(r => r.StudentId == seed.StudentB).Status);
    }

    [Fact]
    public async Task Mark_StudentWithoutApprovedEnrollment_ReturnsConflict()
    {
        var seed = await SeedAsync();
        var sessionId = await CreatePastSessionAsync(seed, hoursAgo: 3);

        var response = await _client.PostAsJsonAsync($"/api/v2.0/sessions/{sessionId}/attendance",
            new { entries = new[] { Entry(seed.Outsider, "Present") } });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Mark_InvalidStatus_ReturnsValidationProblem()
    {
        var seed = await SeedAsync();
        var sessionId = await CreatePastSessionAsync(seed, hoursAgo: 3);

        var response = await _client.PostAsJsonAsync($"/api/v2.0/sessions/{sessionId}/attendance",
            new { entries = new[] { Entry(seed.StudentA, "Sleeping") } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Mark_DuplicateStudentInRequest_ReturnsConflict()
    {
        var seed = await SeedAsync();
        var sessionId = await CreatePastSessionAsync(seed, hoursAgo: 3);

        var response = await _client.PostAsJsonAsync($"/api/v2.0/sessions/{sessionId}/attendance",
            new { entries = new[] { Entry(seed.StudentA, "Present"), Entry(seed.StudentA, "Absent") } });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Mark_BeforeSessionStarts_ReturnsConflict()
    {
        var seed = await SeedAsync();
        _client.SignInAs("Instructor", seed.InstructorId);
        var start = DateTime.UtcNow.AddDays(5);
        var create = await _client.PostAsJsonAsync("/api/v2.0/sessions",
            new { courseId = seed.CourseId, topic = "Future", startAt = start, endAt = start.AddHours(1) });
        var session = await create.Content.ReadFromJsonAsync<SessionJson>();

        var response = await _client.PostAsJsonAsync($"/api/v2.0/sessions/{session!.Id}/attendance",
            new { entries = new[] { Entry(seed.StudentA, "Present") } });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Mark_AsInstructorOfAnotherCourse_ReturnsForbidden()
    {
        var seed = await SeedAsync();
        var sessionId = await CreatePastSessionAsync(seed, hoursAgo: 3);

        _client.SignInAs("Instructor", "someone-else");
        var response = await _client.PostAsJsonAsync($"/api/v2.0/sessions/{sessionId}/attendance",
            new { entries = new[] { Entry(seed.StudentA, "Present") } });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Mark_UnknownSession_ReturnsNotFound()
    {
        _client.SignInAs("Admin");

        var response = await _client.PostAsJsonAsync("/api/v2.0/sessions/999999/attendance",
            new { entries = new[] { Entry(1, "Present") } });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ------------------------------------------------------- roster and update

    [Fact]
    public async Task GetRoster_BeforeAnyMarks_ListsEnrolledStudentsOnly_WithNullStatus()
    {
        var seed = await SeedAsync();
        var sessionId = await CreatePastSessionAsync(seed, hoursAgo: 3);

        var roster = await _client.GetFromJsonAsync<RosterJson>($"/api/v2.0/sessions/{sessionId}/attendance");

        Assert.Equal(2, roster!.Rows.Count);
        Assert.DoesNotContain(roster.Rows, r => r.StudentId == seed.Outsider);
        Assert.All(roster.Rows, r => Assert.Null(r.Status));
    }

    [Fact]
    public async Task UpdateRecord_ChangesStatus_AndUnknownIdIsNotFound()
    {
        var seed = await SeedAsync();
        var sessionId = await CreatePastSessionAsync(seed, hoursAgo: 3);
        var saved = await _client.PostAsJsonAsync($"/api/v2.0/sessions/{sessionId}/attendance",
            new { entries = new[] { Entry(seed.StudentA, "Absent") } });
        var roster = await saved.Content.ReadFromJsonAsync<RosterJson>();
        var recordId = roster!.Rows.Single(r => r.StudentId == seed.StudentA).RecordId!.Value;

        var update = await _client.PutAsJsonAsync($"/api/v2.0/attendance/{recordId}",
            new { status = "Excused", remarks = "Doctor's note" });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var body = await update.Content.ReadFromJsonAsync<RecordJson>();
        Assert.Equal("Excused", body!.Status);
        Assert.Equal("Doctor's note", body.Remarks);

        var missing = await _client.PutAsJsonAsync("/api/v2.0/attendance/999999", new { status = "Present" });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    // ----------------------------------------------------------------- reports

    [Fact]
    public async Task Summary_ComputesPercentagePerStudent_ExcusedIsNeutral()
    {
        var seed = await SeedAsync();
        var s1 = await CreatePastSessionAsync(seed, hoursAgo: 8, topic: "One");
        var s2 = await CreatePastSessionAsync(seed, hoursAgo: 5, topic: "Two");
        var s3 = await CreatePastSessionAsync(seed, hoursAgo: 2, topic: "Three");

        // Alice: Present, Absent, Excused  -> 1 of 2 counted = 50.0%
        // Bob:   Present, Present, Late    -> 3 of 3 counted = 100.0%
        await _client.PostAsJsonAsync($"/api/v2.0/sessions/{s1}/attendance",
            new { entries = new[] { Entry(seed.StudentA, "Present"), Entry(seed.StudentB, "Present") } });
        await _client.PostAsJsonAsync($"/api/v2.0/sessions/{s2}/attendance",
            new { entries = new[] { Entry(seed.StudentA, "Absent"), Entry(seed.StudentB, "Present") } });
        await _client.PostAsJsonAsync($"/api/v2.0/sessions/{s3}/attendance",
            new { entries = new[] { Entry(seed.StudentA, "Excused"), Entry(seed.StudentB, "Late") } });

        var summary = await _client.GetFromJsonAsync<SummaryJson>(
            $"/api/v2.0/courses/{seed.CourseId}/attendance/summary");

        Assert.Equal(3, summary!.SessionsHeld);
        Assert.Equal(50.0m, summary.Students.Single(s => s.StudentId == seed.StudentA).Summary.Percentage);
        Assert.Equal(100.0m, summary.Students.Single(s => s.StudentId == seed.StudentB).Summary.Percentage);
        Assert.Equal(80.0m, summary.Overall.Percentage); // 4 attended of 5 counted
    }

    [Fact]
    public async Task StudentAttendance_ReturnsHistoryAndSummary()
    {
        var seed = await SeedAsync();
        var s1 = await CreatePastSessionAsync(seed, hoursAgo: 6);
        var s2 = await CreatePastSessionAsync(seed, hoursAgo: 3);
        await _client.PostAsJsonAsync($"/api/v2.0/sessions/{s1}/attendance",
            new { entries = new[] { Entry(seed.StudentA, "Present") } });
        await _client.PostAsJsonAsync($"/api/v2.0/sessions/{s2}/attendance",
            new { entries = new[] { Entry(seed.StudentA, "Absent") } });

        var report = await _client.GetFromJsonAsync<StudentReportJson>(
            $"/api/v2.0/students/{seed.StudentA}/attendance?courseId={seed.CourseId}");

        Assert.Equal(2, report!.Records.TotalCount);
        Assert.Equal(50.0m, report.Summary.Percentage);
    }

    [Fact]
    public async Task StudentAttendance_ForAnotherInstructorsCourse_ReturnsForbidden()
    {
        var seed = await SeedAsync();
        _client.SignInAs("Instructor", "someone-else");

        var response = await _client.GetAsync(
            $"/api/v2.0/students/{seed.StudentA}/attendance?courseId={seed.CourseId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CourseSessions_ListsCreatedSessions_Paged()
    {
        var seed = await SeedAsync();
        await CreatePastSessionAsync(seed, hoursAgo: 6, topic: "Listed one");
        await CreatePastSessionAsync(seed, hoursAgo: 3, topic: "Listed two");

        var page = await _client.GetFromJsonAsync<PagedSessionsJson>(
            $"/api/v2.0/courses/{seed.CourseId}/sessions?page=1&pageSize=50");

        Assert.Equal(2, page!.TotalCount);
        Assert.Equal("Listed two", page.Items[0].Topic); // newest first
    }

    // ----------------------------------------------------------- JSON shapes

    private sealed class SessionJson
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Topic { get; set; } = "";
        public string InstructorId { get; set; } = "";
        public int EnrolledCount { get; set; }
        public int RecordedCount { get; set; }
    }

    private sealed class RowJson
    {
        public int StudentId { get; set; }
        public int? RecordId { get; set; }
        public string? Status { get; set; }
    }

    private sealed class RosterJson
    {
        public SessionJson Session { get; set; } = new();
        public List<RowJson> Rows { get; set; } = new();
    }

    private sealed class RecordJson
    {
        public string Status { get; set; } = "";
        public string? Remarks { get; set; }
    }

    private sealed class SummaryValuesJson
    {
        public decimal? Percentage { get; set; }
    }

    private sealed class StudentSummaryJson
    {
        public int StudentId { get; set; }
        public SummaryValuesJson Summary { get; set; } = new();
    }

    private sealed class SummaryJson
    {
        public int SessionsHeld { get; set; }
        public SummaryValuesJson Overall { get; set; } = new();
        public List<StudentSummaryJson> Students { get; set; } = new();
    }

    private sealed class StudentRecordsJson
    {
        public int TotalCount { get; set; }
    }

    private sealed class StudentReportJson
    {
        public SummaryValuesJson Summary { get; set; } = new();
        public StudentRecordsJson Records { get; set; } = new();
    }

    private sealed class PagedSessionsJson
    {
        public List<SessionJson> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
