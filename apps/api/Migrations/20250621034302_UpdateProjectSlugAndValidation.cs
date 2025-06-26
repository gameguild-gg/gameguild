using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProjectSlugAndValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Project_ProjectCategory_CategoryId",
                table: "Project");

            migrationBuilder.DropForeignKey(
                name: "FK_Project_ResourceMetadata_MetadataId",
                table: "Project");

            migrationBuilder.DropForeignKey(
                name: "FK_Project_Tenants_TenantId",
                table: "Project");

            migrationBuilder.DropForeignKey(
                name: "FK_Project_Users_CreatedById",
                table: "Project");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMetadata_Project_ProjectId",
                table: "ProjectMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectPermissions_Project_ResourceId",
                table: "ProjectPermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectVersion_Project_ProjectId",
                table: "ProjectVersion");

            migrationBuilder.DropIndex(
                name: "IX_ProjectPermissions_ResourceId",
                table: "ProjectPermissions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectPermissions_UserId",
                table: "ProjectPermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Project",
                table: "Project");

            migrationBuilder.RenameTable(
                name: "Project",
                newName: "Projects");

            migrationBuilder.RenameIndex(
                name: "IX_Project_TenantId",
                table: "Projects",
                newName: "IX_Projects_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Project_MetadataId",
                table: "Projects",
                newName: "IX_Projects_MetadataId");

            migrationBuilder.RenameIndex(
                name: "IX_Project_DeletedAt",
                table: "Projects",
                newName: "IX_Projects_DeletedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Project_CreatedById",
                table: "Projects",
                newName: "IX_Projects_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Project_CreatedAt",
                table: "Projects",
                newName: "IX_Projects_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Project_CategoryId",
                table: "Projects",
                newName: "IX_Projects_CategoryId");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                table: "Projects",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Projects",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "DevelopmentStatus",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DownloadUrl",
                table: "Projects",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Projects",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectCategoryId",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Projects",
                table: "Projects",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Jam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Rules = table.Column<string>(type: "TEXT", nullable: true),
                    SubmissionCriteria = table.Column<string>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VotingEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MaxParticipants = table.Column<int>(type: "INTEGER", nullable: true),
                    ParticipantCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jam", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jam_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectCollaborators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Permissions = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCollaborators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectCollaborators_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectCollaborators_ResourceMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ResourceMetadata",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectCollaborators_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectCollaborators_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Categories = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsFeatured = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    HelpfulVotes = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalVotes = table.Column<int>(type: "INTEGER", nullable: false),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ProjectVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFeedbacks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectFeedbacks_ResourceMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ResourceMetadata",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectFeedbacks_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectFeedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFollowers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NotificationSettings = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    EmailNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    PushNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFollowers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFollowers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectFollowers_ResourceMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ResourceMetadata",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectFollowers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectFollowers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReleaseVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsLatest = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPrerelease = table.Column<bool>(type: "INTEGER", nullable: false),
                    DownloadUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: true),
                    DownloadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ReleaseNotes = table.Column<string>(type: "TEXT", nullable: true),
                    Checksum = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SystemRequirements = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SupportedPlatforms = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReleaseType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    BuildNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReleaseMetadata = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectReleases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectReleases_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectReleases_ResourceMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ResourceMetadata",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectReleases_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Team",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Team", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectJamSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsEligible = table.Column<bool>(type: "INTEGER", nullable: false),
                    SubmissionNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    FinalScore = table.Column<decimal>(type: "TEXT", nullable: true),
                    Ranking = table.Column<int>(type: "INTEGER", nullable: true),
                    HasAward = table.Column<bool>(type: "INTEGER", nullable: false),
                    AwardDetails = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectJamSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectJamSubmissions_Jam_JamId",
                        column: x => x.JamId,
                        principalTable: "Jam",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectJamSubmissions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectJamSubmissions_ResourceMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ResourceMetadata",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectJamSubmissions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Permissions = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ContributionPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTeams_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTeams_ResourceMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ResourceMetadata",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectTeams_Team_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTeams_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TeamMember",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    InvitedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMember_Team_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMember_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JamScore",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SubmissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JudgeUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    Feedback = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectJamSubmissionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JamScore", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JamScore_ProjectJamSubmissions_ProjectJamSubmissionId",
                        column: x => x.ProjectJamSubmissionId,
                        principalTable: "ProjectJamSubmissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JamScore_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPermissions_Expiration",
                table: "ProjectPermissions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPermissions_Resource_User",
                table: "ProjectPermissions",
                columns: new[] { "ResourceId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPermissions_User_Tenant_Resource",
                table: "ProjectPermissions",
                columns: new[] { "UserId", "TenantId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CategoryId_Status",
                table: "Projects",
                columns: new[] { "CategoryId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectCategoryId",
                table: "Projects",
                column: "ProjectCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status",
                table: "Projects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status_Visibility",
                table: "Projects",
                columns: new[] { "Status", "Visibility" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Title",
                table: "Projects",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_UpdatedAt",
                table: "Projects",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Visibility",
                table: "Projects",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_Jam_CreatedAt",
                table: "Jam",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Jam_DeletedAt",
                table: "Jam",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Jam_TenantId",
                table: "Jam",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_JamScore_CreatedAt",
                table: "JamScore",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JamScore_DeletedAt",
                table: "JamScore",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JamScore_ProjectJamSubmissionId",
                table: "JamScore",
                column: "ProjectJamSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_JamScore_TenantId",
                table: "JamScore",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_CreatedAt",
                table: "ProjectCollaborators",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_DeletedAt",
                table: "ProjectCollaborators",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_MetadataId",
                table: "ProjectCollaborators",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_Project_User",
                table: "ProjectCollaborators",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_TenantId",
                table: "ProjectCollaborators",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_User",
                table: "ProjectCollaborators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_CreatedAt",
                table: "ProjectFeedbacks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_Date",
                table: "ProjectFeedbacks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_DeletedAt",
                table: "ProjectFeedbacks",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_MetadataId",
                table: "ProjectFeedbacks",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_Project_Rating",
                table: "ProjectFeedbacks",
                columns: new[] { "ProjectId", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_Project_User",
                table: "ProjectFeedbacks",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_TenantId",
                table: "ProjectFeedbacks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_User",
                table: "ProjectFeedbacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_CreatedAt",
                table: "ProjectFollowers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_Date",
                table: "ProjectFollowers",
                column: "FollowedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_DeletedAt",
                table: "ProjectFollowers",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_MetadataId",
                table: "ProjectFollowers",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_Project_User",
                table: "ProjectFollowers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_TenantId",
                table: "ProjectFollowers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_User",
                table: "ProjectFollowers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_CreatedAt",
                table: "ProjectJamSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_Date",
                table: "ProjectJamSubmissions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_DeletedAt",
                table: "ProjectJamSubmissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_Jam",
                table: "ProjectJamSubmissions",
                column: "JamId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_MetadataId",
                table: "ProjectJamSubmissions",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_Project_Jam",
                table: "ProjectJamSubmissions",
                columns: new[] { "ProjectId", "JamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_Score",
                table: "ProjectJamSubmissions",
                column: "FinalScore");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_TenantId",
                table: "ProjectJamSubmissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_CreatedAt",
                table: "ProjectReleases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_DeletedAt",
                table: "ProjectReleases",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_Latest",
                table: "ProjectReleases",
                column: "IsLatest");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_MetadataId",
                table: "ProjectReleases",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_Project_Date",
                table: "ProjectReleases",
                columns: new[] { "ProjectId", "ReleasedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_Project_Version",
                table: "ProjectReleases",
                columns: new[] { "ProjectId", "ReleaseVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_TenantId",
                table: "ProjectReleases",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_CreatedAt",
                table: "ProjectTeams",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_Date",
                table: "ProjectTeams",
                column: "AssignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_DeletedAt",
                table: "ProjectTeams",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_MetadataId",
                table: "ProjectTeams",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_Project_Team",
                table: "ProjectTeams",
                columns: new[] { "ProjectId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_Team",
                table: "ProjectTeams",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_TenantId",
                table: "ProjectTeams",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_CreatedAt",
                table: "TeamMember",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_DeletedAt",
                table: "TeamMember",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_TeamId",
                table: "TeamMember",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_TenantId",
                table: "TeamMember",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMetadata_Projects_ProjectId",
                table: "ProjectMetadata",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectPermissions_Projects_ResourceId",
                table: "ProjectPermissions",
                column: "ResourceId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectCategory_CategoryId",
                table: "Projects",
                column: "CategoryId",
                principalTable: "ProjectCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectCategory_ProjectCategoryId",
                table: "Projects",
                column: "ProjectCategoryId",
                principalTable: "ProjectCategory",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ResourceMetadata_MetadataId",
                table: "Projects",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Tenants_TenantId",
                table: "Projects",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_CreatedById",
                table: "Projects",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectVersion_Projects_ProjectId",
                table: "ProjectVersion",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMetadata_Projects_ProjectId",
                table: "ProjectMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectPermissions_Projects_ResourceId",
                table: "ProjectPermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectCategory_CategoryId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectCategory_ProjectCategoryId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ResourceMetadata_MetadataId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Tenants_TenantId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_CreatedById",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectVersion_Projects_ProjectId",
                table: "ProjectVersion");

            migrationBuilder.DropTable(
                name: "JamScore");

            migrationBuilder.DropTable(
                name: "ProjectCollaborators");

            migrationBuilder.DropTable(
                name: "ProjectFeedbacks");

            migrationBuilder.DropTable(
                name: "ProjectFollowers");

            migrationBuilder.DropTable(
                name: "ProjectReleases");

            migrationBuilder.DropTable(
                name: "ProjectTeams");

            migrationBuilder.DropTable(
                name: "TeamMember");

            migrationBuilder.DropTable(
                name: "ProjectJamSubmissions");

            migrationBuilder.DropTable(
                name: "Team");

            migrationBuilder.DropTable(
                name: "Jam");

            migrationBuilder.DropIndex(
                name: "IX_ProjectPermissions_Expiration",
                table: "ProjectPermissions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectPermissions_Resource_User",
                table: "ProjectPermissions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectPermissions_User_Tenant_Resource",
                table: "ProjectPermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Projects",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_CategoryId_Status",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ProjectCategoryId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Status",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Status_Visibility",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Title",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_UpdatedAt",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Visibility",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DevelopmentStatus",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DownloadUrl",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectCategoryId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Projects");

            migrationBuilder.RenameTable(
                name: "Projects",
                newName: "Project");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_TenantId",
                table: "Project",
                newName: "IX_Project_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_MetadataId",
                table: "Project",
                newName: "IX_Project_MetadataId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_DeletedAt",
                table: "Project",
                newName: "IX_Project_DeletedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_CreatedById",
                table: "Project",
                newName: "IX_Project_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_CreatedAt",
                table: "Project",
                newName: "IX_Project_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_CategoryId",
                table: "Project",
                newName: "IX_Project_CategoryId");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                table: "Project",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Project",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Project",
                table: "Project",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPermissions_ResourceId",
                table: "ProjectPermissions",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPermissions_UserId",
                table: "ProjectPermissions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Project_ProjectCategory_CategoryId",
                table: "Project",
                column: "CategoryId",
                principalTable: "ProjectCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Project_ResourceMetadata_MetadataId",
                table: "Project",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Project_Tenants_TenantId",
                table: "Project",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Project_Users_CreatedById",
                table: "Project",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMetadata_Project_ProjectId",
                table: "ProjectMetadata",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectPermissions_Project_ResourceId",
                table: "ProjectPermissions",
                column: "ResourceId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectVersion_Project_ProjectId",
                table: "ProjectVersion",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
