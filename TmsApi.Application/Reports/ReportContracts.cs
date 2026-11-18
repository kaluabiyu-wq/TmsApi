namespace TmsApi.Application.Reports;

public sealed record ReportQuery(
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    int? CourseId = null,
    decimal PassMark = 50m);

public sealed record ReportMetric(string Key, string Label, decimal Value, string? Unit = null);
public sealed record ReportPage(
    string ReportType,
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyList<ReportMetric> Metrics,
    IReadOnlyList<Dictionary<string, object?>> Items);

public sealed record DashboardSummary(
    DateTime GeneratedAtUtc,
    int TotalStudents,
    int ActiveStudents,
    int InactiveStudents,
    int GraduatedStudents,
    int TotalCourses,
    int ApprovedEnrollments,
    int PendingEnrollments,
    int RejectedEnrollments,
    decimal AverageAttendancePercent,
    decimal AveragePerformancePercent,
    decimal PassRatePercent,
    int IssuedCertificates,
    int RevokedCertificates,
    int CompletedStudents,
    IReadOnlyList<ReportMetric> EnrollmentDistribution,
    IReadOnlyList<ReportMetric> AttendanceDistribution,
    IReadOnlyList<ReportMetric> PerformanceByCourse);

public sealed record GenerateReportRequest(
    string ReportType,
    string Format,
    DateTime? From = null,
    DateTime? To = null,
    string? Search = null,
    int? CourseId = null,
    decimal PassMark = 50m);

public sealed record GeneratedReportResponse(
    long Id,
    string ReportType,
    string Format,
    string Status,
    string? FileName,
    string? ContentType,
    long? FileSizeBytes,
    string? ErrorMessage,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    DateTime? ExpiresAt);

public interface IInstitutionalReportService
{
    Task<DashboardSummary> GetDashboardSummaryAsync(ReportQuery query, CancellationToken ct = default);
    Task<ReportPage> GetReportAsync(string reportType, ReportQuery query, CancellationToken ct = default);
    Task<GeneratedReportResponse> GenerateAsync(string userId, GenerateReportRequest request, CancellationToken ct = default);
    Task<GeneratedReportResponse?> GetGeneratedAsync(long id, string userId, bool canViewAll, CancellationToken ct = default);
    Task<(byte[] Content, string ContentType, string FileName)?> DownloadAsync(long id, string userId, bool canViewAll, CancellationToken ct = default);
}
