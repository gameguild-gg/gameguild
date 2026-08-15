using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPeerReviewsAndSubmissionExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CourseGroupId",
                table: "AssessmentSubmissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RubricScoresPayload",
                table: "AssessmentSubmissions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PeerReviewsRequiredCount",
                table: "Assessments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AssessmentPeerReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    Feedback = table.Column<string>(type: "text", nullable: true),
                    RubricScoresPayload = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentPeerReviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentPeerReviews_AssessmentId",
                table: "AssessmentPeerReviews",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentPeerReviews_ReviewerUserId",
                table: "AssessmentPeerReviews",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentPeerReviews_ReviewerUserId_SubmissionId",
                table: "AssessmentPeerReviews",
                columns: new[] { "ReviewerUserId", "SubmissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentPeerReviews_SubmissionId",
                table: "AssessmentPeerReviews",
                column: "SubmissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentPeerReviews");

            migrationBuilder.DropColumn(
                name: "CourseGroupId",
                table: "AssessmentSubmissions");

            migrationBuilder.DropColumn(
                name: "RubricScoresPayload",
                table: "AssessmentSubmissions");

            migrationBuilder.DropColumn(
                name: "PeerReviewsRequiredCount",
                table: "Assessments");
        }
    }
}
