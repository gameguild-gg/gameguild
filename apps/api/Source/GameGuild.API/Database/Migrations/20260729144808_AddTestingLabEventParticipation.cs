using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingLabEventParticipation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TestingRequestId",
                table: "testing_feedback",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "FeedbackFormId",
                table: "testing_feedback",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationId",
                table: "testing_feedback",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "testing_feedback",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "testing_slot_registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    WaitlistPosition = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PromotedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckedInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckedOutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_slot_registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_slot_registrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_slot_registrations_testing_event_slots_SlotId",
                        column: x => x.SlotId,
                        principalTable: "testing_event_slots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_testing_slot_registrations_testing_events_EventId",
                        column: x => x.EventId,
                        principalTable: "testing_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_ApplicationId",
                table: "testing_feedback",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_EventId_ApplicationId_UserId",
                table: "testing_feedback",
                columns: new[] { "EventId", "ApplicationId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_testing_slot_registrations_active_slot_user",
                table: "testing_slot_registrations",
                columns: new[] { "SlotId", "UserId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"Status\" <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_testing_slot_registrations_EventId",
                table: "testing_slot_registrations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_slot_registrations_SlotId_Status",
                table: "testing_slot_registrations",
                columns: new[] { "SlotId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_testing_slot_registrations_TenantId",
                table: "testing_slot_registrations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_slot_registrations_UserId",
                table: "testing_slot_registrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_slot_registrations_waitlist_position",
                table: "testing_slot_registrations",
                columns: new[] { "SlotId", "WaitlistPosition" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"Status\" = 'Waitlisted'");

            migrationBuilder.AddForeignKey(
                name: "FK_testing_feedback_testing_events_EventId",
                table: "testing_feedback",
                column: "EventId",
                principalTable: "testing_events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_testing_feedback_testing_project_applications_ApplicationId",
                table: "testing_feedback",
                column: "ApplicationId",
                principalTable: "testing_project_applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM testing_feedback
                WHERE "EventId" IS NOT NULL
                   OR "ApplicationId" IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_testing_feedback_testing_events_EventId",
                table: "testing_feedback");

            migrationBuilder.DropForeignKey(
                name: "FK_testing_feedback_testing_project_applications_ApplicationId",
                table: "testing_feedback");

            migrationBuilder.DropTable(
                name: "testing_slot_registrations");

            migrationBuilder.DropIndex(
                name: "IX_testing_feedback_ApplicationId",
                table: "testing_feedback");

            migrationBuilder.DropIndex(
                name: "IX_testing_feedback_EventId_ApplicationId_UserId",
                table: "testing_feedback");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "testing_feedback");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "testing_feedback");

            migrationBuilder.AlterColumn<Guid>(
                name: "TestingRequestId",
                table: "testing_feedback",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FeedbackFormId",
                table: "testing_feedback",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
