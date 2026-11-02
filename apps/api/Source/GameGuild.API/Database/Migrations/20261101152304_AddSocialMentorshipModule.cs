using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialMentorshipModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "social_mentor_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Headline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    PricingModel = table.Column<int>(type: "integer", nullable: false),
                    FreeContributionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BookingMode = table.Column<int>(type: "integer", nullable: false),
                    OffersIntroductoryCall = table.Column<bool>(type: "boolean", nullable: false),
                    IntroductoryCallMinutes = table.Column<int>(type: "integer", nullable: false),
                    TimezoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_mentor_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "social_mentor_availability_slots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MentorProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    MentorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_mentor_availability_slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_social_mentor_availability_slots_social_mentor_profiles_Men~",
                        column: x => x.MentorProfileId,
                        principalTable: "social_mentor_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "social_mentor_skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MentorProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Skill = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NormalizedSkill = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_mentor_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_social_mentor_skills_social_mentor_profiles_MentorProfileId",
                        column: x => x.MentorProfileId,
                        principalTable: "social_mentor_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "social_mentorship_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MentorProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    MentorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenteeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotId = table.Column<Guid>(type: "uuid", nullable: true),
                    Topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MeetingChannel = table.Column<int>(type: "integer", nullable: false),
                    MeetingUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsIntroductory = table.Column<bool>(type: "boolean", nullable: false),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_mentorship_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_social_mentorship_sessions_social_mentor_availability_slots~",
                        column: x => x.SlotId,
                        principalTable: "social_mentor_availability_slots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_social_mentor_availability_slots_MentorProfileId_StartsAt",
                table: "social_mentor_availability_slots",
                columns: new[] { "MentorProfileId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_social_mentor_availability_slots_MentorUserId",
                table: "social_mentor_availability_slots",
                column: "MentorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_social_mentor_availability_slots_SessionId",
                table: "social_mentor_availability_slots",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_social_mentor_availability_slots_Status_StartsAt",
                table: "social_mentor_availability_slots",
                columns: new[] { "Status", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_social_mentor_profiles_Status_Role",
                table: "social_mentor_profiles",
                columns: new[] { "Status", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_social_mentor_profiles_TenantId",
                table: "social_mentor_profiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_social_mentor_profiles_UserId",
                table: "social_mentor_profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_mentor_skills_MentorProfileId_NormalizedSkill",
                table: "social_mentor_skills",
                columns: new[] { "MentorProfileId", "NormalizedSkill" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_mentor_skills_NormalizedSkill",
                table: "social_mentor_skills",
                column: "NormalizedSkill");

            migrationBuilder.CreateIndex(
                name: "IX_social_mentorship_sessions_MenteeUserId_StartsAt",
                table: "social_mentorship_sessions",
                columns: new[] { "MenteeUserId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_social_mentorship_sessions_MentorUserId_StartsAt",
                table: "social_mentorship_sessions",
                columns: new[] { "MentorUserId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_social_mentorship_sessions_SlotId",
                table: "social_mentorship_sessions",
                column: "SlotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "social_mentor_skills");

            migrationBuilder.DropTable(
                name: "social_mentorship_sessions");

            migrationBuilder.DropTable(
                name: "social_mentor_availability_slots");

            migrationBuilder.DropTable(
                name: "social_mentor_profiles");
        }
    }
}
