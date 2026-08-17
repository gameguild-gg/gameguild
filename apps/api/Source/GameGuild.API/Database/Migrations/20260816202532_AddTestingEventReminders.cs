using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingEventReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReminderDaysBefore",
                table: "testing_lab_settings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReminderDaysBeforeOverride",
                table: "testing_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SentReminderDays",
                table: "testing_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderDaysBefore",
                table: "testing_lab_settings");

            migrationBuilder.DropColumn(
                name: "ReminderDaysBeforeOverride",
                table: "testing_events");

            migrationBuilder.DropColumn(
                name: "SentReminderDays",
                table: "testing_events");
        }
    }
}
