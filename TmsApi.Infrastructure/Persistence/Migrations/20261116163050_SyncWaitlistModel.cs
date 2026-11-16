using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmsApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncWaitlistModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntry_Courses_CourseId",
                table: "WaitlistEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntry_Students_StudentId",
                table: "WaitlistEntry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WaitlistEntry",
                table: "WaitlistEntry");

            migrationBuilder.RenameTable(
                name: "WaitlistEntry",
                newName: "WaitlistEntries");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WaitlistEntries",
                table: "WaitlistEntries",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WaitlistEntries_Courses_CourseId",
                table: "WaitlistEntries",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitlistEntries_Students_StudentId",
                table: "WaitlistEntries",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntries_Courses_CourseId",
                table: "WaitlistEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntries_Students_StudentId",
                table: "WaitlistEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WaitlistEntries",
                table: "WaitlistEntries");

            migrationBuilder.RenameTable(
                name: "WaitlistEntries",
                newName: "WaitlistEntry");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WaitlistEntry",
                table: "WaitlistEntry",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WaitlistEntry_Courses_CourseId",
                table: "WaitlistEntry",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitlistEntry_Students_StudentId",
                table: "WaitlistEntry",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
