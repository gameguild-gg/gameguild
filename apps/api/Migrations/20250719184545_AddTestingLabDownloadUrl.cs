using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingLabDownloadUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DownloadUrl",
                table: "TestingRequests",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedbackFormContent",
                table: "TestingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReported",
                table: "TestingFeedback",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OverallRating",
                table: "TestingFeedback",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualityRating",
                table: "TestingFeedback",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportReason",
                table: "TestingFeedback",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReportedAt",
                table: "TestingFeedback",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReportedByUserId",
                table: "TestingFeedback",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WouldRecommend",
                table: "TestingFeedback",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IconUrl = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSecret = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRepeatable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Conditions = table.Column<string>(type: "jsonb", nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "achievement_levels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AchievementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    RequiredProgress = table.Column<int>(type: "INTEGER", nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    IconUrl = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement_levels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_achievement_levels_achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "achievement_prerequisites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AchievementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrerequisiteAchievementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequiresCompletion = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinimumLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement_prerequisites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_achievement_prerequisites_achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_achievement_prerequisites_achievements_PrerequisiteAchievementId",
                        column: x => x.PrerequisiteAchievementId,
                        principalTable: "achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "achievement_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AchievementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrentProgress = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetProgress = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Context = table.Column<string>(type: "jsonb", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement_progress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_achievement_progress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_achievement_progress_achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AchievementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: true),
                    Progress = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxProgress = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsNotified = table.Column<bool>(type: "INTEGER", nullable: false),
                    Context = table.Column<string>(type: "jsonb", nullable: true),
                    PointsEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EarnCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_achievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_achievements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_achievements_achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedback_ReportedByUserId",
                table: "TestingFeedback",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_levels_AchievementId",
                table: "achievement_levels",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_levels_Level",
                table: "achievement_levels",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_prerequisites_AchievementId",
                table: "achievement_prerequisites",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_prerequisites_PrerequisiteAchievementId",
                table: "achievement_prerequisites",
                column: "PrerequisiteAchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_progress_AchievementId",
                table: "achievement_progress",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_progress_TenantId",
                table: "achievement_progress",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_progress_UserId",
                table: "achievement_progress",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_progress_UserId_AchievementId",
                table: "achievement_progress",
                columns: new[] { "UserId", "AchievementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_achievements_Category",
                table: "achievements",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_achievements_IsActive",
                table: "achievements",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_achievements_TenantId",
                table: "achievements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_achievements_Type",
                table: "achievements",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_AchievementId",
                table: "user_achievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_EarnedAt",
                table: "user_achievements",
                column: "EarnedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_TenantId",
                table: "user_achievements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_UserId",
                table: "user_achievements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_UserId_AchievementId",
                table: "user_achievements",
                columns: new[] { "UserId", "AchievementId" });

            migrationBuilder.AddForeignKey(
                name: "FK_TestingFeedback_Users_ReportedByUserId",
                table: "TestingFeedback",
                column: "ReportedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestingFeedback_Users_ReportedByUserId",
                table: "TestingFeedback");

            migrationBuilder.DropTable(
                name: "achievement_levels");

            migrationBuilder.DropTable(
                name: "achievement_prerequisites");

            migrationBuilder.DropTable(
                name: "achievement_progress");

            migrationBuilder.DropTable(
                name: "user_achievements");

            migrationBuilder.DropTable(
                name: "achievements");

            migrationBuilder.DropIndex(
                name: "IX_TestingFeedback_ReportedByUserId",
                table: "TestingFeedback");

            migrationBuilder.DropColumn(
                name: "DownloadUrl",
                table: "TestingRequests");

            migrationBuilder.DropColumn(
                name: "FeedbackFormContent",
                table: "TestingRequests");

            migrationBuilder.DropColumn(
                name: "IsReported",
                table: "TestingFeedback");

            migrationBuilder.DropColumn(
                name: "OverallRating",
                table: "TestingFeedback");

            migrationBuilder.DropColumn(
                name: "QualityRating",
                table: "TestingFeedback");

            migrationBuilder.DropColumn(
                name: "ReportReason",
                table: "TestingFeedback");

            migrationBuilder.DropColumn(
                name: "ReportedAt",
                table: "TestingFeedback");

            migrationBuilder.DropColumn(
                name: "ReportedByUserId",
                table: "TestingFeedback");

            migrationBuilder.DropColumn(
                name: "WouldRecommend",
                table: "TestingFeedback");
        }
    }
}
