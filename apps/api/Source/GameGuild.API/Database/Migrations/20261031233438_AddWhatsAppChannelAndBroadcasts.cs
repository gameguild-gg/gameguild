using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppChannelAndBroadcasts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppEnabled",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "WhatsAppBroadcasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TemplateName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    VariablesJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    SegmentRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SegmentActiveMembersOnly = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecipientCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppBroadcasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppBroadcastRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BroadcastId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppBroadcastRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppBroadcastRecipients_WhatsAppBroadcasts_BroadcastId",
                        column: x => x.BroadcastId,
                        principalTable: "WhatsAppBroadcasts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppBroadcastRecipients_BroadcastId",
                table: "WhatsAppBroadcastRecipients",
                column: "BroadcastId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppBroadcastRecipients_BroadcastId_Status",
                table: "WhatsAppBroadcastRecipients",
                columns: new[] { "BroadcastId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppBroadcastRecipients_UserId",
                table: "WhatsAppBroadcastRecipients",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppBroadcasts_ScheduledAtUtc",
                table: "WhatsAppBroadcasts",
                column: "ScheduledAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppBroadcasts_TenantId_CreatedAt",
                table: "WhatsAppBroadcasts",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppBroadcasts_TenantId_Status",
                table: "WhatsAppBroadcasts",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatsAppBroadcastRecipients");

            migrationBuilder.DropTable(
                name: "WhatsAppBroadcasts");

            migrationBuilder.DropColumn(
                name: "WhatsAppEnabled",
                table: "NotificationPreferences");
        }
    }
}
