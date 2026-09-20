using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TmsApi.Migrations
{
    /// <inheritdoc />
    public partial class FixStudentQueryFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Students",
                columns: new[] { "ID", "GPA", "IsActive", "IsDeleted", "Name", "RegistrationNumber" },
                values: new object[,]
                {
                    { 1, 3.8m, true, false, "Alice Smith", "TMS-2026-0001" },
                    { 2, 2.9m, true, false, "Bob Jones", "TMS-2026-0002" },
                    { 3, 3.4m, false, false, "Charlie Brown", "TMS-2026-0003" },
                    { 4, 3.9m, true, false, "Diana Prince", "TMS-2026-0004" },
                    { 5, 2.5m, true, false, "Evan Wright", "TMS-2026-0005" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "ID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "ID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "ID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "ID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Students",
                keyColumn: "ID",
                keyValue: 5);
        }
    }
}
