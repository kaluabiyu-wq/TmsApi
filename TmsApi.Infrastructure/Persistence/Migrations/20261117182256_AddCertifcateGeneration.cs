
using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TmsApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCertifcateGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Replace the old GradeAppeals and GradeRecords tables.
            // Both tables were confirmed empty in the deployed database.
            migrationBuilder.DropTable(
                name: "GradeAppeals");

            migrationBuilder.DropTable(
                name: "GradeRecords");

            // Remove existing certificate foreign keys.
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Courses_CourseId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Students_StudentId",
                table: "Certificates");

            // Remove indexes that will be recreated.
            migrationBuilder.DropIndex(
                name: "UX_WaitlistEntries_ActiveStudentCourse",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_StudentId",
                table: "Certificates");

            migrationBuilder.Sql(
                """DROP INDEX IF EXISTS "IX_Assessments_CourseId";""");

            // Update certificate serial number.
            migrationBuilder.AlterColumn<string>(
                name: "SerialNumber",
                table: "Certificates",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            // Add certificate columns.
            migrationBuilder.AddColumn<string>(
                name: "CertificateUrl",
                table: "Certificates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuedBy",
                table: "Certificates",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RevocationReason",
                table: "Certificates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "Certificates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Certificates",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "Certificates",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TemplateId",
                table: "Certificates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationCode",
                table: "Certificates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Certificate issuance logs.
            migrationBuilder.CreateTable(
                name: "CertificateIssuanceLogs",
                columns: table => new
                {
                    Id = table.Column<long>(
                            type: "bigint",
                            nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    CertificateId = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    Action = table.Column<string>(
                        type: "character varying(40)",
                        maxLength: 40,
                        nullable: false),

                    PerformedBy = table.Column<string>(
                        type: "character varying(450)",
                        maxLength: 450,
                        nullable: false),

                    Details = table.Column<string>(
                        type: "text",
                        nullable: true),

                    IpAddress = table.Column<string>(
                        type: "text",
                        nullable: true),

                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_CertificateIssuanceLogs",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_CertificateIssuanceLogs_Certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "Certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Certificate templates.
            migrationBuilder.CreateTable(
                name: "CertificateTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(
                            type: "integer",
                            nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    Name = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: false),

                    InstitutionName = table.Column<string>(
                        type: "character varying(250)",
                        maxLength: 250,
                        nullable: false),

                    LogoPath = table.Column<string>(
                        type: "text",
                        nullable: true),

                    SignatureName = table.Column<string>(
                        type: "text",
                        nullable: true),

                    SignatureTitle = table.Column<string>(
                        type: "text",
                        nullable: true),

                    CertificateTitle = table.Column<string>(
                        type: "text",
                        nullable: false),

                    BodyText = table.Column<string>(
                        type: "text",
                        nullable: false),

                    MinimumFinalGrade = table.Column<decimal>(
                        type: "numeric(5,2)",
                        precision: 5,
                        scale: 2,
                        nullable: false),

                    MinimumAttendancePercent = table.Column<decimal>(
                        type: "numeric(5,2)",
                        precision: 5,
                        scale: 2,
                        nullable: false),

                    RequireCompletedEnrollment = table.Column<bool>(
                        type: "boolean",
                        nullable: false),

                    RequireAllPublishedAssessments = table.Column<bool>(
                        type: "boolean",
                        nullable: false),

                    IsActive = table.Column<bool>(
                        type: "boolean",
                        nullable: false),

                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),

                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_CertificateTemplates",
                        x => x.Id);
                });

            // New GradeRecords table.
            migrationBuilder.CreateTable(
                name: "GradeRecords",
                columns: table => new
                {
                    Id = table.Column<int>(
                            type: "integer",
                            nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    StudentId = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    CourseId = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    AssessmentId = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    SubmissionId = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    Score = table.Column<decimal>(
                        type: "numeric(10,2)",
                        precision: 10,
                        scale: 2,
                        nullable: false),

                    MaxScore = table.Column<decimal>(
                        type: "numeric(10,2)",
                        precision: 10,
                        scale: 2,
                        nullable: false),

                    Percentage = table.Column<decimal>(
                        type: "numeric(7,2)",
                        precision: 7,
                        scale: 2,
                        nullable: false),

                    Weight = table.Column<decimal>(
                        type: "numeric(7,2)",
                        precision: 7,
                        scale: 2,
                        nullable: false),

                    IsPublished = table.Column<bool>(
                        type: "boolean",
                        nullable: false),

                    PublishedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true),

                    PublishedByUserId = table.Column<string>(
                        type: "text",
                        nullable: true),

                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),

                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_GradeRecords",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_GradeRecords_AssessmentSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "AssessmentSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_GradeRecords_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_GradeRecords_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_GradeRecords_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            // New GradeAppeals table.
            migrationBuilder.CreateTable(
                name: "GradeAppeals",
                columns: table => new
                {
                    Id = table.Column<int>(
                            type: "integer",
                            nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    GradeRecordId = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    StudentId = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    Reason = table.Column<string>(
                        type: "character varying(4000)",
                        maxLength: 4000,
                        nullable: false),

                    Status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false),

                    ReviewerUserId = table.Column<string>(
                        type: "text",
                        nullable: true),

                    ReviewComment = table.Column<string>(
                        type: "character varying(4000)",
                        maxLength: 4000,
                        nullable: true),

                    SubmittedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),

                    ReviewedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_GradeAppeals",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_GradeAppeals_GradeRecords_GradeRecordId",
                        column: x => x.GradeRecordId,
                        principalTable: "GradeRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_GradeAppeals_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            // Waitlist index.
            migrationBuilder.CreateIndex(
                name: "UX_WaitlistEntries_ActiveStudentCourse",
                table: "WaitlistEntries",
                columns: new[] { "StudentId", "CourseId" },
                unique: true,
                filter: "\"Status\" IN ('Waiting','Offered')");

            // Certificate indexes.
            migrationBuilder.CreateIndex(
                name: "IX_Certificates_SerialNumber",
                table: "Certificates",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_StudentId_CourseId",
                table: "Certificates",
                columns: new[] { "StudentId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_TemplateId",
                table: "Certificates",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_VerificationCode",
                table: "Certificates",
                column: "VerificationCode",
                unique: true);

            

            // Certificate log and template indexes.
            migrationBuilder.CreateIndex(
                name: "IX_CertificateIssuanceLogs_CertificateId_CreatedAt",
                table: "CertificateIssuanceLogs",
                columns: new[] { "CertificateId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateTemplates_IsActive_Name",
                table: "CertificateTemplates",
                columns: new[] { "IsActive", "Name" });

            // Grade appeal indexes.
            migrationBuilder.CreateIndex(
                name: "IX_GradeAppeals_GradeRecordId_Status",
                table: "GradeAppeals",
                columns: new[] { "GradeRecordId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeAppeals_StudentId",
                table: "GradeAppeals",
                column: "StudentId");

            // Grade record indexes.
            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_AssessmentId",
                table: "GradeRecords",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_CourseId",
                table: "GradeRecords",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_StudentId_AssessmentId",
                table: "GradeRecords",
                columns: new[] { "StudentId", "AssessmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_SubmissionId",
                table: "GradeRecords",
                column: "SubmissionId");

            // Certificate foreign keys.
            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_CertificateTemplates_TemplateId",
                table: "Certificates",
                column: "TemplateId",
                principalTable: "CertificateTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Courses_CourseId",
                table: "Certificates",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Students_StudentId",
                table: "Certificates",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the new certificate foreign keys.
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_CertificateTemplates_TemplateId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Courses_CourseId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Students_StudentId",
                table: "Certificates");

            // Drop tables introduced by this migration.
            migrationBuilder.DropTable(
                name: "CertificateIssuanceLogs");

            migrationBuilder.DropTable(
                name: "CertificateTemplates");

            migrationBuilder.DropTable(
                name: "GradeAppeals");

            migrationBuilder.DropTable(
                name: "GradeRecords");

            // Remove indexes introduced by this migration.
            migrationBuilder.DropIndex(
                name: "UX_WaitlistEntries_ActiveStudentCourse",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_SerialNumber",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_StudentId_CourseId",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_TemplateId",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_VerificationCode",
                table: "Certificates");


            // Remove certificate columns.
            migrationBuilder.DropColumn(
                name: "CertificateUrl",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "IssuedBy",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "RevocationReason",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "VerificationCode",
                table: "Certificates");

            // Restore the original SerialNumber column type.
            migrationBuilder.AlterColumn<string>(
                name: "SerialNumber",
                table: "Certificates",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);

            // Restore the original GradeRecords table.
            migrationBuilder.CreateTable(
                name: "GradeRecords",
                columns: table => new
                {
                    Id = table.Column<int>(
                            type: "integer",
                            nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    AssessmentId = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    EnrollmentId = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    Score = table.Column<decimal>(
                        type: "numeric",
                        nullable: false),

                    MaxScore = table.Column<decimal>(
                        type: "numeric",
                        nullable: false),

                    IsPublished = table.Column<bool>(
                        type: "boolean",
                        nullable: false),

                    PublishedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true),

                    Feedback = table.Column<string>(
                        type: "text",
                        nullable: true),

                    GradedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),

                    GradedBy = table.Column<string>(
                        type: "text",
                        nullable: false,
                        defaultValue: ""),

                    IsManualOverride = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: false),

                    WeightedScore = table.Column<decimal>(
                        type: "numeric",
                        nullable: false,
                        defaultValue: 0.0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_GradeRecords",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_GradeRecords_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_GradeRecords_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            // Restore the original GradeAppeals table.
            migrationBuilder.CreateTable(
                name: "GradeAppeals",
                columns: table => new
                {
                    Id = table.Column<int>(
                            type: "integer",
                            nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    EnrollmentId = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    StudentId = table.Column<string>(
                        type: "character varying",
                        nullable: false),

                    Reason = table.Column<string>(
                        type: "character varying",
                        nullable: false),

                    Status = table.Column<string>(
                        type: "character varying",
                        nullable: false),

                    Response = table.Column<string>(
                        type: "character varying",
                        nullable: true),

                    SubmittedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),

                    ReviewedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true),

                    ReviewedBy = table.Column<string>(
                        type: "character varying",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_GradeAppeals",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_GradeAppeals_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            // Restore original GradeRecords indexes.
            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_AssessmentId",
                table: "GradeRecords",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeRecords_EnrollmentId",
                table: "GradeRecords",
                column: "EnrollmentId");

            // Restore original GradeAppeals indexes.
            migrationBuilder.CreateIndex(
                name: "IX_GradeAppeals_EnrollmentId",
                table: "GradeAppeals",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeAppeals_EnrollmentId_Status",
                table: "GradeAppeals",
                columns: new[] { "EnrollmentId", "Status" });

            // Restore original Waitlist index.
            migrationBuilder.CreateIndex(
                name: "UX_WaitlistEntries_ActiveStudentCourse",
                table: "WaitlistEntries",
                columns: new[] { "StudentId", "CourseId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            // Restore original certificate and assessment indexes.
            migrationBuilder.CreateIndex(
                name: "IX_Certificates_StudentId",
                table: "Certificates",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_CourseId",
                table: "Assessments",
                column: "CourseId");

            // Restore original certificate foreign keys.
            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Courses_CourseId",
                table: "Certificates",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Students_StudentId",
                table: "Certificates",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
