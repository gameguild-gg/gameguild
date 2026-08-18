using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationEmailDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "RecipientId",
                table: "Notifications",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "Notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryStatus",
                table: "Notifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "Notifications",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientEmail",
                table: "Notifications",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Channel_DeliveryStatus_NextAttemptAt",
                table: "Notifications",
                columns: new[] { "Channel", "DeliveryStatus", "NextAttemptAt" });

            // Backfill before the column goes NOT NULL: already-delivered rows become Sent,
            // everything else Pending. Existing rows are InApp (zero Email-channel producers today).
            migrationBuilder.Sql("""
                UPDATE "Notifications"
                SET "DeliveryStatus" = CASE WHEN "IsSent" THEN 'Sent' ELSE 'Pending' END
                WHERE "DeliveryStatus" IS NULL
                """);

            migrationBuilder.AlterColumn<string>(
                name: "DeliveryStatus",
                table: "Notifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_Channel_DeliveryStatus_NextAttemptAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RecipientEmail",
                table: "Notifications");

            migrationBuilder.AlterColumn<Guid>(
                name: "RecipientId",
                table: "Notifications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
