using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingLabEventWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventSlotId",
                table: "testing_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "testing_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Mode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ApprovalMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ManagerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationsOpenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApplicationsCloseAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequiresFeedback = table.Column<bool>(type: "boolean", nullable: false),
                    LearningCompletionRequirement = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CohortId = table.Column<Guid>(type: "uuid", nullable: true),
                    LearningActivityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_events_Users_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "testing_committee_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsChair = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_committee_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_committee_members_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_committee_members_testing_events_EventId",
                        column: x => x.EventId,
                        principalTable: "testing_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "testing_event_slots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Mode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxTesters = table.Column<int>(type: "integer", nullable: true),
                    MaxProjects = table.Column<int>(type: "integer", nullable: true),
                    CampusName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RoomName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MeetingUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_event_slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_event_slots_testing_events_EventId",
                        column: x => x.EventId,
                        principalTable: "testing_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_testing_event_slots_testing_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "testing_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "testing_project_applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreferredAvailability = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AssignedSlotId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecisionRationale = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_project_applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_project_applications_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_project_applications_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_project_applications_project_versions_ProjectVersio~",
                        column: x => x.ProjectVersionId,
                        principalTable: "project_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_testing_project_applications_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_project_applications_testing_event_slots_AssignedSl~",
                        column: x => x.AssignedSlotId,
                        principalTable: "testing_event_slots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_project_applications_testing_events_EventId",
                        column: x => x.EventId,
                        principalTable: "testing_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "testing_application_votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_application_votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_application_votes_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_application_votes_testing_project_applications_Appl~",
                        column: x => x.ApplicationId,
                        principalTable: "testing_project_applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "testing_feedback_obligations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TesterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedbackId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FulfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_feedback_obligations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_feedback_obligations_Users_TesterUserId",
                        column: x => x.TesterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_feedback_obligations_testing_event_slots_SlotId",
                        column: x => x.SlotId,
                        principalTable: "testing_event_slots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_testing_feedback_obligations_testing_events_EventId",
                        column: x => x.EventId,
                        principalTable: "testing_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_testing_feedback_obligations_testing_feedback_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "testing_feedback",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_testing_feedback_obligations_testing_project_applications_A~",
                        column: x => x.ApplicationId,
                        principalTable: "testing_project_applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_testing_sessions_EventSlotId",
                table: "testing_sessions",
                column: "EventSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_application_votes_active_application_reviewer",
                table: "testing_application_votes",
                columns: new[] { "ApplicationId", "ReviewerId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_testing_application_votes_ReviewerId",
                table: "testing_application_votes",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_application_votes_TenantId",
                table: "testing_application_votes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_committee_members_active_event_user",
                table: "testing_committee_members",
                columns: new[] { "EventId", "UserId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_testing_committee_members_TenantId",
                table: "testing_committee_members",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_committee_members_UserId",
                table: "testing_committee_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_event_slots_EventId_StartsAt",
                table: "testing_event_slots",
                columns: new[] { "EventId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_testing_event_slots_LocationId",
                table: "testing_event_slots",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_event_slots_TenantId",
                table: "testing_event_slots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_events_ManagerUserId",
                table: "testing_events",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_events_TenantId",
                table: "testing_events",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_events_TenantId_Status_StartsAt",
                table: "testing_events",
                columns: new[] { "TenantId", "Status", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_obligations_active_assignment",
                table: "testing_feedback_obligations",
                columns: new[] { "SlotId", "ApplicationId", "TesterUserId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_obligations_ApplicationId",
                table: "testing_feedback_obligations",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_obligations_EventId",
                table: "testing_feedback_obligations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_obligations_FeedbackId",
                table: "testing_feedback_obligations",
                column: "FeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_obligations_Status",
                table: "testing_feedback_obligations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_obligations_TenantId",
                table: "testing_feedback_obligations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_obligations_TesterUserId",
                table: "testing_feedback_obligations",
                column: "TesterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_project_applications_active_event_project",
                table: "testing_project_applications",
                columns: new[] { "EventId", "ProjectId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"Status\" NOT IN ('Rejected', 'Withdrawn')");

            migrationBuilder.CreateIndex(
                name: "IX_testing_project_applications_AssignedSlotId",
                table: "testing_project_applications",
                column: "AssignedSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_project_applications_DecidedByUserId",
                table: "testing_project_applications",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_project_applications_EventId_Status",
                table: "testing_project_applications",
                columns: new[] { "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_testing_project_applications_ProjectId",
                table: "testing_project_applications",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_project_applications_ProjectVersionId",
                table: "testing_project_applications",
                column: "ProjectVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_project_applications_SubmittedByUserId",
                table: "testing_project_applications",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_project_applications_TenantId",
                table: "testing_project_applications",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_testing_sessions_testing_event_slots_EventSlotId",
                table: "testing_sessions",
                column: "EventSlotId",
                principalTable: "testing_event_slots",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_testing_sessions_testing_event_slots_EventSlotId",
                table: "testing_sessions");

            migrationBuilder.DropTable(
                name: "testing_application_votes");

            migrationBuilder.DropTable(
                name: "testing_committee_members");

            migrationBuilder.DropTable(
                name: "testing_feedback_obligations");

            migrationBuilder.DropTable(
                name: "testing_project_applications");

            migrationBuilder.DropTable(
                name: "testing_event_slots");

            migrationBuilder.DropTable(
                name: "testing_events");

            migrationBuilder.DropIndex(
                name: "IX_testing_sessions_EventSlotId",
                table: "testing_sessions");

            migrationBuilder.DropColumn(
                name: "EventSlotId",
                table: "testing_sessions");
        }
    }
}
