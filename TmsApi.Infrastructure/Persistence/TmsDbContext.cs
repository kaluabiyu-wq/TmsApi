

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

    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<CertificateTemplate> CertificateTemplates => Set<CertificateTemplate>();
    public DbSet<CertificateIssuanceLog> CertificateIssuanceLogs => Set<CertificateIssuanceLog>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<CourseSchedule> CourseSchedules => Set<CourseSchedule>();
    public DbSet<WaitlistEntry> WaitlistEntries { get; set; }
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<GeneratedReport> GeneratedReports => Set<GeneratedReport>();
    public DbSet<TrainingProgram> TrainingPrograms => Set<TrainingProgram>();
    public DbSet<ProgramCourse> ProgramCourses => Set<ProgramCourse>();
    public DbSet<StudentProgramEnrollment> StudentProgramEnrollments => Set<StudentProgramEnrollment>();
    public DbSet<StudentProgress> StudentProgress => Set<StudentProgress>();

     public DbSet<RefreshToken> RefreshTokens { get; set; }


    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.ApplyConfigurationsFromAssembly(typeof(TmsDbContext).Assembly);
        b.Entity<TrainingProgram>(entity =>
        {
            entity.ToTable("TrainingPrograms");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.HasIndex(x => x.Code).IsUnique();
        });
        b.Entity<ProgramCourse>(entity =>
        {
            entity.ToTable("ProgramCourses");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Program).WithMany(x => x.ProgramCourses).HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.ProgramId, x.CourseId }).IsUnique();
            entity.HasIndex(x => new { x.ProgramId, x.Sequence }).IsUnique();
        });
        b.Entity<StudentProgramEnrollment>(entity =>
        {
            entity.ToTable("StudentProgramEnrollments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Program).WithMany(x => x.StudentEnrollments).HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.StudentId, x.ProgramId }).IsUnique();
        });
        b.Entity<StudentProgress>(entity =>
        {
            entity.ToTable("StudentProgress");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.HasOne(x => x.StudentProgramEnrollment).WithMany(x => x.ProgressItems).HasForeignKey(x => x.StudentProgramEnrollmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ProgramCourse).WithMany(x => x.StudentProgress).HasForeignKey(x => x.ProgramCourseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.StudentProgramEnrollmentId, x.ProgramCourseId }).IsUnique();
        });

        b.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(150).IsRequired();
            entity.Property(x => x.EntityName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OldValues).HasColumnType("jsonb");
            entity.Property(x => x.NewValues).HasColumnType("jsonb");
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
            entity.Property(x => x.Timestamp).HasColumnType("timestamp with time zone").IsRequired();
            entity.HasIndex(x => x.Timestamp);
            entity.HasIndex(x => new { x.UserId, x.Timestamp });
            entity.HasIndex(x => new { x.EntityName, x.EntityId, x.Timestamp });
            entity.HasIndex(x => x.Action);
        });
    
    }

    
}
