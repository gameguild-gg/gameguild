using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260711053000_AddSupportTicketPersistence")]
public partial class AddSupportTicketPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SupportTickets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ReporterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                ReporterName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                ReporterEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                Subject = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                Priority = table.Column<int>(type: "integer", nullable: false),
                AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                AssignedToName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                FirstResponseAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ResponseDueBy = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ResolutionSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastMessagePreview = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_SupportTickets", x => x.Id));

        migrationBuilder.CreateTable(
            name: "SupportTicketMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                AuthorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                AuthorEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                AuthorType = table.Column<int>(type: "integer", nullable: false),
                Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupportTicketMessages", x => x.Id);
                table.ForeignKey(
                    name: "FK_SupportTicketMessages_SupportTickets_TicketId",
                    column: x => x.TicketId,
                    principalTable: "SupportTickets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SupportTicketMessages_TenantId_TicketId",
            table: "SupportTicketMessages",
            columns: new[] { "TenantId", "TicketId" });
        migrationBuilder.CreateIndex(
            name: "IX_SupportTicketMessages_TicketId",
            table: "SupportTicketMessages",
            column: "TicketId");
        migrationBuilder.CreateIndex(
            name: "IX_SupportTickets_TenantId_CustomerId",
            table: "SupportTickets",
            columns: new[] { "TenantId", "CustomerId" });
        migrationBuilder.CreateIndex(
            name: "IX_SupportTickets_TenantId_Priority",
            table: "SupportTickets",
            columns: new[] { "TenantId", "Priority" });
        migrationBuilder.CreateIndex(
            name: "IX_SupportTickets_TenantId_Status",
            table: "SupportTickets",
            columns: new[] { "TenantId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SupportTicketMessages");
        migrationBuilder.DropTable(name: "SupportTickets");
    }
}
