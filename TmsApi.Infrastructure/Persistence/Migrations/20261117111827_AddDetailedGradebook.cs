using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TmsApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailedGradebook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_WaitlistEntries_ActiveStudentCourse",
                table: "WaitlistEntries");

           migrationBuilder.Sql(
           "DROP INDEX IF EXISTS \"IX_Assessments_CourseId\";");

            migrationBuilder.CreateTable(
                name: "GradeRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    AssessmentId = table.Column<int>(type: "integer", nullable: false),
                    SubmissionId = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeRecords", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "GradeAppeals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GradeRecordId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReviewerUserId = table.Column<string>(type: "text", nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeAppeals", x => x.Id);
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

            migrationBuilder.CreateIndex(
                name: "UX_WaitlistEntries_ActiveStudentCourse",
                table: "WaitlistEntries",
                columns: new[] { "StudentId", "CourseId" },
                unique: true,
                filter: "\"Status\" IN ('Waiting','Offered')");

           migrationBuilder.Sql(
                   """
           CREATE INDEX IF NOT EXISTS "IX_AssessmentAttempts_StudentId"
           ON "AssessmentAttempts" ("StudentId");
           """);

             migrationBuilder.Sql(
                            """
                  CREATE INDEX IF NOT EXISTS "IX_AssessmentAnswers_QuestionId"
                    ON "AssessmentAnswers" ("QuestionId");
             """);

            migrationBuilder.CreateIndex(
                name: "IX_GradeAppeals_GradeRecordId_Status",
                table: "GradeAppeals",
                columns: new[] { "GradeRecordId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeAppeals_StudentId",
                table: "GradeAppeals",
                column: "StudentId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GradeAppeals");

            migrationBuilder.DropTable(
                name: "GradeRecords");

            migrationBuilder.DropIndex(
                name: "UX_WaitlistEntries_ActiveStudentCourse",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentAttempts_StudentId",
                table: "AssessmentAttempts");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentAnswers_QuestionId",
                table: "AssessmentAnswers");

            migrationBuilder.CreateIndex(
                name: "UX_WaitlistEntries_ActiveStudentCourse",
                table: "WaitlistEntries",
                columns: new[] { "StudentId", "CourseId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_CourseId",
                table: "Assessments",
                column: "CourseId");
        }
    }
}
