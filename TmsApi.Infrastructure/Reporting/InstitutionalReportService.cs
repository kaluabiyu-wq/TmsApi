using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TmsApi.Application.Reports;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Reporting;

public sealed class InstitutionalReportService(TmsDbContext db, IWebHostEnvironment environment) : IInstitutionalReportService
{
    private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase)
        { "students", "enrollment", "courses", "attendance", "performance", "certificates", "instructors", "completion" };

    public async Task<DashboardSummary> GetDashboardSummaryAsync(ReportQuery q, CancellationToken ct = default)
    {
        if (q.PassMark < 0 || q.PassMark > 100) throw new ArgumentException("Pass mark must be between 0 and 100.");
        var students = db.Students.AsNoTracking().Where(x => !x.IsDeleted);
        var totalStudents = await students.CountAsync(ct);
        var active = await students.CountAsync(x => x.IsActive && x.Status != "Graduated", ct);
        var graduated = await students.CountAsync(x => x.Status == "Graduated", ct);
        var inactive = totalStudents - active - graduated;
        var enrollments = db.Enrollments.AsNoTracking().Where(x => !x.IsArchived);
        if (q.From.HasValue) enrollments = enrollments.Where(x => x.EnrolledAt >= Utc(q.From.Value));
        if (q.To.HasValue) enrollments = enrollments.Where(x => x.EnrolledAt < Utc(q.To.Value).AddDays(1));
        var approved = await enrollments.CountAsync(x => x.Status == "Approved", ct);
        var pending = await enrollments.CountAsync(x => x.Status == "Pending", ct);
        var rejected = await enrollments.CountAsync(x => x.Status == "Rejected", ct);
        var courses = await db.Courses.AsNoTracking().CountAsync(ct);
        var certificateQuery = db.Certificates.AsNoTracking();
        if (q.From.HasValue) certificateQuery = certificateQuery.Where(x => x.IssuedAt >= Utc(q.From.Value));
        if (q.To.HasValue) certificateQuery = certificateQuery.Where(x => x.IssuedAt < Utc(q.To.Value).AddDays(1));
        var issued = await certificateQuery.CountAsync(x => x.Status == "Issued", ct);
        var revoked = await certificateQuery.CountAsync(x => x.Status == "Revoked", ct);
        var completedStudents = await certificateQuery.Where(x => x.Status == "Issued").Select(x => x.StudentId).Distinct().CountAsync(ct);
        var attendanceQuery = db.AttendanceRecords.AsNoTracking().Include(x => x.Session).AsQueryable();
        if (q.From.HasValue) attendanceQuery = attendanceQuery.Where(x => x.Session.StartAt >= Utc(q.From.Value));
        if (q.To.HasValue) attendanceQuery = attendanceQuery.Where(x => x.Session.StartAt < Utc(q.To.Value).AddDays(1));
        var attendanceCounts = await attendanceQuery.GroupBy(x => x.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct);
        var attended = attendanceCounts.Where(x => x.Status is AttendanceStatus.Present or AttendanceStatus.Late).Sum(x => x.Count);
        var attendanceDenominator = attendanceCounts.Where(x => x.Status is AttendanceStatus.Present or AttendanceStatus.Late or AttendanceStatus.Absent).Sum(x => x.Count);
        var attendancePct = Pct(attended, attendanceDenominator);
        var grades = db.GradeRecords.AsNoTracking().Where(x => x.IsPublished);
        if (q.From.HasValue) grades = grades.Where(x => x.PublishedAt >= Utc(q.From.Value));
        if (q.To.HasValue) grades = grades.Where(x => x.PublishedAt < Utc(q.To.Value).AddDays(1));
        var gradeCount = await grades.CountAsync(ct);
        var avgPerformance = gradeCount == 0 ? 0 : await grades.AverageAsync(x => x.Percentage, ct);
        var passedCount = await grades.CountAsync(x => x.Percentage >= q.PassMark, ct);
        var passRate = Pct(passedCount, gradeCount);
        var enrollmentDistribution = new[] { new ReportMetric("approved", "Approved", approved), new ReportMetric("pending", "Pending", pending), new ReportMetric("rejected", "Rejected", rejected) };
        var attendanceDistribution = Enum.GetValues<AttendanceStatus>().Select(s => new ReportMetric(s.ToString().ToLowerInvariant(), s.ToString(), attendanceCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0)).ToArray();
        var performanceRows = await grades.GroupBy(x => x.Course.Title).Select(g => new { Label = g.Key, Average = Math.Round(g.Average(x => x.Percentage), 2) }).OrderByDescending(x => x.Average).Take(8).ToListAsync(ct);
        var performanceByCourse = performanceRows.Select(x => new ReportMetric(x.Label, x.Label, x.Average, "%")).ToList();
        return new DashboardSummary(DateTime.UtcNow,totalStudents,active,inactive,graduated,courses,approved,pending,rejected,Math.Round(attendancePct,2),Math.Round(avgPerformance,2),Math.Round(passRate,2),issued,revoked,completedStudents,enrollmentDistribution,attendanceDistribution,performanceByCourse);
    }

    public async Task<ReportPage> GetReportAsync(string reportType, ReportQuery query, CancellationToken ct = default)
    {
        if (query.PassMark < 0 || query.PassMark > 100) throw new ArgumentException("Pass mark must be between 0 and 100.");
        var type = reportType.Trim().ToLowerInvariant();
        if (!Types.Contains(type)) throw new ArgumentException("Unsupported report type.");
        var page = Math.Max(1, query.Page); var size = Math.Clamp(query.PageSize, 1, 200);
        var items = new List<Dictionary<string, object?>>(); var metrics = new List<ReportMetric>(); int total;
        switch (type)
        {
            case "students":
            {
                var q = db.Students.AsNoTracking().Where(x => !x.IsDeleted);
                if (!string.IsNullOrWhiteSpace(query.Search)) q = q.Where(x => x.Name.Contains(query.Search) || x.RegistrationNumber.Contains(query.Search));
                total = await q.CountAsync(ct);
                var rows = await q.OrderBy(x => x.Name).Skip((page-1)*size).Take(size).Select(x => new { x.ID,x.RegistrationNumber,x.Name,x.GPA,x.IsActive,x.Status }).ToListAsync(ct);
                foreach(var x in rows) items.Add(Row(("studentId",x.ID),("registrationNumber",x.RegistrationNumber),("name",x.Name),("gpa",x.GPA),("status",x.Status == "Graduated" ? "Graduated" : x.IsActive ? "Active" : "Inactive")));
                var activeCount=await q.CountAsync(x=>x.IsActive && x.Status!="Graduated",ct);
                var inactiveCount=await q.CountAsync(x=>!x.IsActive && x.Status!="Graduated",ct);
                var graduatedCount=await q.CountAsync(x=>x.Status=="Graduated",ct);
                metrics.AddRange(new[]{new ReportMetric("active","Active",activeCount),new ReportMetric("inactive","Inactive",inactiveCount),new ReportMetric("graduated","Graduated",graduatedCount)}); break;
            }
            case "enrollment":
            {
                var q = db.Enrollments.AsNoTracking().Include(x=>x.Student).Include(x=>x.Course).Where(x=>!x.IsArchived);
                if(query.From.HasValue) q=q.Where(x=>x.EnrolledAt>=Utc(query.From.Value)); if(query.To.HasValue) q=q.Where(x=>x.EnrolledAt<Utc(query.To.Value).AddDays(1));
                if(query.CourseId.HasValue) q=q.Where(x=>x.CourseId==query.CourseId); if(!string.IsNullOrWhiteSpace(query.Search)) q=q.Where(x=>x.Student.Name.Contains(query.Search)||x.Course.Title.Contains(query.Search));
                total=await q.CountAsync(ct); var rows=await q.OrderByDescending(x=>x.EnrolledAt).Skip((page-1)*size).Take(size).Select(x=>new{x.ID,x.Student.Name,x.Student.RegistrationNumber,Course=x.Course.Title,x.Status,x.EnrolledAt,x.Year}).ToListAsync(ct);
                foreach(var x in rows) items.Add(Row(("enrollmentId",x.ID),("student",x.Name),("registrationNumber",x.RegistrationNumber),("course",x.Course),("status",x.Status),("enrolledAt",x.EnrolledAt),("year",x.Year)));
                var counts=await q.GroupBy(x=>x.Status).Select(g=>new{g.Key,Count=g.Count()}).ToListAsync(ct); metrics.AddRange(counts.Select(x=>new ReportMetric(x.Key,x.Key,x.Count))); break;
            }
            case "courses":
            {
                var q=db.Courses.AsNoTracking().AsQueryable(); if(!string.IsNullOrWhiteSpace(query.Search)) q=q.Where(x=>x.Title.Contains(query.Search)||x.Code.Contains(query.Search));
                total=await q.CountAsync(ct); var rows=await q.OrderBy(x=>x.Title).Skip((page-1)*size).Take(size).Select(x=>new{x.Id,x.Code,x.Title,x.MaxCapacity,x.InstructorId,Approved=x.Enrollments.Count(e=>!e.IsArchived&&e.Status=="Approved")}).ToListAsync(ct);
                foreach(var x in rows) items.Add(Row(("courseId",x.Id),("code",x.Code),("course",x.Title),("instructorId",x.InstructorId),("capacity",x.MaxCapacity),("approvedEnrollments",x.Approved),("utilizationPercent",Pct(x.Approved,x.MaxCapacity))));
                metrics.Add(new ReportMetric("courses","Courses",total)); break;
            }
            case "attendance":
            {
                var q=db.AttendanceRecords.AsNoTracking().Include(x=>x.Session).ThenInclude(x=>x.Course).Include(x=>x.Student).AsQueryable();
                if(query.From.HasValue) q=q.Where(x=>x.Session.StartAt>=Utc(query.From.Value)); if(query.To.HasValue) q=q.Where(x=>x.Session.StartAt<Utc(query.To.Value).AddDays(1)); if(query.CourseId.HasValue) q=q.Where(x=>x.Session.CourseId==query.CourseId);
                if(!string.IsNullOrWhiteSpace(query.Search)) q=q.Where(x=>x.Student.Name.Contains(query.Search)||x.Session.Course.Title.Contains(query.Search));
                total=await q.CountAsync(ct); var grouped=await q.GroupBy(x=>new{x.Session.CourseId,Course=x.Session.Course.Title}).Select(g=>new{g.Key.CourseId,g.Key.Course,Total=g.Count(),Present=g.Count(x=>x.Status==AttendanceStatus.Present),Late=g.Count(x=>x.Status==AttendanceStatus.Late),Absent=g.Count(x=>x.Status==AttendanceStatus.Absent),Excused=g.Count(x=>x.Status==AttendanceStatus.Excused)}).OrderBy(x=>x.Course).Skip((page-1)*size).Take(size).ToListAsync(ct);
                foreach(var x in grouped) items.Add(Row(("courseId",x.CourseId),("course",x.Course),("present",x.Present),("late",x.Late),("absent",x.Absent),("excused",x.Excused),("attendancePercent",Pct(x.Present+x.Late,x.Present+x.Late+x.Absent))));
                total=await q.Select(x=>x.Session.CourseId).Distinct().CountAsync(ct); var sum=await q.GroupBy(x=>x.Status).Select(g=>new{g.Key,Count=g.Count()}).ToListAsync(ct); var denominator=sum.Where(x=>x.Key is AttendanceStatus.Present or AttendanceStatus.Late or AttendanceStatus.Absent).Sum(x=>x.Count); metrics.Add(new ReportMetric("attendance","Attendance",Pct(sum.Where(x=>x.Key is AttendanceStatus.Present or AttendanceStatus.Late).Sum(x=>x.Count),denominator),"%")); break;
            }
            case "performance":
            {
                var q=db.GradeRecords.AsNoTracking().Include(x=>x.Course).Include(x=>x.Student).Where(x=>x.IsPublished);
                if(query.From.HasValue) q=q.Where(x=>x.PublishedAt>=Utc(query.From.Value)); if(query.To.HasValue) q=q.Where(x=>x.PublishedAt<Utc(query.To.Value).AddDays(1)); if(query.CourseId.HasValue) q=q.Where(x=>x.CourseId==query.CourseId); if(!string.IsNullOrWhiteSpace(query.Search)) q=q.Where(x=>x.Student.Name.Contains(query.Search)||x.Course.Title.Contains(query.Search));
                total=await q.CountAsync(ct); var rows=await q.OrderByDescending(x=>x.PublishedAt).Skip((page-1)*size).Take(size).Select(x=>new{x.Student.Name,x.Student.RegistrationNumber,Course=x.Course.Title,x.AssessmentId,x.Score,x.MaxScore,x.Percentage,x.PublishedAt}).ToListAsync(ct);
                foreach(var x in rows) items.Add(Row(("student",x.Name),("registrationNumber",x.RegistrationNumber),("course",x.Course),("assessmentId",x.AssessmentId),("score",x.Score),("maxScore",x.MaxScore),("percentage",x.Percentage),("result",x.Percentage>=query.PassMark?"Pass":"Fail"),("publishedAt",x.PublishedAt)));
                var gradeCount=await q.CountAsync(ct); var average=gradeCount==0?0:Math.Round(await q.AverageAsync(x=>x.Percentage,ct),2); var passed=await q.CountAsync(x=>x.Percentage>=query.PassMark,ct);
                metrics.AddRange(new[]{new ReportMetric("average","Average score",average,"%"),new ReportMetric("passRate","Pass rate",Pct(passed,gradeCount),"%"),new ReportMetric("passMark","Pass mark",query.PassMark,"%")}); break;
            }
            case "certificates":
            {
                var q=db.Certificates.AsNoTracking().Include(x=>x.Student).Include(x=>x.Course).AsQueryable(); if(query.From.HasValue) q=q.Where(x=>x.IssuedAt>=Utc(query.From.Value)); if(query.To.HasValue) q=q.Where(x=>x.IssuedAt<Utc(query.To.Value).AddDays(1)); if(!string.IsNullOrWhiteSpace(query.Search)) q=q.Where(x=>x.Student.Name.Contains(query.Search)||x.SerialNumber.Contains(query.Search)||x.Course.Title.Contains(query.Search));
                total=await q.CountAsync(ct); var rows=await q.OrderByDescending(x=>x.IssuedAt).Skip((page-1)*size).Take(size).Select(x=>new{x.SerialNumber,Student=x.Student.Name,Course=x.Course.Title,x.Status,x.IssuedAt,x.RevokedAt,x.RevocationReason}).ToListAsync(ct);
                foreach(var x in rows) items.Add(Row(("serialNumber",x.SerialNumber),("student",x.Student),("course",x.Course),("status",x.Status),("issuedAt",x.IssuedAt),("revokedAt",x.RevokedAt),("revocationReason",x.RevocationReason)));
                var counts=await q.GroupBy(x=>x.Status).Select(g=>new{g.Key,Count=g.Count()}).ToListAsync(ct); metrics.AddRange(counts.Select(x=>new ReportMetric(x.Key,x.Key,x.Count))); break;
            }
            case "instructors":
            {
                var q=db.Courses.AsNoTracking().AsQueryable(); if(!string.IsNullOrWhiteSpace(query.Search)) q=q.Where(x=>x.InstructorId.Contains(query.Search));
                var grouped=await q.Where(x=>x.InstructorId!="").GroupBy(x=>x.InstructorId).Select(g=>new{InstructorId=g.Key,Courses=g.Count()}).OrderByDescending(x=>x.Courses).ToListAsync(ct);
                var ids=grouped.Select(x=>x.InstructorId).ToList();
                var users=await db.Users.AsNoTracking().Where(x=>ids.Contains(x.Id)).Select(x=>new{x.Id,Name=(x.FirstName+" "+x.LastName).Trim(),x.Email}).ToDictionaryAsync(x=>x.Id,ct);
                var courseRows=await q.Where(x=>ids.Contains(x.InstructorId)).Select(x=>new{x.InstructorId,x.Title}).ToListAsync(ct);
                var courseNames=courseRows.GroupBy(x=>x.InstructorId).ToDictionary(g=>g.Key,g=>g.Select(x=>x.Title).ToList());
                var sessionQuery=db.TrainingSessions.AsNoTracking().Where(x=>ids.Contains(x.InstructorId));
                if(query.From.HasValue) sessionQuery=sessionQuery.Where(x=>x.StartAt>=Utc(query.From.Value));
                if(query.To.HasValue) sessionQuery=sessionQuery.Where(x=>x.StartAt<Utc(query.To.Value).AddDays(1));
                var sessions=await sessionQuery.GroupBy(x=>x.InstructorId).Select(g=>new{InstructorId=g.Key,Count=g.Count()}).ToDictionaryAsync(x=>x.InstructorId,x=>x.Count,ct);
                total=grouped.Count;
                foreach(var x in grouped.Skip((page-1)*size).Take(size))
                {
                    users.TryGetValue(x.InstructorId,out var user); var names=courseNames.GetValueOrDefault(x.InstructorId)??new List<string>();
                    items.Add(Row(("instructorId",x.InstructorId),("instructor",user?.Name??x.InstructorId),("email",user?.Email),("assignedCourses",x.Courses),("sessions",sessions.GetValueOrDefault(x.InstructorId)),("courses",string.Join(", ",names))));
                }
                metrics.Add(new ReportMetric("instructors","Instructors",total)); break;
            }
            case "completion":
            {
                // Issued certificates are the current persisted evidence of completed course training.
                var q=db.Certificates.AsNoTracking().Include(x=>x.Student).Include(x=>x.Course).Where(x=>x.Status=="Issued"); if(query.From.HasValue) q=q.Where(x=>x.IssuedAt>=Utc(query.From.Value)); if(query.To.HasValue) q=q.Where(x=>x.IssuedAt<Utc(query.To.Value).AddDays(1)); if(query.CourseId.HasValue) q=q.Where(x=>x.CourseId==query.CourseId); if(!string.IsNullOrWhiteSpace(query.Search)) q=q.Where(x=>x.Student.Name.Contains(query.Search)||x.Course.Title.Contains(query.Search));
                total=await q.CountAsync(ct); var rows=await q.OrderByDescending(x=>x.IssuedAt).Skip((page-1)*size).Take(size).Select(x=>new{x.Student.Name,x.Student.RegistrationNumber,Course=x.Course.Title,x.SerialNumber,x.IssuedAt}).ToListAsync(ct);
                foreach(var x in rows) items.Add(Row(("student",x.Name),("registrationNumber",x.RegistrationNumber),("course",x.Course),("certificateNumber",x.SerialNumber),("completedAt",x.IssuedAt)));
                metrics.Add(new ReportMetric("completed","Issued completion certificates",total)); break;
            }
            default: throw new ArgumentException("Unsupported report type.");
        }
        return new ReportPage(type,page,size,total,(int)Math.Ceiling(total/(double)size),metrics,items);
    }

    public async Task<GeneratedReportResponse> GenerateAsync(string userId, GenerateReportRequest request, CancellationToken ct = default)
    {
        if(string.IsNullOrWhiteSpace(request.ReportType) || string.IsNullOrWhiteSpace(request.Format)) throw new ArgumentException("Report type and format are required.");
        if(request.PassMark < 0 || request.PassMark > 100) throw new ArgumentException("Pass mark must be between 0 and 100.");
        var type=request.ReportType.Trim().ToLowerInvariant(); var format=request.Format.Trim().ToLowerInvariant();
        if(!Types.Contains(type)) throw new ArgumentException("Unsupported report type.");
        if(format is not ("pdf" or "xlsx")) throw new ArgumentException("Format must be pdf or xlsx.");
        if(request.From.HasValue && request.To.HasValue && request.From.Value.Date>request.To.Value.Date) throw new ArgumentException("From date must be on or before To date.");
        var definition=await db.ReportDefinitions.SingleOrDefaultAsync(x=>x.Key==type && x.IsEnabled,ct) ?? throw new InvalidOperationException("Report definition is not configured.");
        var generated=new GeneratedReport{ReportDefinitionId=definition.Id,RequestedByUserId=userId,Format=format,Status="Processing",FiltersJson=JsonSerializer.Serialize(request),RequestedAt=DateTime.UtcNow,ExpiresAt=DateTime.UtcNow.AddDays(30)};
        db.GeneratedReports.Add(generated); await db.SaveChangesAsync(ct);
        try
        {
            var report=await GetReportAsync(type,new ReportQuery(request.From,request.To,1,200,request.Search,request.CourseId,request.PassMark),ct);
            var allItems=report.Items.ToList();
            for(var page=2;page<=report.TotalPages;page++)
            {
                var next=await GetReportAsync(type,new ReportQuery(request.From,request.To,page,200,request.Search,request.CourseId,request.PassMark),ct);
                allItems.AddRange(next.Items);
            }
            report=report with { Page=1, PageSize=allItems.Count, Items=allItems };
            var bytes=format=="pdf"?GeneratePdf(report):ExcelWorkbookWriter.Write(report);
            var root=Path.Combine(environment.ContentRootPath,"App_Data","Reports"); Directory.CreateDirectory(root);
            var filename=$"{type}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.{format}"; var path=Path.Combine(root,filename);
            await File.WriteAllBytesAsync(path,bytes,ct);
            generated.Status="Completed"; generated.FileName=filename; generated.StoragePath=path; generated.FileSizeBytes=bytes.LongLength;
            generated.ContentType=format=="pdf"?"application/pdf":"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"; generated.CompletedAt=DateTime.UtcNow;
        }
        catch(Exception)
        {
            generated.Status="Failed"; generated.ErrorMessage="Report generation failed. Please contact an administrator.";
        }
        await db.SaveChangesAsync(ct); return ToResponse(generated,type);
    }

    public async Task<GeneratedReportResponse?> GetGeneratedAsync(long id,string userId,bool canViewAll,CancellationToken ct=default)
    {
        var q=db.GeneratedReports.AsNoTracking().Include(x=>x.ReportDefinition).Where(x=>x.Id==id);
        if(!canViewAll) q=q.Where(x=>x.RequestedByUserId==userId);
        var x=await q.SingleOrDefaultAsync(ct); return x is null?null:ToResponse(x,x.ReportDefinition.Key);
    }
    public async Task<(byte[] Content,string ContentType,string FileName)?> DownloadAsync(long id,string userId,bool canViewAll,CancellationToken ct=default)
    {
        var q=db.GeneratedReports.AsNoTracking().Where(x=>x.Id==id);
        if(!canViewAll) q=q.Where(x=>x.RequestedByUserId==userId);
        var x=await q.SingleOrDefaultAsync(ct); if(x is null||x.Status!="Completed"||x.StoragePath is null||(x.ExpiresAt is null || x.ExpiresAt < DateTime.UtcNow)||!File.Exists(x.StoragePath)) return null;
        var content=await File.ReadAllBytesAsync(x.StoragePath,ct); return(content,x.ContentType??"application/octet-stream",x.FileName??$"report-{id}.{x.Format}");
    }

    private static byte[] GeneratePdf(ReportPage report)
    {
        QuestPDF.Settings.License=LicenseType.Community;
        var headers=report.Items.FirstOrDefault()?.Keys.ToArray()??ColumnsFor(report.ReportType);
        var document=Document.Create(container=>container.Page(page=>
        {
            page.Size(PageSizes.A4.Landscape()); page.Margin(28); page.DefaultTextStyle(x=>x.FontFamily("Arial").FontSize(8));
            page.Header().Column(c=>{c.Item().Text("Institutional Report").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);c.Item().Text($"{report.ReportType.ToUpperInvariant()} · Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(9);});
            page.Content().PaddingVertical(12).Table(table=>
            {
                table.ColumnsDefinition(cols=>{foreach(var _ in headers) cols.RelativeColumn();});
                table.Header(h=>{foreach(var key in headers) h.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(key).Bold();});
                foreach(var row in report.Items) foreach(var key in headers) table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(Convert.ToString(row.GetValueOrDefault(key),CultureInfo.InvariantCulture)??"");
            });
            page.Footer().AlignRight().Text(x=>{x.Span("Page ");x.CurrentPageNumber();x.Span(" / ");x.TotalPages();});
        }));
        return document.GeneratePdf();
    }
    private static string[] ColumnsFor(string type)=>type switch
    {
        "students"=>new[]{"studentId","registrationNumber","name","gpa","status"},
        "enrollment"=>new[]{"enrollmentId","student","registrationNumber","course","status","enrolledAt","year"},
        "courses"=>new[]{"courseId","code","course","instructorId","capacity","approvedEnrollments","utilizationPercent"},
        "attendance"=>new[]{"courseId","course","present","late","absent","excused","attendancePercent"},
        "performance"=>new[]{"student","registrationNumber","course","assessmentId","score","maxScore","percentage","result","publishedAt"},
        "certificates"=>new[]{"serialNumber","student","course","status","issuedAt","revokedAt","revocationReason"},
        "instructors"=>new[]{"instructorId","instructor","email","assignedCourses","sessions","courses"},
        "completion"=>new[]{"student","registrationNumber","course","certificateNumber","completedAt"},
        _=>Array.Empty<string>()
    };
    private static DateTime Utc(DateTime d)=>d.Kind==DateTimeKind.Utc?d:DateTime.SpecifyKind(d,DateTimeKind.Utc);
    private static decimal Pct(int numerator,int denominator)=>denominator==0?0:Math.Round(numerator*100m/denominator,2);
    private static decimal Pct(decimal numerator,decimal denominator)=>denominator==0?0:Math.Round(numerator*100m/denominator,2);
    private static Dictionary<string,object?> Row(params (string Key,object? Value)[] pairs)=>pairs.ToDictionary(x=>x.Key,x=>x.Value);
    private static GeneratedReportResponse ToResponse(GeneratedReport x,string type)=>new(x.Id,type,x.Format,x.Status,x.FileName,x.ContentType,x.FileSizeBytes,x.ErrorMessage,x.RequestedAt,x.CompletedAt,x.ExpiresAt);
}
