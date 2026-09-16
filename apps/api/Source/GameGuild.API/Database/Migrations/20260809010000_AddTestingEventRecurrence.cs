using System;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260809010000_AddTestingEventRecurrence")]
public partial class AddTestingEventRecurrence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "RecurrenceDaysOfWeek",
            table: "testing_events",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "RecurrenceEndsAt",
            table: "testing_events",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RecurrenceFrequency",
            table: "testing_events",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "RecurrenceInterval",
            table: "testing_events",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "RecurrenceOccurrence",
            table: "testing_events",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "RecurrenceOccurrenceCount",
            table: "testing_events",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "RecurrenceSeriesId",
            table: "testing_events",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_testing_events_recurrence_series_occurrence",
            table: "testing_events",
            columns: new[] { "RecurrenceSeriesId", "RecurrenceOccurrence" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_testing_events_recurrence_series_occurrence",
            table: "testing_events");

        migrationBuilder.DropColumn(name: "RecurrenceDaysOfWeek", table: "testing_events");
        migrationBuilder.DropColumn(name: "RecurrenceEndsAt", table: "testing_events");
        migrationBuilder.DropColumn(name: "RecurrenceFrequency", table: "testing_events");
        migrationBuilder.DropColumn(name: "RecurrenceInterval", table: "testing_events");
        migrationBuilder.DropColumn(name: "RecurrenceOccurrence", table: "testing_events");
        migrationBuilder.DropColumn(name: "RecurrenceOccurrenceCount", table: "testing_events");
        migrationBuilder.DropColumn(name: "RecurrenceSeriesId", table: "testing_events");
    }
}
