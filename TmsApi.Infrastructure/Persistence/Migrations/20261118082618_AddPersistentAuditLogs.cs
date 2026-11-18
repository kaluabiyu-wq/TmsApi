
using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TmsApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the persistent audit log table.
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(
                        type: "bigint",
                        nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    UserId = table.Column<string>(
                        type: "character varying(450)",
                        maxLength: 450,
                        nullable: false),

                    Action = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: false),

                    EntityName = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: false),

                    EntityId = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false),

                    OldValues = table.Column<string>(
                        type: "jsonb",
                        nullable: true),

                    NewValues = table.Column<string>(
                        type: "jsonb",
                        nullable: true),

                    IpAddress = table.Column<string>(
                        type: "character varying(64)",
                        maxLength: 64,
                        nullable: true),

                    Timestamp = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),

                    CorrelationId = table.Column<string>(
                        type: "character varying(128)",
                        maxLength: 128,
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_AuditLogs",
                        x => x.Id);
                });

            // Audit log indexes.
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId_Timestamp",
                table: "AuditLogs",
                columns: new[]
                {
                    "EntityName",
                    "EntityId",
                    "Timestamp"
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs",
                columns: new[]
                {
                    "UserId",
                    "Timestamp"
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback removes only the audit log table.
            migrationBuilder.DropTable(
                name: "AuditLogs");
        }
    }
}
