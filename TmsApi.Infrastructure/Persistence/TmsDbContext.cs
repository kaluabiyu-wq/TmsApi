

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence;

public class TmsDbContext : IdentityDbContext<TmsUser>
{
    public  TmsDbContext(DbContextOptions<TmsDbContext> options) : 
  base(options) 
{}
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();
    public DbSet<AssessmentAttempt> AssessmentAttempts => Set<AssessmentAttempt>();
    public DbSet<AssessmentSubmission> AssessmentSubmissions => Set<AssessmentSubmission>();
    public DbSet<AssessmentAnswer> AssessmentAnswers => Set<AssessmentAnswer>();
    public DbSet<GradeRecord> GradeRecords => Set<GradeRecord>();
    public DbSet<GradeAppeal> GradeAppeals => Set<GradeAppeal>();
    public DbSet<GradeChangeHistory> GradeChangeHistories => Set<GradeChangeHistory>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<WaitlistEntry> WaitlistEntries { get; set; }
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

     public DbSet<RefreshToken> RefreshTokens { get; set; }


    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.ApplyConfigurationsFromAssembly(typeof(TmsDbContext).Assembly);
    
    }

    
}
