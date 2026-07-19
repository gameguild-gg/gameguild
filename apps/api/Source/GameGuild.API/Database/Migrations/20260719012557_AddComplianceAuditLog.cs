using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260719012557_AddComplianceAuditLog")]
public partial class AddComplianceAuditLog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ResourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Metadata = table.Column<string>(type: "text", nullable: true),
                Success = table.Column<bool>(type: "boolean", nullable: false),
                ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                RiskLevel = table.Column<int>(type: "integer", nullable: false),
                Category = table.Column<int>(type: "integer", nullable: false),
                CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_ActionType",
            table: "AuditLogs",
            column: "ActionType");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_CreatedAt",
            table: "AuditLogs",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_ResourceId",
            table: "AuditLogs",
            column: "ResourceId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_ResourceType",
            table: "AuditLogs",
            column: "ResourceType");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_TenantId",
            table: "AuditLogs",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_TenantId_CreatedAt",
            table: "AuditLogs",
            columns: new[] { "TenantId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_UserId",
            table: "AuditLogs",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuditLogs");
    }
}
