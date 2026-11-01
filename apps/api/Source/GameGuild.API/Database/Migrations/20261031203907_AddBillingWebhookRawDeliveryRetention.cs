using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingWebhookRawDeliveryRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "billing_webhook_raw_deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingWebhookEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalEventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RawPayload = table.Column<string>(type: "text", nullable: false),
                    RawHeaders = table.Column<string>(type: "text", nullable: false),
                    PayloadSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PiiRedacted = table.Column<bool>(type: "boolean", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billing_webhook_raw_deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_billing_webhook_raw_deliveries_event",
                        column: x => x.BillingWebhookEventId,
                        principalTable: "billing_webhook_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_billing_webhook_raw_deliveries_event_id",
                table: "billing_webhook_raw_deliveries",
                column: "BillingWebhookEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_billing_webhook_raw_deliveries_expires_at",
                table: "billing_webhook_raw_deliveries",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "ix_billing_webhook_raw_deliveries_received_at",
                table: "billing_webhook_raw_deliveries",
                column: "ReceivedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "billing_webhook_raw_deliveries");
        }
    }
}
