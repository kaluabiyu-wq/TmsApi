using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public sealed class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.HasMany(x => x.Questions).WithOne(x => x.Assessment).HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Attempts).WithOne(x => x.Assessment).HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CourseId, x.Status });
    }
}
public sealed class AssessmentQuestionConfiguration : IEntityTypeConfiguration<AssessmentQuestion>
{
    public void Configure(EntityTypeBuilder<AssessmentQuestion> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Prompt).HasMaxLength(4000).IsRequired();
        b.Property(x => x.QuestionType).HasMaxLength(30).IsRequired();
        b.Property(x => x.CorrectAnswer).HasMaxLength(4000);
        b.Property(x => x.Points).HasPrecision(10, 2);
        b.HasIndex(x => new { x.AssessmentId, x.SortOrder });
    }
}
public sealed class AssessmentAttemptConfiguration : IEntityTypeConfiguration<AssessmentAttempt>
{
    public void Configure(EntityTypeBuilder<AssessmentAttempt> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.HasIndex(x => new { x.AssessmentId, x.StudentId, x.AttemptNumber }).IsUnique();
        b.HasOne(x => x.Student).WithMany(x => x.AssessmentAttempts).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Submission).WithOne(x => x.Attempt).HasForeignKey<AssessmentSubmission>(x => x.AttemptId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class AssessmentSubmissionConfiguration : IEntityTypeConfiguration<AssessmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssessmentSubmission> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Score).HasPrecision(10, 2);
        b.Property(x => x.GradingStatus).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.SubmittedAt);
    }
}
public sealed class AssessmentAnswerConfiguration : IEntityTypeConfiguration<AssessmentAnswer>
{
    public void Configure(EntityTypeBuilder<AssessmentAnswer> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.AnswerText).HasMaxLength(8000);
        b.Property(x => x.AwardedPoints).HasPrecision(10, 2);
        b.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
        b.HasOne(x => x.Question).WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
    }
}
