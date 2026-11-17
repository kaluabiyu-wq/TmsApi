
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmsApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncAssessmentWorkflowModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_WaitlistEntries_ActiveStudentCourse",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_Assessments_CourseId",
                table: "Assessments");

            migrationBuilder.CreateIndex(
                name: "UX_WaitlistEntries_ActiveStudentCourse",
                table: "WaitlistEntries",
                columns: new[] { "StudentId", "CourseId" },
                unique: true,
                filter: "\"Status\" IN ('Waiting','Offered')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_WaitlistEntries_ActiveStudentCourse",
                table: "WaitlistEntries");

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
