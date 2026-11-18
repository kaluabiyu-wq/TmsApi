using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TmsApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Addreports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Students",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "ReportDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedReports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Format = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FiltersJson = table.Column<string>(type: "jsonb", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    StoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedReports_ReportDefinitions_ReportDefinitionId",
                        column: x => x.ReportDefinitionId,
                        principalTable: "ReportDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_ReportDefinitionId",
                table: "GeneratedReports",
                column: "ReportDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_RequestedByUserId_RequestedAt",
                table: "GeneratedReports",
                columns: new[] { "RequestedByUserId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_Status_RequestedAt",
                table: "GeneratedReports",
                columns: new[] { "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_Key",
                table: "ReportDefinitions",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneratedReports");

            migrationBuilder.DropTable(
                name: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Students");
        }
    }
}
