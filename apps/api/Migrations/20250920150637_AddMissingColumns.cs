using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommentPermissions_Comment_ResourceId",
                table: "CommentPermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_CommentPermissions_Users_UserId",
                table: "CommentPermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectPermissions_Projects_ResourceId",
                table: "ProjectPermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectPermissions_Users_UserId",
                table: "ProjectPermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceLocalizations_Comment_CommentId",
                table: "ResourceLocalizations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPermissions_RoleTemplates_RoleTemplateId",
                table: "UserPermissions");

            migrationBuilder.DropTable(
                name: "Comment");

            migrationBuilder.DropIndex(
                name: "IX_UserTenantReputations_TenantId",
                table: "UserTenantReputations");

            migrationBuilder.DropIndex(
                name: "IX_UserReputations_TenantId",
                table: "UserReputations");

            migrationBuilder.DropIndex(
                name: "IX_UserReputationHistory_TenantId",
                table: "UserReputationHistory");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_TenantId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserPermissions_RoleTemplateId",
                table: "UserPermissions");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_TenantId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_ReputationLevels_TenantId",
                table: "ReputationLevels");

            migrationBuilder.DropIndex(
                name: "IX_ReputationActions_TenantId",
                table: "ReputationActions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTeams_TenantId",
                table: "ProjectTeams");

            migrationBuilder.DropIndex(
                name: "IX_Projects_TenantId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ProjectReleases_TenantId",
                table: "ProjectReleases");

            migrationBuilder.DropIndex(
                name: "IX_ProjectJamSubmissions_TenantId",
                table: "ProjectJamSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectFollowers_TenantId",
                table: "ProjectFollowers");

            migrationBuilder.DropIndex(
                name: "IX_ProjectFeedbacks_TenantId",
                table: "ProjectFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectCollaborators_TenantId",
                table: "ProjectCollaborators");

            migrationBuilder.DropIndex(
                name: "IX_ProjectCategory_TenantId",
                table: "ProjectCategory");

            migrationBuilder.DropIndex(
                name: "IX_programs_TenantId",
                table: "programs");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_posts_TenantId",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_ContentLicenses_TenantId",
                table: "ContentLicenses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserRoleAssignments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserRoleAssignments");

            migrationBuilder.DropColumn(
                name: "RoleTemplateId",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "user_achievements");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "user_achievements");

            migrationBuilder.DropColumn(
                name: "PermissionFlags1",
                table: "ProjectPermissions");

            migrationBuilder.DropColumn(
                name: "PermissionFlags2",
                table: "ProjectPermissions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ModuleRoles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ModuleRoles");

            migrationBuilder.DropColumn(
                name: "PermissionFlags1",
                table: "CommentPermissions");

            migrationBuilder.DropColumn(
                name: "PermissionFlags2",
                table: "CommentPermissions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "achievements");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "achievements");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "achievement_progress");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "achievement_progress");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "achievement_prerequisites");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "achievement_prerequisites");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "achievement_levels");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "achievement_levels");

            migrationBuilder.RenameColumn(
                name: "PermissionTemplatesJson",
                table: "RoleTemplates",
                newName: "PermissionDefinitions");

            migrationBuilder.RenameColumn(
                name: "CommentId",
                table: "ResourceLocalizations",
                newName: "TenantSettingsId");

            migrationBuilder.RenameIndex(
                name: "IX_ResourceLocalizations_CommentId",
                table: "ResourceLocalizations",
                newName: "IX_ResourceLocalizations_TenantSettingsId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "Users",
                type: "numeric(18,8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "AvailableBalance",
                table: "Users",
                type: "numeric(18,8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldDefaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Email1",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserRoleAssignments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "UserRoleAssignments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "UserRoleAssignments",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "UserRoleAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "AutoRenew",
                table: "user_subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BillingCycle",
                table: "user_subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BillingCycleCount",
                table: "user_subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CancellationNote",
                table: "user_subscriptions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancellationReason",
                table: "user_subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalCustomerId",
                table: "user_subscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "user_subscriptions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                table: "user_subscriptions",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "user_achievements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "user_achievements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "user_achievements",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "user_achievements",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "AdminEmail",
                table: "Tenants",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "RoleTemplates",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "RoleTemplates",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "RoleTemplates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "RoleTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "RoleTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "RoleTemplates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Permissions",
                table: "ProjectPermissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ModuleRoles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ModuleRoles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ModuleRoles",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ModuleRoles",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Permissions",
                table: "CommentPermissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "achievements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "achievements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "achievements",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "achievements",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "achievement_progress",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "achievement_progress",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "achievement_progress",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "achievement_progress",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "achievement_prerequisites",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "achievement_prerequisites",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "achievement_prerequisites",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "achievement_prerequisites",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "achievement_prerequisites",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "achievement_levels",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "achievement_levels",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "achievement_levels",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "achievement_levels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "achievement_levels",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BillingWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalEventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Headers = table.Column<string>(type: "text", nullable: true),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    IsFailed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessingAttempts = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingWebhookEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingWebhookEvents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EnabledValue = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsGlobal = table.Column<bool>(type: "boolean", nullable: false),
                    RolloutPercentage = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    Environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "production"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureFlags_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessingTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsSuspicious = table.Column<bool>(type: "boolean", nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginAttempts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MfaAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaAttempts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    PushNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    InAppNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    SoundEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CommentNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    FollowNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    InviteNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    TaskNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    MentionNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    SystemNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    CourseNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    AchievementNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    SocialNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    PromotionNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsStarred = table.Column<bool>(type: "boolean", nullable: false),
                    ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ActionText = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResourceQuotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SoftLimit = table.Column<long>(type: "bigint", nullable: true),
                    HardLimit = table.Column<long>(type: "bigint", nullable: true),
                    CurrentUsage = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    LastReset = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResetTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ResetDayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    ResetDayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    NotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationThresholds = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceQuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceQuotas_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceUsageRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Count = table.Column<long>(type: "bigint", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AveragePerDay = table.Column<double>(type: "double precision", nullable: true),
                    PeakUsage = table.Column<long>(type: "bigint", nullable: true),
                    PeakUsageDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceUsageRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceUsageRecords_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantRoleApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    RoleTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CustomDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PermissionOverrides = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantRoleApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantRoleApplications_RoleTemplates_RoleTemplateId",
                        column: x => x.RoleTemplateId,
                        principalTable: "RoleTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantRoleApplications_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TenantSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DefaultTimezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DateFormat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Use24HourFormat = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    SecondaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CustomCss = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultTheme = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FeatureFlags = table.Column<string>(type: "JSON", nullable: true),
                    ModuleSettings = table.Column<string>(type: "JSON", nullable: true),
                    AllowUserRegistration = table.Column<bool>(type: "boolean", nullable: false),
                    RequireRegistrationApproval = table.Column<bool>(type: "boolean", nullable: false),
                    EnableEmailNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    EnablePushNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    EnableSmsNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultNotificationEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RequireTwoFactorAuth = table.Column<bool>(type: "boolean", nullable: false),
                    MinPasswordLength = table.Column<int>(type: "integer", nullable: false),
                    PasswordComplexityRules = table.Column<string>(type: "JSON", nullable: true),
                    SessionTimeoutMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: true),
                    StorageQuotaMB = table.Column<long>(type: "bigint", nullable: true),
                    SubscriptionPlan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SupportEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SupportPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "JSON", nullable: true),
                    TenantId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantSettings_ResourceMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ResourceMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TenantSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TenantSettings_Tenants_TenantId1",
                        column: x => x.TenantId1,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrustedDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "text", nullable: false),
                    DeviceName = table.Column<string>(type: "text", nullable: false),
                    DeviceInfo = table.Column<string>(type: "text", nullable: false),
                    TrustedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssociatedIpAddresses = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustedDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrustedDevices_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserMfaConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TotpSecretKey = table.Column<string>(type: "text", nullable: true),
                    BackupCodes = table.Column<string>(type: "text", nullable: true),
                    EnabledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockedOutUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreferredMethod = table.Column<int>(type: "integer", nullable: false),
                    QrCodeSetupData = table.Column<string>(type: "text", nullable: true),
                    IsSetupComplete = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMfaConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMfaConfigurations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserPrivacyAuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SettingName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPrivacyAuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPrivacyAuditLog_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserPrivacySettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    NameVisibility = table.Column<int>(type: "integer", nullable: false),
                    EmailVisibility = table.Column<int>(type: "integer", nullable: false),
                    PhoneVisibility = table.Column<int>(type: "integer", nullable: false),
                    AvatarVisibility = table.Column<int>(type: "integer", nullable: false),
                    BioVisibility = table.Column<int>(type: "integer", nullable: false),
                    LastSeenVisibility = table.Column<int>(type: "integer", nullable: false),
                    OnlineStatusVisibility = table.Column<int>(type: "integer", nullable: false),
                    ActivityFeedVisibility = table.Column<int>(type: "integer", nullable: false),
                    PostsVisibility = table.Column<int>(type: "integer", nullable: false),
                    CommentsVisibility = table.Column<int>(type: "integer", nullable: false),
                    AchievementsVisibility = table.Column<int>(type: "integer", nullable: false),
                    ProjectsVisibility = table.Column<int>(type: "integer", nullable: false),
                    FriendsListVisibility = table.Column<int>(type: "integer", nullable: false),
                    FollowersVisibility = table.Column<int>(type: "integer", nullable: false),
                    FollowingVisibility = table.Column<int>(type: "integer", nullable: false),
                    StatisticsVisibility = table.Column<int>(type: "integer", nullable: false),
                    GamingHistoryVisibility = table.Column<int>(type: "integer", nullable: false),
                    DirectMessagesAllowed = table.Column<int>(type: "integer", nullable: false),
                    MentionsAllowed = table.Column<int>(type: "integer", nullable: false),
                    InvitationsAllowed = table.Column<int>(type: "integer", nullable: false),
                    ShowInSearch = table.Column<bool>(type: "boolean", nullable: false),
                    ShowInDirectory = table.Column<bool>(type: "boolean", nullable: false),
                    ShowReadReceipts = table.Column<bool>(type: "boolean", nullable: false),
                    ShowTypingIndicators = table.Column<bool>(type: "boolean", nullable: false),
                    AllowAnalytics = table.Column<bool>(type: "boolean", nullable: false),
                    AllowPersonalization = table.Column<bool>(type: "boolean", nullable: false),
                    AllowThirdPartyIntegrations = table.Column<bool>(type: "boolean", nullable: false),
                    CustomSettings = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPrivacySettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPrivacySettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    AccessTokenHash = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "text", nullable: true),
                    DeviceInfo = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TerminationReason = table.Column<string>(type: "text", nullable: true),
                    TerminatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsTrustedDevice = table.Column<bool>(type: "boolean", nullable: false),
                    TrustedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlagTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FeatureFlagId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetIdentifier = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsIncluded = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlagTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureFlagTargets_FeatureFlags_FeatureFlagId",
                        column: x => x.FeatureFlagId,
                        principalTable: "FeatureFlags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeatureFlagTargets_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlagUsage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FeatureFlagId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    WasEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReturnedValue = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContextData = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlagUsage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureFlagUsage_FeatureFlags_FeatureFlagId",
                        column: x => x.FeatureFlagId,
                        principalTable: "FeatureFlags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeatureFlagUsage_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserTenantRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantRoleApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTenantRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTenantRoles_TenantRoleApplications_TenantRoleApplicatio~",
                        column: x => x.TenantRoleApplicationId,
                        principalTable: "TenantRoleApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTenantRoles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantReputations_TenantId",
                table: "UserTenantReputations",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email1",
                table: "Users",
                column: "Email1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_CreatedAt",
                table: "UserRoleAssignments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_DeletedAt",
                table: "UserRoleAssignments",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_TenantId",
                table: "UserRoleAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputations_TenantId",
                table: "UserReputations",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_TenantId",
                table: "UserReputationHistory",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_TenantId",
                table: "UserProfiles",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_CreatedAt",
                table: "user_achievements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_DeletedAt",
                table: "user_achievements",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantId",
                table: "Tenants",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplates_Name",
                table: "RoleTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReputationLevels_TenantId",
                table: "ReputationLevels",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReputationActions_TenantId",
                table: "ReputationActions",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_TenantId",
                table: "ProjectTeams",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TenantId",
                table: "Projects",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_TenantId",
                table: "ProjectReleases",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_TenantId",
                table: "ProjectJamSubmissions",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_TenantId",
                table: "ProjectFollowers",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_TenantId",
                table: "ProjectFeedbacks",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_TenantId",
                table: "ProjectCollaborators",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCategory_TenantId",
                table: "ProjectCategory",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_programs_TenantId",
                table: "programs",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId",
                table: "Products",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_posts_TenantId",
                table: "posts",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoles_CreatedAt",
                table: "ModuleRoles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoles_DeletedAt",
                table: "ModuleRoles",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoles_TenantId",
                table: "ModuleRoles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentLicenses_TenantId",
                table: "ContentLicenses",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_achievements_CreatedAt",
                table: "achievements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_achievements_DeletedAt",
                table: "achievements",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_progress_CreatedAt",
                table: "achievement_progress",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_progress_DeletedAt",
                table: "achievement_progress",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_prerequisites_CreatedAt",
                table: "achievement_prerequisites",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_prerequisites_DeletedAt",
                table: "achievement_prerequisites",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_prerequisites_TenantId",
                table: "achievement_prerequisites",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_levels_CreatedAt",
                table: "achievement_levels",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_levels_DeletedAt",
                table: "achievement_levels",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_levels_TenantId",
                table: "achievement_levels",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_DeletedAt",
                table: "AuditLogs",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_CreatedAt",
                table: "BillingWebhookEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_DeletedAt",
                table: "BillingWebhookEvents",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_EventType",
                table: "BillingWebhookEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_IsFailed",
                table: "BillingWebhookEvents",
                column: "IsFailed");

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_IsProcessed",
                table: "BillingWebhookEvents",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_Provider_ExternalEventId",
                table: "BillingWebhookEvents",
                columns: new[] { "Provider", "ExternalEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_SubscriptionId",
                table: "BillingWebhookEvents",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_TenantId",
                table: "BillingWebhookEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_UserId",
                table: "BillingWebhookEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_CreatedAt",
                table: "FeatureFlags",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_DeletedAt",
                table: "FeatureFlags",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_Environment",
                table: "FeatureFlags",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_IsEnabled",
                table: "FeatureFlags",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_IsGlobal",
                table: "FeatureFlags",
                column: "IsGlobal");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_Key",
                table: "FeatureFlags",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_TenantId",
                table: "FeatureFlags",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagTargets_CreatedAt",
                table: "FeatureFlagTargets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagTargets_DeletedAt",
                table: "FeatureFlagTargets",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagTargets_FeatureFlagId_TargetType_TargetIdentifier",
                table: "FeatureFlagTargets",
                columns: new[] { "FeatureFlagId", "TargetType", "TargetIdentifier" });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagTargets_TargetType",
                table: "FeatureFlagTargets",
                column: "TargetType");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagTargets_TenantId",
                table: "FeatureFlagTargets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagUsage_CreatedAt",
                table: "FeatureFlagUsage",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagUsage_DeletedAt",
                table: "FeatureFlagUsage",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagUsage_FeatureFlagId",
                table: "FeatureFlagUsage",
                column: "FeatureFlagId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagUsage_FeatureFlagId_CreatedAt",
                table: "FeatureFlagUsage",
                columns: new[] { "FeatureFlagId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagUsage_TenantId",
                table: "FeatureFlagUsage",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagUsage_UserId",
                table: "FeatureFlagUsage",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagUsage_WasEnabled",
                table: "FeatureFlagUsage",
                column: "WasEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_CreatedAt",
                table: "LoginAttempts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_DeletedAt",
                table: "LoginAttempts",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_TenantId",
                table: "LoginAttempts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaAttempts_CreatedAt",
                table: "MfaAttempts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MfaAttempts_DeletedAt",
                table: "MfaAttempts",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MfaAttempts_TenantId",
                table: "MfaAttempts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_CreatedAt",
                table: "NotificationPreferences",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_DeletedAt",
                table: "NotificationPreferences",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_TenantId",
                table: "NotificationPreferences",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DeletedAt",
                table: "Notifications",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId",
                table: "Notifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceQuotas_CreatedAt",
                table: "ResourceQuotas",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceQuotas_DeletedAt",
                table: "ResourceQuotas",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceQuotas_TenantId_Type",
                table: "ResourceQuotas",
                columns: new[] { "TenantId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceUsageRecords_CreatedAt",
                table: "ResourceUsageRecords",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceUsageRecords_DeletedAt",
                table: "ResourceUsageRecords",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceUsageRecords_TenantId_Type_PeriodStart",
                table: "ResourceUsageRecords",
                columns: new[] { "TenantId", "Type", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleApplications_CreatedAt",
                table: "TenantRoleApplications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleApplications_DeletedAt",
                table: "TenantRoleApplications",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleApplications_RoleTemplateId",
                table: "TenantRoleApplications",
                column: "RoleTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleApplications_TenantId_RoleTemplateId",
                table: "TenantRoleApplications",
                columns: new[] { "TenantId", "RoleTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_CreatedAt",
                table: "TenantSettings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_DeletedAt",
                table: "TenantSettings",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_MetadataId",
                table: "TenantSettings",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId",
                table: "TenantSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId1",
                table: "TenantSettings",
                column: "TenantId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrustedDevices_CreatedAt",
                table: "TrustedDevices",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrustedDevices_DeletedAt",
                table: "TrustedDevices",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrustedDevices_TenantId",
                table: "TrustedDevices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMfaConfigurations_CreatedAt",
                table: "UserMfaConfigurations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserMfaConfigurations_DeletedAt",
                table: "UserMfaConfigurations",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserMfaConfigurations_TenantId",
                table: "UserMfaConfigurations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPrivacyAuditLog_CreatedAt",
                table: "UserPrivacyAuditLog",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserPrivacyAuditLog_DeletedAt",
                table: "UserPrivacyAuditLog",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserPrivacyAuditLog_TenantId",
                table: "UserPrivacyAuditLog",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPrivacyAuditLog_UserId_CreatedAt",
                table: "UserPrivacyAuditLog",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPrivacySettings_CreatedAt",
                table: "UserPrivacySettings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserPrivacySettings_DeletedAt",
                table: "UserPrivacySettings",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserPrivacySettings_TenantId",
                table: "UserPrivacySettings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPrivacySettings_UserId",
                table: "UserPrivacySettings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_CreatedAt",
                table: "UserSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_DeletedAt",
                table: "UserSessions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_TenantId",
                table: "UserSessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantRoles_CreatedAt",
                table: "UserTenantRoles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantRoles_DeletedAt",
                table: "UserTenantRoles",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantRoles_TenantId",
                table: "UserTenantRoles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantRoles_TenantRoleApplicationId",
                table: "UserTenantRoles",
                column: "TenantRoleApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantRoles_UserId_TenantRoleApplicationId",
                table: "UserTenantRoles",
                columns: new[] { "UserId", "TenantRoleApplicationId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_achievement_levels_Tenants_TenantId",
                table: "achievement_levels",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_achievement_prerequisites_Tenants_TenantId",
                table: "achievement_prerequisites",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_achievement_progress_Tenants_TenantId",
                table: "achievement_progress",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_achievements_Tenants_TenantId",
                table: "achievements",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModuleRoles_Tenants_TenantId",
                table: "ModuleRoles",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceLocalizations_TenantSettings_TenantSettingsId",
                table: "ResourceLocalizations",
                column: "TenantSettingsId",
                principalTable: "TenantSettings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_achievements_Tenants_TenantId",
                table: "user_achievements",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleAssignments_Tenants_TenantId",
                table: "UserRoleAssignments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_achievement_levels_Tenants_TenantId",
                table: "achievement_levels");

            migrationBuilder.DropForeignKey(
                name: "FK_achievement_prerequisites_Tenants_TenantId",
                table: "achievement_prerequisites");

            migrationBuilder.DropForeignKey(
                name: "FK_achievement_progress_Tenants_TenantId",
                table: "achievement_progress");

            migrationBuilder.DropForeignKey(
                name: "FK_achievements_Tenants_TenantId",
                table: "achievements");

            migrationBuilder.DropForeignKey(
                name: "FK_ModuleRoles_Tenants_TenantId",
                table: "ModuleRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceLocalizations_TenantSettings_TenantSettingsId",
                table: "ResourceLocalizations");

            migrationBuilder.DropForeignKey(
                name: "FK_user_achievements_Tenants_TenantId",
                table: "user_achievements");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleAssignments_Tenants_TenantId",
                table: "UserRoleAssignments");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BillingWebhookEvents");

            migrationBuilder.DropTable(
                name: "FeatureFlagTargets");

            migrationBuilder.DropTable(
                name: "FeatureFlagUsage");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "MfaAttempts");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ResourceQuotas");

            migrationBuilder.DropTable(
                name: "ResourceUsageRecords");

            migrationBuilder.DropTable(
                name: "TenantSettings");

            migrationBuilder.DropTable(
                name: "TrustedDevices");

            migrationBuilder.DropTable(
                name: "UserMfaConfigurations");

            migrationBuilder.DropTable(
                name: "UserPrivacyAuditLog");

            migrationBuilder.DropTable(
                name: "UserPrivacySettings");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "UserTenantRoles");

            migrationBuilder.DropTable(
                name: "FeatureFlags");

            migrationBuilder.DropTable(
                name: "TenantRoleApplications");

            migrationBuilder.DropIndex(
                name: "IX_UserTenantReputations_TenantId",
                table: "UserTenantReputations");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserRoleAssignments_CreatedAt",
                table: "UserRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_UserRoleAssignments_DeletedAt",
                table: "UserRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_UserRoleAssignments_TenantId",
                table: "UserRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_UserReputations_TenantId",
                table: "UserReputations");

            migrationBuilder.DropIndex(
                name: "IX_UserReputationHistory_TenantId",
                table: "UserReputationHistory");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_TenantId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_user_achievements_CreatedAt",
                table: "user_achievements");

            migrationBuilder.DropIndex(
                name: "IX_user_achievements_DeletedAt",
                table: "user_achievements");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_TenantId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_RoleTemplates_Name",
                table: "RoleTemplates");

            migrationBuilder.DropIndex(
                name: "IX_ReputationLevels_TenantId",
                table: "ReputationLevels");

            migrationBuilder.DropIndex(
                name: "IX_ReputationActions_TenantId",
                table: "ReputationActions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTeams_TenantId",
                table: "ProjectTeams");

            migrationBuilder.DropIndex(
                name: "IX_Projects_TenantId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ProjectReleases_TenantId",
                table: "ProjectReleases");

            migrationBuilder.DropIndex(
                name: "IX_ProjectJamSubmissions_TenantId",
                table: "ProjectJamSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectFollowers_TenantId",
                table: "ProjectFollowers");

            migrationBuilder.DropIndex(
                name: "IX_ProjectFeedbacks_TenantId",
                table: "ProjectFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectCollaborators_TenantId",
                table: "ProjectCollaborators");

            migrationBuilder.DropIndex(
                name: "IX_ProjectCategory_TenantId",
                table: "ProjectCategory");

            migrationBuilder.DropIndex(
                name: "IX_programs_TenantId",
                table: "programs");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_posts_TenantId",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_ModuleRoles_CreatedAt",
                table: "ModuleRoles");

            migrationBuilder.DropIndex(
                name: "IX_ModuleRoles_DeletedAt",
                table: "ModuleRoles");

            migrationBuilder.DropIndex(
                name: "IX_ModuleRoles_TenantId",
                table: "ModuleRoles");

            migrationBuilder.DropIndex(
                name: "IX_ContentLicenses_TenantId",
                table: "ContentLicenses");

            migrationBuilder.DropIndex(
                name: "IX_achievements_CreatedAt",
                table: "achievements");

            migrationBuilder.DropIndex(
                name: "IX_achievements_DeletedAt",
                table: "achievements");

            migrationBuilder.DropIndex(
                name: "IX_achievement_progress_CreatedAt",
                table: "achievement_progress");

            migrationBuilder.DropIndex(
                name: "IX_achievement_progress_DeletedAt",
                table: "achievement_progress");

            migrationBuilder.DropIndex(
                name: "IX_achievement_prerequisites_CreatedAt",
                table: "achievement_prerequisites");

            migrationBuilder.DropIndex(
                name: "IX_achievement_prerequisites_DeletedAt",
                table: "achievement_prerequisites");

            migrationBuilder.DropIndex(
                name: "IX_achievement_prerequisites_TenantId",
                table: "achievement_prerequisites");

            migrationBuilder.DropIndex(
                name: "IX_achievement_levels_CreatedAt",
                table: "achievement_levels");

            migrationBuilder.DropIndex(
                name: "IX_achievement_levels_DeletedAt",
                table: "achievement_levels");

            migrationBuilder.DropIndex(
                name: "IX_achievement_levels_TenantId",
                table: "achievement_levels");

            migrationBuilder.DropColumn(
                name: "Email1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "UserRoleAssignments");

            migrationBuilder.DropColumn(
                name: "AutoRenew",
                table: "user_subscriptions");

            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "user_subscriptions");

            migrationBuilder.DropColumn(
                name: "BillingCycleCount",
                table: "user_subscriptions");

            migrationBuilder.DropColumn(
                name: "CancellationNote",
                table: "user_subscriptions");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "user_subscriptions");

            migrationBuilder.DropColumn(
                name: "ExternalCustomerId",
                table: "user_subscriptions");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "user_subscriptions");

            migrationBuilder.DropColumn(
                name: "amount",
                table: "user_subscriptions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "user_achievements");

            migrationBuilder.DropColumn(
                name: "AdminEmail",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "RoleTemplates");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "RoleTemplates");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "RoleTemplates");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "RoleTemplates");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "RoleTemplates");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "ProjectPermissions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ModuleRoles");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "CommentPermissions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "achievements");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "achievement_progress");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "achievement_prerequisites");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "achievement_prerequisites");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "achievement_levels");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "achievement_levels");

            migrationBuilder.RenameColumn(
                name: "PermissionDefinitions",
                table: "RoleTemplates",
                newName: "PermissionTemplatesJson");

            migrationBuilder.RenameColumn(
                name: "TenantSettingsId",
                table: "ResourceLocalizations",
                newName: "CommentId");

            migrationBuilder.RenameIndex(
                name: "IX_ResourceLocalizations_TenantSettingsId",
                table: "ResourceLocalizations",
                newName: "IX_ResourceLocalizations_CommentId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "Users",
                type: "numeric(18,8)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AvailableBalance",
                table: "Users",
                type: "numeric(18,8)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserRoleAssignments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "UserRoleAssignments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "UserRoleAssignments",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserRoleAssignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UserRoleAssignments",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "RoleTemplateId",
                table: "UserPermissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "user_achievements",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "user_achievements",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "user_achievements",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "user_achievements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "user_achievements",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "RoleTemplates",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PermissionFlags1",
                table: "ProjectPermissions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "PermissionFlags2",
                table: "ProjectPermissions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ModuleRoles",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ModuleRoles",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ModuleRoles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ModuleRoles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ModuleRoles",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<long>(
                name: "PermissionFlags1",
                table: "CommentPermissions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "PermissionFlags2",
                table: "CommentPermissions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "achievements",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "achievements",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "achievements",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "achievements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "achievements",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "achievement_progress",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "achievement_progress",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "achievement_progress",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "achievement_progress",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "achievement_progress",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "achievement_prerequisites",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "achievement_prerequisites",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "achievement_prerequisites",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "achievement_prerequisites",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "achievement_prerequisites",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "achievement_levels",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "achievement_levels",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "achievement_levels",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "achievement_levels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "achievement_levels",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "Comment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comment_ResourceMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ResourceMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Comment_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantReputations_TenantId",
                table: "UserTenantReputations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputations_TenantId",
                table: "UserReputations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_TenantId",
                table: "UserReputationHistory",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_TenantId",
                table: "UserProfiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_RoleTemplateId",
                table: "UserPermissions",
                column: "RoleTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantId",
                table: "Tenants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationLevels_TenantId",
                table: "ReputationLevels",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationActions_TenantId",
                table: "ReputationActions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_TenantId",
                table: "ProjectTeams",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TenantId",
                table: "Projects",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_TenantId",
                table: "ProjectReleases",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_TenantId",
                table: "ProjectJamSubmissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_TenantId",
                table: "ProjectFollowers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_TenantId",
                table: "ProjectFeedbacks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_TenantId",
                table: "ProjectCollaborators",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCategory_TenantId",
                table: "ProjectCategory",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_programs_TenantId",
                table: "programs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId",
                table: "Products",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_posts_TenantId",
                table: "posts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentLicenses_TenantId",
                table: "ContentLicenses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_CreatedAt",
                table: "Comment",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_DeletedAt",
                table: "Comment",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_MetadataId",
                table: "Comment",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_TenantId",
                table: "Comment",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommentPermissions_Comment_ResourceId",
                table: "CommentPermissions",
                column: "ResourceId",
                principalTable: "Comment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommentPermissions_Users_UserId",
                table: "CommentPermissions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectPermissions_Projects_ResourceId",
                table: "ProjectPermissions",
                column: "ResourceId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectPermissions_Users_UserId",
                table: "ProjectPermissions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceLocalizations_Comment_CommentId",
                table: "ResourceLocalizations",
                column: "CommentId",
                principalTable: "Comment",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserPermissions_RoleTemplates_RoleTemplateId",
                table: "UserPermissions",
                column: "RoleTemplateId",
                principalTable: "RoleTemplates",
                principalColumn: "Id");
        }
    }
}
