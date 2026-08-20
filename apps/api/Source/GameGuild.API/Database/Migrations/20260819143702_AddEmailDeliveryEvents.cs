using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDeliveryEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                table: "Notifications",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequeueCount",
                table: "Notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EmailDeliveryEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    EventType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BounceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DiagnosticCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SnsMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailDeliveryEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailSuppressions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Reason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BounceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    SuppressedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSuppressions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ProviderMessageId",
                table: "Notifications",
                column: "ProviderMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryEvents_ProviderMessageId",
                table: "EmailDeliveryEvents",
                column: "ProviderMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryEvents_SnsMessageId",
                table: "EmailDeliveryEvents",
                column: "SnsMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailSuppressions_EmailAddress",
                table: "EmailSuppressions",
                column: "EmailAddress",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailDeliveryEvents");

            migrationBuilder.DropTable(
                name: "EmailSuppressions");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ProviderMessageId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RequeueCount",
                table: "Notifications");
        }
    }
}
