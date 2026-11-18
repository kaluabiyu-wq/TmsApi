using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TmsApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDetailedGradebook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GradeAppeals_GradeRecords_GradeRecordId",
                table: "GradeAppeals");

            migrationBuilder.DropForeignKey(
                name: "FK_GradeAppeals_Students_StudentId",
                table: "GradeAppeals");

            migrationBuilder.DropForeignKey(
                name: "FK_GradeRecords_AssessmentSubmissions_SubmissionId",
                table: "GradeRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_GradeRecords_Assessments_AssessmentId",
                table: "GradeRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_GradeRecords_Courses_CourseId",
                table: "GradeRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_GradeRecords_Students_StudentId",
                table: "GradeRecords");

            migrationBuilder.DropIndex(
                name: "IX_GradeRecords_CourseId",
                table: "GradeRecords");

            migrationBuilder.DropIndex(
                name: "IX_GradeRecords_StudentId_AssessmentId",
                table: "GradeRecords");

            migrationBuilder.DropIndex(
                name: "IX_GradeAppeals_StudentId",
                table: "GradeAppeals");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "Percentage",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "ReviewerUserId",
                table: "GradeAppeals");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "GradeRecords",
                newName: "GradedAt");

            migrationBuilder.RenameColumn(
                name: "SubmissionId",
                table: "GradeRecords",
                newName: "EnrollmentId");

            migrationBuilder.RenameColumn(
                name: "PublishedByUserId",
                table: "GradeRecords",
                newName: "Feedback");

            migrationBuilder.RenameIndex(
                name: "IX_GradeRecords_SubmissionId",
                table: "GradeRecords",
                newName: "IX_GradeRecords_EnrollmentId");

            migrationBuilder.RenameColumn(
                name: "ReviewComment",
                table: "GradeAppeals",
                newName: "Response");

            migrationBuilder.RenameColumn(
                name: "GradeRecordId",
                table: "GradeAppeals",
                newName: "EnrollmentId");

            migrationBuilder.RenameIndex(
                name: "IX_GradeAppeals_GradeRecordId_Status",
                table: "GradeAppeals",
                newName: "IX_GradeAppeals_EnrollmentId_Status");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "GradeRecords",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxScore",
                table: "GradeRecords",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "GradedBy",
                table: "GradeRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsManualOverride",
                table: "GradeRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightedScore",
                table: "GradeRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "GradeAppeals",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "GradeAppeals",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "GradeAppeals",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GradeChangeHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GradeRecordId = table.Column<int>(type: "integer", nullable: false),
                    EnrollmentId = table.Column<int>(type: "integer", nullable: false),
                    AssessmentId = table.Column<int>(type: "integer", nullable: false),
                    PreviousScore = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    NewScore = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PreviousFinalGrade = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    NewFinalGrade = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Reason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AppealId = table.Column<int>(type: "integer", nullable: true),
                    ChangedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeChangeHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GradeAppeals_EnrollmentId",
                table: "GradeAppeals",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeChangeHistories_EnrollmentId",
                table: "GradeChangeHistories",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeChangeHistories_GradeRecordId",
                table: "GradeChangeHistories",
                column: "GradeRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_GradeAppeals_Enrollments_EnrollmentId",
                table: "GradeAppeals",
                column: "EnrollmentId",
                principalTable: "Enrollments",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeRecords_Assessments_AssessmentId",
                table: "GradeRecords",
                column: "AssessmentId",
                principalTable: "Assessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeRecords_Enrollments_EnrollmentId",
                table: "GradeRecords",
                column: "EnrollmentId",
                principalTable: "Enrollments",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GradeAppeals_Enrollments_EnrollmentId",
                table: "GradeAppeals");

            migrationBuilder.DropForeignKey(
                name: "FK_GradeRecords_Assessments_AssessmentId",
                table: "GradeRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_GradeRecords_Enrollments_EnrollmentId",
                table: "GradeRecords");

            migrationBuilder.DropTable(
                name: "GradeChangeHistories");

            migrationBuilder.DropIndex(
                name: "IX_GradeAppeals_EnrollmentId",
                table: "GradeAppeals");

            migrationBuilder.DropColumn(
                name: "GradedBy",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "IsManualOverride",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "WeightedScore",
                table: "GradeRecords");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "GradeAppeals");

            migrationBuilder.RenameColumn(
                name: "GradedAt",
                table: "GradeRecords",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Feedback",
                table: "GradeRecords",
                newName: "PublishedByUserId");

            migrationBuilder.RenameColumn(
                name: "EnrollmentId",
                table: "GradeRecords",
                newName: "SubmissionId");

            migrationBuilder.RenameIndex(
                name: "IX_GradeRecords_EnrollmentId",
                table: "GradeRecords",
                newName: "IX_GradeRecords_SubmissionId");

            migrationBuilder.RenameColumn(
                name: "Response",
                table: "GradeAppeals",
                newName: "ReviewComment");

            migrationBuilder.RenameColumn(
                name: "EnrollmentId",
                table: "GradeAppeals",
                newName: "GradeRecordId");

            migrationBuilder.RenameIndex(
                name: "IX_GradeAppeals_EnrollmentId_Status",
                table: "GradeAppeals",
                newName: "IX_GradeAppeals_GradeRecordId_Status");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "GradeRecords",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxScore",
                table: "GradeRecords",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "GradeRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "GradeRecords",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "Percentage",
                table: "GradeRecords",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StudentId",
                table: "GradeRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "GradeRecords",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "GradeAppeals",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "GradeAppeals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<string>(
                name: "ReviewerUserId",
                table: "GradeAppeals",
                type: "text",
                nullable: true);

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
                name: "IX_GradeAppeals_StudentId",
                table: "GradeAppeals",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_GradeAppeals_GradeRecords_GradeRecordId",
                table: "GradeAppeals",
                column: "GradeRecordId",
                principalTable: "GradeRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeAppeals_Students_StudentId",
                table: "GradeAppeals",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeRecords_AssessmentSubmissions_SubmissionId",
                table: "GradeRecords",
                column: "SubmissionId",
                principalTable: "AssessmentSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeRecords_Assessments_AssessmentId",
                table: "GradeRecords",
                column: "AssessmentId",
                principalTable: "Assessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeRecords_Courses_CourseId",
                table: "GradeRecords",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeRecords_Students_StudentId",
                table: "GradeRecords",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
