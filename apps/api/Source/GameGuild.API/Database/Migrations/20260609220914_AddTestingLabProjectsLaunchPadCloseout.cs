using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingLabProjectsLaunchPadCloseout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Jams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "project_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "testing_lab_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DefaultSessionDuration = table.Column<int>(type: "integer", nullable: false),
                    AllowPublicSignups = table.Column<bool>(type: "boolean", nullable: false),
                    RequireApproval = table.Column<bool>(type: "boolean", nullable: false),
                    EnableNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    MaxSimultaneousSessions = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_lab_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_lab_settings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "testing_locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    MaxProjectsCapacity = table.Column<int>(type: "integer", nullable: false),
                    Equipment = table.Column<string>(type: "text", nullable: true),
                    IsVirtual = table.Column<bool>(type: "boolean", nullable: false),
                    VirtualUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "text", maxLength: 2000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DevelopmentStatus = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RepositoryUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SocialLinks = table.Column<string>(type: "text", nullable: true),
                    DownloadUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    FeaturedImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    License = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Copyright = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_projects_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_projects_project_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "project_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_projects_project_categories_ProjectCategoryId",
                        column: x => x.ProjectCategoryId,
                        principalTable: "project_categories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "launch_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Positioning = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TargetLaunchAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LaunchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Channels = table.Column<string[]>(type: "text[]", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_launch_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_launch_plans_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InvitedEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Permissions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_invitations_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project_invitations_Users_InvitedUserId",
                        column: x => x.InvitedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_project_invitations_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    DownloadCount = table.Column<int>(type: "integer", nullable: false),
                    FollowerCount = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_metadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_metadata_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_releases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ReleaseVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsLatest = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrerelease = table.Column<bool>(type: "boolean", nullable: false),
                    DownloadUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    DownloadCount = table.Column<int>(type: "integer", nullable: false),
                    ReleaseNotes = table.Column<string>(type: "text", nullable: true),
                    Checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SystemRequirements = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SupportedPlatforms = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReleaseType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BuildNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReleaseMetadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_releases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_releases_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Permissions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContributionPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_teams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_teams_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReleaseNotes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DownloadCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_versions_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_versions_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectCollaborators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Permissions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProjectId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCollaborators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectCollaborators_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectCollaborators_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectCollaborators_projects_ProjectId1",
                        column: x => x.ProjectId1,
                        principalTable: "projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Categories = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HelpfulVotes = table.Column<int>(type: "integer", nullable: false),
                    TotalVotes = table.Column<int>(type: "integer", nullable: false),
                    Platform = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProjectVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFeedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectFeedbacks_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectFeedbacks_projects_ProjectId1",
                        column: x => x.ProjectId1,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFollowers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotificationSettings = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EmailNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    PushNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFollowers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFollowers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectFollowers_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectJamSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    JamId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsEligible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SubmissionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FinalScore = table.Column<decimal>(type: "numeric", nullable: true),
                    Ranking = table.Column<int>(type: "integer", nullable: true),
                    HasAward = table.Column<bool>(type: "boolean", nullable: false),
                    AwardDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectJamSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectJamSubmissions_Jams_JamId",
                        column: x => x.JamId,
                        principalTable: "Jams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectJamSubmissions_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "launch_checklist_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaunchPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_launch_checklist_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_launch_checklist_items_launch_plans_LaunchPlanId",
                        column: x => x.LaunchPlanId,
                        principalTable: "launch_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "testing_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DownloadUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InstructionsType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    InstructionsContent = table.Column<string>(type: "text", nullable: true),
                    InstructionsUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InstructionsFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    FeedbackFormContent = table.Column<string>(type: "text", nullable: true),
                    MaxTesters = table.Column<int>(type: "integer", nullable: true),
                    CurrentTesterCount = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EstimatedDurationHours = table.Column<int>(type: "integer", nullable: true),
                    Mode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_requests_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_requests_project_versions_ProjectVersionId",
                        column: x => x.ProjectVersionId,
                        principalTable: "project_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "JamScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JamSubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    JudgeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: false),
                    Category = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JamScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JamScores_ProjectJamSubmissions_JamSubmissionId",
                        column: x => x.JamSubmissionId,
                        principalTable: "ProjectJamSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "testing_feedback_forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    FormData = table.Column<string>(type: "text", nullable: false),
                    TestingRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsForOnline = table.Column<bool>(type: "boolean", nullable: false),
                    IsForSessions = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    FormType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FormVersion = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_feedback_forms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_feedback_forms_testing_requests_TestingRequestId",
                        column: x => x.TestingRequestId,
                        principalTable: "testing_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "testing_participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestingRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstructionsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    InstructionsAcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeSpentMinutes = table.Column<int>(type: "integer", nullable: true),
                    FeedbackCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_participants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_participants_testing_requests_TestingRequestId",
                        column: x => x.TestingRequestId,
                        principalTable: "testing_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "testing_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestingRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SessionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxTesters = table.Column<int>(type: "integer", nullable: false),
                    MaxProjects = table.Column<int>(type: "integer", nullable: false),
                    RegisteredTesterCount = table.Column<int>(type: "integer", nullable: false),
                    RegisteredProjectMemberCount = table.Column<int>(type: "integer", nullable: false),
                    RegisteredProjectCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ManagerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManagerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_sessions_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_sessions_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_sessions_testing_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "testing_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_sessions_testing_requests_TestingRequestId",
                        column: x => x.TestingRequestId,
                        principalTable: "testing_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "session_projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RegisteredById = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_session_projects_Users_RegisteredById",
                        column: x => x.RegisteredById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_session_projects_project_versions_ProjectVersionId",
                        column: x => x.ProjectVersionId,
                        principalTable: "project_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_session_projects_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_session_projects_testing_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "testing_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "session_registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckedInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckedOutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttendanceStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_session_registrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_session_registrations_testing_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "testing_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "session_waitlist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    RegistrationNotes = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_waitlist", x => x.Id);
                    table.ForeignKey(
                        name: "FK_session_waitlist_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_session_waitlist_testing_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "testing_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "testing_feedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestingRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedbackFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TestingContext = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FeedbackData = table.Column<string>(type: "text", nullable: false),
                    OverallRating = table.Column<int>(type: "integer", nullable: true),
                    WouldRecommend = table.Column<bool>(type: "boolean", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "text", nullable: true),
                    IsReported = table.Column<bool>(type: "boolean", nullable: false),
                    QualityRating = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ReportReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReportedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TestingParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_testing_feedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_testing_feedback_Users_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_testing_feedback_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_feedback_testing_feedback_forms_FeedbackFormId",
                        column: x => x.FeedbackFormId,
                        principalTable: "testing_feedback_forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_testing_feedback_testing_participants_TestingParticipantId",
                        column: x => x.TestingParticipantId,
                        principalTable: "testing_participants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_testing_feedback_testing_requests_TestingRequestId",
                        column: x => x.TestingRequestId,
                        principalTable: "testing_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_testing_feedback_testing_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "testing_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "feedback_quality_ratings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedbackId = table.Column<Guid>(type: "uuid", nullable: false),
                    RatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRating = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedback_quality_ratings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_feedback_quality_ratings_Users_RatedByUserId",
                        column: x => x.RatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_feedback_quality_ratings_testing_feedback_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "testing_feedback",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_feedback_quality_ratings_FeedbackId",
                table: "feedback_quality_ratings",
                column: "FeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_quality_ratings_QualityRating",
                table: "feedback_quality_ratings",
                column: "QualityRating");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_quality_ratings_RatedByUserId",
                table: "feedback_quality_ratings",
                column: "RatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_quality_ratings_TenantId",
                table: "feedback_quality_ratings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_JamScores_JamSubmissionId",
                table: "JamScores",
                column: "JamSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_launch_checklist_items_LaunchPlanId",
                table: "launch_checklist_items",
                column: "LaunchPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_launch_plans_ProjectId",
                table: "launch_plans",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_launch_plans_Status_TargetLaunchAt",
                table: "launch_plans",
                columns: new[] { "Status", "TargetLaunchAt" });

            migrationBuilder.CreateIndex(
                name: "IX_project_categories_Name",
                table: "project_categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_invitations_InvitedByUserId",
                table: "project_invitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_Project_Status",
                table: "project_invitations",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_Token",
                table: "project_invitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_User_Status",
                table: "project_invitations",
                columns: new[] { "InvitedUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_project_metadata_ProjectId",
                table: "project_metadata",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_releases_ProjectId_ReleaseVersion",
                table: "project_releases",
                columns: new[] { "ProjectId", "ReleaseVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_Latest",
                table: "project_releases",
                column: "IsLatest");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_Project_Date",
                table: "project_releases",
                columns: new[] { "ProjectId", "ReleasedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_Project_Version",
                table: "project_releases",
                columns: new[] { "ProjectId", "ReleaseVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_Date",
                table: "project_teams",
                column: "AssignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_Project_Team",
                table: "project_teams",
                columns: new[] { "ProjectId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_Team",
                table: "project_teams",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_project_versions_CreatedById",
                table: "project_versions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_project_versions_ProjectId",
                table: "project_versions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_project_versions_VersionNumber",
                table: "project_versions",
                column: "VersionNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_Project_User",
                table: "ProjectCollaborators",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_ProjectId1",
                table: "ProjectCollaborators",
                column: "ProjectId1");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_User",
                table: "ProjectCollaborators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_Date",
                table: "ProjectFeedbacks",
                column: "CreatedAt");

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
                name: "IX_ProjectFeedbacks_ProjectId1",
                table: "ProjectFeedbacks",
                column: "ProjectId1");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_User",
                table: "ProjectFeedbacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_Date",
                table: "ProjectFollowers",
                column: "FollowedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_Project_User",
                table: "ProjectFollowers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_User",
                table: "ProjectFollowers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_Date",
                table: "ProjectJamSubmissions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_Jam",
                table: "ProjectJamSubmissions",
                column: "JamId");

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
                name: "IX_projects_CategoryId",
                table: "projects",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_CategoryId_Status",
                table: "projects",
                columns: new[] { "CategoryId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_projects_CreatedAt",
                table: "projects",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_projects_CreatedById",
                table: "projects",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_projects_ProjectCategoryId",
                table: "projects",
                column: "ProjectCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_Status",
                table: "projects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_projects_Status_Visibility",
                table: "projects",
                columns: new[] { "Status", "Visibility" });

            migrationBuilder.CreateIndex(
                name: "IX_projects_TenantId",
                table: "projects",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_TenantId_Status",
                table: "projects",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_projects_Title",
                table: "projects",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_projects_UpdatedAt",
                table: "projects",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_projects_Visibility",
                table: "projects",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_session_projects_ProjectId",
                table: "session_projects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_session_projects_ProjectVersionId",
                table: "session_projects",
                column: "ProjectVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_session_projects_RegisteredById",
                table: "session_projects",
                column: "RegisteredById");

            migrationBuilder.CreateIndex(
                name: "IX_session_projects_SessionId",
                table: "session_projects",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_session_registrations_RegisteredAt",
                table: "session_registrations",
                column: "RegisteredAt");

            migrationBuilder.CreateIndex(
                name: "IX_session_registrations_RegistrationType",
                table: "session_registrations",
                column: "RegistrationType");

            migrationBuilder.CreateIndex(
                name: "IX_session_registrations_SessionId",
                table: "session_registrations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_session_registrations_Status",
                table: "session_registrations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_session_registrations_TenantId",
                table: "session_registrations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_session_registrations_UserId",
                table: "session_registrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_session_waitlist_SessionId",
                table: "session_waitlist",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_session_waitlist_UserId",
                table: "session_waitlist",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamId",
                table: "TeamMembers",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_UserId",
                table: "TeamMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_FeedbackFormId",
                table: "testing_feedback",
                column: "FeedbackFormId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_IsReported",
                table: "testing_feedback",
                column: "IsReported");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_OverallRating",
                table: "testing_feedback",
                column: "OverallRating");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_QualityRating",
                table: "testing_feedback",
                column: "QualityRating");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_ReportedById",
                table: "testing_feedback",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_SessionId",
                table: "testing_feedback",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_TenantId",
                table: "testing_feedback",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_TestingContext",
                table: "testing_feedback",
                column: "TestingContext");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_TestingParticipantId",
                table: "testing_feedback",
                column: "TestingParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_TestingRequestId",
                table: "testing_feedback",
                column: "TestingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_UserId",
                table: "testing_feedback",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_forms_FormType",
                table: "testing_feedback_forms",
                column: "FormType");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_forms_IsActive",
                table: "testing_feedback_forms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_forms_Name",
                table: "testing_feedback_forms",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_forms_TenantId",
                table: "testing_feedback_forms",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_feedback_forms_TestingRequestId",
                table: "testing_feedback_forms",
                column: "TestingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_lab_settings_TenantId",
                table: "testing_lab_settings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_locations_Capacity",
                table: "testing_locations",
                column: "Capacity");

            migrationBuilder.CreateIndex(
                name: "IX_testing_locations_City",
                table: "testing_locations",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_testing_locations_IsVirtual",
                table: "testing_locations",
                column: "IsVirtual");

            migrationBuilder.CreateIndex(
                name: "IX_testing_locations_Name",
                table: "testing_locations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_testing_locations_Status",
                table: "testing_locations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_testing_locations_TenantId",
                table: "testing_locations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_participants_CompletedAt",
                table: "testing_participants",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_testing_participants_InstructionsAcknowledged",
                table: "testing_participants",
                column: "InstructionsAcknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_testing_participants_StartedAt",
                table: "testing_participants",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_testing_participants_TenantId",
                table: "testing_participants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_participants_TestingRequestId",
                table: "testing_participants",
                column: "TestingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_participants_UserId",
                table: "testing_participants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_requests_CreatedById",
                table: "testing_requests",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_testing_requests_EndDate",
                table: "testing_requests",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_testing_requests_InstructionsType",
                table: "testing_requests",
                column: "InstructionsType");

            migrationBuilder.CreateIndex(
                name: "IX_testing_requests_ProjectVersionId",
                table: "testing_requests",
                column: "ProjectVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_requests_StartDate",
                table: "testing_requests",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_testing_requests_Status",
                table: "testing_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_testing_requests_TenantId",
                table: "testing_requests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_sessions_CreatedById",
                table: "testing_sessions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_testing_sessions_LocationId",
                table: "testing_sessions",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_sessions_ManagerId",
                table: "testing_sessions",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_sessions_SessionDate",
                table: "testing_sessions",
                column: "SessionDate");

            migrationBuilder.CreateIndex(
                name: "IX_testing_sessions_Status",
                table: "testing_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_testing_sessions_TenantId",
                table: "testing_sessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_testing_sessions_TestingRequestId",
                table: "testing_sessions",
                column: "TestingRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feedback_quality_ratings");

            migrationBuilder.DropTable(
                name: "JamScores");

            migrationBuilder.DropTable(
                name: "launch_checklist_items");

            migrationBuilder.DropTable(
                name: "project_invitations");

            migrationBuilder.DropTable(
                name: "project_metadata");

            migrationBuilder.DropTable(
                name: "project_releases");

            migrationBuilder.DropTable(
                name: "project_teams");

            migrationBuilder.DropTable(
                name: "ProjectCollaborators");

            migrationBuilder.DropTable(
                name: "ProjectFeedbacks");

            migrationBuilder.DropTable(
                name: "ProjectFollowers");

            migrationBuilder.DropTable(
                name: "session_projects");

            migrationBuilder.DropTable(
                name: "session_registrations");

            migrationBuilder.DropTable(
                name: "session_waitlist");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "testing_lab_settings");

            migrationBuilder.DropTable(
                name: "testing_feedback");

            migrationBuilder.DropTable(
                name: "ProjectJamSubmissions");

            migrationBuilder.DropTable(
                name: "launch_plans");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "testing_feedback_forms");

            migrationBuilder.DropTable(
                name: "testing_participants");

            migrationBuilder.DropTable(
                name: "testing_sessions");

            migrationBuilder.DropTable(
                name: "Jams");

            migrationBuilder.DropTable(
                name: "testing_locations");

            migrationBuilder.DropTable(
                name: "testing_requests");

            migrationBuilder.DropTable(
                name: "project_versions");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "project_categories");
        }
    }
}
