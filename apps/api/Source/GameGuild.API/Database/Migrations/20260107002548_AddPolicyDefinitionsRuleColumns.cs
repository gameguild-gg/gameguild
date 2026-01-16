using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyDefinitionsRuleColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantDomains_Tenants_TenantId1",
                table: "TenantDomains");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantMembers_Tenants_TenantId1",
                table: "TenantMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantSettings_Tenants_TenantId1",
                table: "TenantSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantStatistics_Tenants_TenantId1",
                table: "TenantStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_UsageTracking_Tenants_TenantId1",
                table: "UsageTracking");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_financial_ledger_entries_revenue_events_RevenueEventId",
                table: "financial_ledger_entries");

            migrationBuilder.DropTable(
                name: "audit_trails");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "costallocationreport",
                schema: "gameguild.resources");

            migrationBuilder.DropTable(
                name: "resourcethrottlingpolicy",
                schema: "gameguild.resources");

            migrationBuilder.DropTable(
                name: "resourceusagetrend",
                schema: "gameguild.resources");

            migrationBuilder.DropTable(
                name: "slaimpactanalysis",
                schema: "gameguild.resources");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "usage_records",
                schema: "resources");

            migrationBuilder.DropTable(
                name: "usageretentionpolicy",
                schema: "gameguild.resources");

            migrationBuilder.DropTable(
                name: "resource_quotas",
                schema: "resources");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "revenue_events");

            migrationBuilder.DropTable(
                name: "financial_ledger_entries");

            migrationBuilder.DropIndex(
                name: "IX_UsageTracking_TenantId1",
                table: "UsageTracking");

            migrationBuilder.DropIndex(
                name: "IX_TenantStatistics_TenantId1",
                table: "TenantStatistics");

            migrationBuilder.DropIndex(
                name: "IX_TenantSettings_TenantId1",
                table: "TenantSettings");

            migrationBuilder.DropIndex(
                name: "IX_TenantMembers_TenantId1",
                table: "TenantMembers");

            migrationBuilder.DropIndex(
                name: "IX_TenantDomains_TenantId1",
                table: "TenantDomains");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSessions",
                table: "UserSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRoles",
                table: "UserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserMfaConfigurations",
                table: "UserMfaConfigurations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrustedDevices",
                table: "TrustedDevices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TenantPermissions",
                table: "TenantPermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MfaAttempts",
                table: "MfaAttempts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConditionalPolicies",
                table: "ConditionalPolicies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuthUsers",
                table: "AuthUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuthenticationAttempts",
                table: "AuthenticationAttempts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AbacPolicies",
                table: "AbacPolicies");

            migrationBuilder.DropColumn(
                name: "TenantId1",
                table: "UsageTracking");

            migrationBuilder.DropColumn(
                name: "TenantId1",
                table: "TenantStatistics");

            migrationBuilder.DropColumn(
                name: "TenantId1",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "TenantId1",
                table: "TenantMembers");

            migrationBuilder.DropColumn(
                name: "TenantId1",
                table: "TenantDomains");

            migrationBuilder.EnsureSchema(
                name: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "UserSessions",
                newName: "usersession",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                newName: "user_role",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "UserMfaConfigurations",
                newName: "user_mfa_configuration",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "TrustedDevices",
                newName: "trusteddevice",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "TenantPermissions",
                newName: "tenantpermission",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "role",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                newName: "refreshtoken",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "MfaAttempts",
                newName: "mfaattempt",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "ConditionalPolicies",
                newName: "conditionalpolicy",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "AuthUsers",
                newName: "authuser",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "AuthenticationAttempts",
                newName: "authenticationattempt",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "AbacPolicies",
                newName: "abacpolicy",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "usersession",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "user_role",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "gameguild.authentication",
                table: "user_role",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "gameguild.authentication",
                table: "user_role",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                schema: "gameguild.authentication",
                table: "user_role",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                schema: "gameguild.authentication",
                table: "user_role",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "gameguild.authentication",
                table: "user_role",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AssignedBy",
                schema: "gameguild.authentication",
                table: "user_role",
                newName: "assigned_by");

            migrationBuilder.RenameColumn(
                name: "AssignedAt",
                schema: "gameguild.authentication",
                table: "user_role",
                newName: "assigned_at");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_RoleId",
                schema: "gameguild.authentication",
                table: "user_role",
                newName: "idx_user_role_role_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TotpSecretKey",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                newName: "totp_secret_key");

            migrationBuilder.RenameColumn(
                name: "QrCodeSetupData",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                newName: "qr_code_setup_data");

            migrationBuilder.RenameColumn(
                name: "PreferredMethod",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                newName: "preferred_method");

            migrationBuilder.RenameColumn(
                name: "LockedOutUntil",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                newName: "locked_out_until");

            migrationBuilder.RenameColumn(
                name: "LastUsedAt",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                newName: "last_used_at");

            migrationBuilder.RenameColumn(
                name: "IsSetupComplete",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                newName: "is_setup_complete");

            migrationBuilder.RenameColumn(
                name: "IsEnabled",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                newName: "is_enabled");

            migrationBuilder.RenameColumn(
                name: "FailedAttempts",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                newName: "failed_attempts");

            migrationBuilder.RenameColumn(
                name: "EnabledAt",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                newName: "enabled_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "BackupCodes",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                newName: "backup_codes");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "trusteddevice",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "tenantpermission",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Permissions",
                schema: "gameguild.authentication",
                table: "role",
                newName: "permissions");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "gameguild.authentication",
                table: "role",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                schema: "gameguild.authentication",
                table: "role",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "role",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "gameguild.authentication",
                table: "role",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "gameguild.authentication",
                table: "role",
                newName: "tenant_id");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                schema: "gameguild.authentication",
                table: "role",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "gameguild.authentication",
                table: "role",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "refreshtoken",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "mfaattempt",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "conditionalpolicy",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "authuser",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "authenticationattempt",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                newName: "id");

            migrationBuilder.AlterColumn<string>(
                name: "preferred_method",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.Sql(@"
                ALTER TABLE ""gameguild.authentication"".role 
                ALTER COLUMN permissions TYPE jsonb USING permissions::jsonb;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "gameguild.authentication",
                table: "role",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "gameguild.authentication",
                table: "role",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                schema: "gameguild.authentication",
                table: "role",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddPrimaryKey(
                name: "PK_usersession",
                schema: "gameguild.authentication",
                table: "usersession",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_role",
                schema: "gameguild.authentication",
                table: "user_role",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_mfa_configuration",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_trusteddevice",
                schema: "gameguild.authentication",
                table: "trusteddevice",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tenantpermission",
                schema: "gameguild.authentication",
                table: "tenantpermission",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_role",
                schema: "gameguild.authentication",
                table: "role",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_refreshtoken",
                schema: "gameguild.authentication",
                table: "refreshtoken",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_mfaattempt",
                schema: "gameguild.authentication",
                table: "mfaattempt",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_conditionalpolicy",
                schema: "gameguild.authentication",
                table: "conditionalpolicy",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_authuser",
                schema: "gameguild.authentication",
                table: "authuser",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_authenticationattempt",
                schema: "gameguild.authentication",
                table: "authenticationattempt",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_abacpolicy",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                column: "id");

            migrationBuilder.CreateTable(
                name: "AccessControlListEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrincipalType = table.Column<int>(type: "integer", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccessLevel = table.Column<int>(type: "integer", nullable: false),
                    IsDenied = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessControlListEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "accessreviewcampaign",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReminderFrequencyDays = table.Column<int>(type: "integer", nullable: true),
                    AutoRevokeOnExpiry = table.Column<bool>(type: "boolean", nullable: false),
                    GracePeriodDays = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FilterCriteria = table.Column<string>(type: "text", nullable: true),
                    Instructions = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accessreviewcampaign", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "blockchaincertificateanchor",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateType = table.Column<string>(type: "text", nullable: false),
                    CertificateHash = table.Column<string>(type: "text", nullable: false),
                    CertificateData = table.Column<string>(type: "text", nullable: false),
                    TransactionHash = table.Column<string>(type: "text", nullable: false),
                    BlockchainNetwork = table.Column<string>(type: "text", nullable: false),
                    BlockNumber = table.Column<long>(type: "bigint", nullable: true),
                    AnchoredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "text", nullable: true),
                    RevocationTransactionHash = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blockchaincertificateanchor", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contenttypepermission",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentTypeName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Permissions = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    GrantedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenttypepermission", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "identityverification",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    VerifiedValue = table.Column<string>(type: "text", nullable: false),
                    InitiatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationProvider = table.Column<string>(type: "text", nullable: true),
                    ExternalVerificationId = table.Column<string>(type: "text", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DocumentIds = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identityverification", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PolicyDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequireAuthentication = table.Column<bool>(type: "boolean", nullable: false),
                    AuthenticationSchemesJson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RequiredPermissionsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RequiredRolesJson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RequiredClaimsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RequireTenantMatch = table.Column<bool>(type: "boolean", nullable: false),
                    EnvironmentConstraintsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RequireResourceOwnership = table.Column<bool>(type: "boolean", nullable: false),
                    RequireAccessControlListAccess = table.Column<bool>(type: "boolean", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MinimumAccessLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsTenantScoped = table.Column<bool>(type: "boolean", nullable: false),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RulesJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    UseRuleBasedEvaluation = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyDefinitions", x => x.Id);
                });

            // Set default value for UpdatedAt column in PolicyDefinitions table
            migrationBuilder.Sql(@"
                ALTER TABLE ""PolicyDefinitions""
                ALTER COLUMN ""UpdatedAt"" SET DEFAULT NOW();
            ");

            migrationBuilder.CreateTable(
                name: "TenantSecurityVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSecurityVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "accessreviewitem",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceType = table.Column<string>(type: "text", nullable: true),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    Permissions = table.Column<string>(type: "text", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Decision = table.Column<int>(type: "integer", nullable: true),
                    DecisionReason = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastReminderSent = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemindersSent = table.Column<int>(type: "integer", nullable: false),
                    ContextInfo = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accessreviewitem", x => x.id);
                    table.ForeignKey(
                        name: "FK_accessreviewitem_accessreviewcampaign_CampaignId",
                        column: x => x.CampaignId,
                        principalSchema: "gameguild.authentication",
                        principalTable: "accessreviewcampaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_user_role_assigned_by",
                schema: "gameguild.authentication",
                table: "user_role",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "idx_user_role_expires_at",
                schema: "gameguild.authentication",
                table: "user_role",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_user_role_user_id",
                schema: "gameguild.authentication",
                table: "user_role",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_role_user_id_role_id",
                schema: "gameguild.authentication",
                table: "user_role",
                columns: new[] { "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_mfa_configuration_user_id",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_role_is_active",
                schema: "gameguild.authentication",
                table: "role",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_role_name",
                schema: "gameguild.authentication",
                table: "role",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_role_name_tenant_id",
                schema: "gameguild.authentication",
                table: "role",
                columns: new[] { "name", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_role_tenant_id",
                schema: "gameguild.authentication",
                table: "role",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_AccessControlListEntries_ResourceType_ResourceId",
                table: "AccessControlListEntries",
                columns: new[] { "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessControlListEntries_TenantId_PrincipalType_PrincipalId",
                table: "AccessControlListEntries",
                columns: new[] { "TenantId", "PrincipalType", "PrincipalId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessControlListEntries_TenantId_PrincipalType_PrincipalId~",
                table: "AccessControlListEntries",
                columns: new[] { "TenantId", "PrincipalType", "PrincipalId", "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessControlListEntries_TenantId_ResourceType_ResourceId",
                table: "AccessControlListEntries",
                columns: new[] { "TenantId", "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessControlListEntries_TenantId_ResourceType_ResourceId_I~",
                table: "AccessControlListEntries",
                columns: new[] { "TenantId", "ResourceType", "ResourceId", "IsDenied" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessControlListEntries_TenantId_ResourceType_ResourceId_P~",
                table: "AccessControlListEntries",
                columns: new[] { "TenantId", "ResourceType", "ResourceId", "PrincipalType", "PrincipalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accessreviewitem_CampaignId",
                schema: "gameguild.authentication",
                table: "accessreviewitem",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyDefinitions_PolicyName_TenantId",
                table: "PolicyDefinitions",
                columns: new[] { "PolicyName", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSecurityVersions_TenantId",
                table: "TenantSecurityVersions",
                column: "TenantId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_user_role_role_role_id",
                schema: "gameguild.authentication",
                table: "user_role",
                column: "role_id",
                principalSchema: "gameguild.authentication",
                principalTable: "role",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_role_role_role_id",
                schema: "gameguild.authentication",
                table: "user_role");

            migrationBuilder.DropTable(
                name: "AccessControlListEntries");

            migrationBuilder.DropTable(
                name: "accessreviewitem",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "blockchaincertificateanchor",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "contenttypepermission",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "identityverification",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "PolicyDefinitions");

            migrationBuilder.DropTable(
                name: "TenantSecurityVersions");

            migrationBuilder.DropTable(
                name: "accessreviewcampaign",
                schema: "gameguild.authentication");

            migrationBuilder.DropPrimaryKey(
                name: "PK_usersession",
                schema: "gameguild.authentication",
                table: "usersession");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_role",
                schema: "gameguild.authentication",
                table: "user_role");

            migrationBuilder.DropIndex(
                name: "idx_user_role_assigned_by",
                schema: "gameguild.authentication",
                table: "user_role");

            migrationBuilder.DropIndex(
                name: "idx_user_role_expires_at",
                schema: "gameguild.authentication",
                table: "user_role");

            migrationBuilder.DropIndex(
                name: "idx_user_role_user_id",
                schema: "gameguild.authentication",
                table: "user_role");

            migrationBuilder.DropIndex(
                name: "idx_user_role_user_id_role_id",
                schema: "gameguild.authentication",
                table: "user_role");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_mfa_configuration",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration");

            migrationBuilder.DropIndex(
                name: "ix_user_mfa_configuration_user_id",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration");

            migrationBuilder.DropPrimaryKey(
                name: "PK_trusteddevice",
                schema: "gameguild.authentication",
                table: "trusteddevice");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tenantpermission",
                schema: "gameguild.authentication",
                table: "tenantpermission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_role",
                schema: "gameguild.authentication",
                table: "role");

            migrationBuilder.DropIndex(
                name: "idx_role_is_active",
                schema: "gameguild.authentication",
                table: "role");

            migrationBuilder.DropIndex(
                name: "idx_role_name",
                schema: "gameguild.authentication",
                table: "role");

            migrationBuilder.DropIndex(
                name: "idx_role_name_tenant_id",
                schema: "gameguild.authentication",
                table: "role");

            migrationBuilder.DropIndex(
                name: "idx_role_tenant_id",
                schema: "gameguild.authentication",
                table: "role");

            migrationBuilder.DropPrimaryKey(
                name: "PK_refreshtoken",
                schema: "gameguild.authentication",
                table: "refreshtoken");

            migrationBuilder.DropPrimaryKey(
                name: "PK_mfaattempt",
                schema: "gameguild.authentication",
                table: "mfaattempt");

            migrationBuilder.DropPrimaryKey(
                name: "PK_conditionalpolicy",
                schema: "gameguild.authentication",
                table: "conditionalpolicy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_authuser",
                schema: "gameguild.authentication",
                table: "authuser");

            migrationBuilder.DropPrimaryKey(
                name: "PK_authenticationattempt",
                schema: "gameguild.authentication",
                table: "authenticationattempt");

            migrationBuilder.DropPrimaryKey(
                name: "PK_abacpolicy",
                schema: "gameguild.authentication",
                table: "abacpolicy");

            migrationBuilder.EnsureSchema(
                name: "gameguild.resources");

            migrationBuilder.EnsureSchema(
                name: "resources");

            migrationBuilder.RenameTable(
                name: "usersession",
                schema: "gameguild.authentication",
                newName: "UserSessions");

            migrationBuilder.RenameTable(
                name: "user_role",
                schema: "gameguild.authentication",
                newName: "UserRoles");

            migrationBuilder.RenameTable(
                name: "user_mfa_configuration",
                schema: "gameguild.authentication",
                newName: "UserMfaConfigurations");

            migrationBuilder.RenameTable(
                name: "trusteddevice",
                schema: "gameguild.authentication",
                newName: "TrustedDevices");

            migrationBuilder.RenameTable(
                name: "tenantpermission",
                schema: "gameguild.authentication",
                newName: "TenantPermissions");

            migrationBuilder.RenameTable(
                name: "role",
                schema: "gameguild.authentication",
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "refreshtoken",
                schema: "gameguild.authentication",
                newName: "RefreshTokens");

            migrationBuilder.RenameTable(
                name: "mfaattempt",
                schema: "gameguild.authentication",
                newName: "MfaAttempts");

            migrationBuilder.RenameTable(
                name: "conditionalpolicy",
                schema: "gameguild.authentication",
                newName: "ConditionalPolicies");

            migrationBuilder.RenameTable(
                name: "authuser",
                schema: "gameguild.authentication",
                newName: "AuthUsers");

            migrationBuilder.RenameTable(
                name: "authenticationattempt",
                schema: "gameguild.authentication",
                newName: "AuthenticationAttempts");

            migrationBuilder.RenameTable(
                name: "abacpolicy",
                schema: "gameguild.authentication",
                newName: "AbacPolicies");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "UserSessions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "UserRoles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "UserRoles",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "UserRoles",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "UserRoles",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                table: "UserRoles",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "UserRoles",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "assigned_by",
                table: "UserRoles",
                newName: "AssignedBy");

            migrationBuilder.RenameColumn(
                name: "assigned_at",
                table: "UserRoles",
                newName: "AssignedAt");

            migrationBuilder.RenameIndex(
                name: "idx_user_role_role_id",
                table: "UserRoles",
                newName: "IX_UserRoles_RoleId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "UserMfaConfigurations",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "UserMfaConfigurations",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "UserMfaConfigurations",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "totp_secret_key",
                table: "UserMfaConfigurations",
                newName: "TotpSecretKey");

            migrationBuilder.RenameColumn(
                name: "qr_code_setup_data",
                table: "UserMfaConfigurations",
                newName: "QrCodeSetupData");

            migrationBuilder.RenameColumn(
                name: "preferred_method",
                table: "UserMfaConfigurations",
                newName: "PreferredMethod");

            migrationBuilder.RenameColumn(
                name: "locked_out_until",
                table: "UserMfaConfigurations",
                newName: "LockedOutUntil");

            migrationBuilder.RenameColumn(
                name: "last_used_at",
                table: "UserMfaConfigurations",
                newName: "LastUsedAt");

            migrationBuilder.RenameColumn(
                name: "is_setup_complete",
                table: "UserMfaConfigurations",
                newName: "IsSetupComplete");

            migrationBuilder.RenameColumn(
                name: "is_enabled",
                table: "UserMfaConfigurations",
                newName: "IsEnabled");

            migrationBuilder.RenameColumn(
                name: "failed_attempts",
                table: "UserMfaConfigurations",
                newName: "FailedAttempts");

            migrationBuilder.RenameColumn(
                name: "enabled_at",
                table: "UserMfaConfigurations",
                newName: "EnabledAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "UserMfaConfigurations",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "backup_codes",
                table: "UserMfaConfigurations",
                newName: "BackupCodes");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "TrustedDevices",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "TenantPermissions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "permissions",
                table: "Roles",
                newName: "Permissions");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Roles",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Roles",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Roles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Roles",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "Roles",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Roles",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Roles",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "RefreshTokens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "MfaAttempts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ConditionalPolicies",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AuthUsers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AuthenticationAttempts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AbacPolicies",
                newName: "Id");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId1",
                table: "UsageTracking",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId1",
                table: "TenantStatistics",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId1",
                table: "TenantSettings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId1",
                table: "TenantMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId1",
                table: "TenantDomains",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PreferredMethod",
                table: "UserMfaConfigurations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Permissions",
                table: "Roles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Roles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Roles",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSessions",
                table: "UserSessions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRoles",
                table: "UserRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserMfaConfigurations",
                table: "UserMfaConfigurations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrustedDevices",
                table: "TrustedDevices",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TenantPermissions",
                table: "TenantPermissions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                table: "Roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MfaAttempts",
                table: "MfaAttempts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConditionalPolicies",
                table: "ConditionalPolicies",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuthUsers",
                table: "AuthUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuthenticationAttempts",
                table: "AuthenticationAttempts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AbacPolicies",
                table: "AbacPolicies",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "audit_trails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OldValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_trails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    ResourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "costallocationreport",
                schema: "gameguild.resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationTags = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CostCenter = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CostPerUnit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InvoiceReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsExported = table.Column<bool>(type: "boolean", nullable: false),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Project = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ResourceUsageType = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalUsage = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_costallocationreport", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resource_quotas",
                schema: "resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Unique identifier for the resource quota"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "When the quota was created"),
                    CurrentUsage = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L, comment: "Current usage amount"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HardLimit = table.Column<long>(type: "bigint", nullable: true, comment: "Hard limit (enforcement threshold)"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Whether this quota is actively enforced"),
                    LastReset = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NotificationThresholds = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false, comment: "Period type for quota reset"),
                    ResetDayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    ResetDayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    ResetTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    SoftLimit = table.Column<long>(type: "bigint", nullable: true, comment: "Soft limit (warning threshold)"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Tenant that owns this quota"),
                    Type = table.Column<int>(type: "integer", nullable: false, comment: "Type of resource being limited"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "When the quota was last updated"),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_quotas", x => x.Id);
                    table.CheckConstraint("CK_ResourceQuota_CurrentUsage_LessEqual_MaxUsage", "\"HardLimit\" IS NULL OR \"CurrentUsage\" <= \"HardLimit\"");
                    table.CheckConstraint("CK_ResourceQuota_CurrentUsage_NonNegative", "\"CurrentUsage\" >= 0");
                    table.CheckConstraint("CK_ResourceQuota_MaxUsage_NonNegative", "\"HardLimit\" IS NULL OR \"HardLimit\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "resourcethrottlingpolicy",
                schema: "gameguild.resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Configuration = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DegradationFactor = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaxRequestsPerWindow = table.Column<int>(type: "integer", nullable: true),
                    PriorityThreshold = table.Column<int>(type: "integer", nullable: true),
                    ResourceType = table.Column<int>(type: "integer", nullable: false),
                    Strategy = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ThrottlingThresholdPercent = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    WindowDurationSeconds = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resourcethrottlingpolicy", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resourceusagetrend",
                schema: "gameguild.resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnomalyCount = table.Column<int>(type: "integer", nullable: false),
                    AverageUsage = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GrowthRate = table.Column<double>(type: "double precision", nullable: false),
                    MaxUsage = table.Column<long>(type: "bigint", nullable: false),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MinUsage = table.Column<long>(type: "bigint", nullable: false),
                    Pattern = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PatternConfidence = table.Column<double>(type: "double precision", nullable: false),
                    PeakUsageTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResourceType = table.Column<int>(type: "integer", nullable: false),
                    StandardDeviation = table.Column<double>(type: "double precision", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resourceusagetrend", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnnualPriceInCents = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Features = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HasAdvancedAnalytics = table.Column<bool>(type: "boolean", nullable: false),
                    HasCustomBranding = table.Column<bool>(type: "boolean", nullable: false),
                    HasPrioritySupport = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    MaxApiCallsPerMonth = table.Column<long>(type: "bigint", nullable: true),
                    MaxStorageMb = table.Column<long>(type: "bigint", nullable: true),
                    MaxUsers = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MonthlyPriceInCents = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TrialPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usage_records",
                schema: "resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Unique identifier for the usage record"),
                    AveragePerDay = table.Column<double>(type: "double precision", nullable: true, comment: "Average usage per day"),
                    Count = table.Column<long>(type: "bigint", nullable: false, comment: "Amount of resource consumed"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", maxLength: 1000, nullable: true, comment: "Additional metadata in JSON format"),
                    PeakUsage = table.Column<long>(type: "bigint", nullable: true, comment: "Peak usage during period"),
                    PeakUsageDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When peak usage occurred"),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the usage period ended"),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the usage period started"),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceQuotaId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Associated resource quota"),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Tenant that used the resource"),
                    Type = table.Column<int>(type: "integer", nullable: false, comment: "Type of resource used"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UsageAmount = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_records", x => x.Id);
                    table.CheckConstraint("CK_UsageRecord_Count_NonNegative", "\"Count\" >= 0");
                    table.CheckConstraint("CK_UsageRecord_PeakUsage_NonNegative", "\"PeakUsage\" IS NULL OR \"PeakUsage\" >= 0");
                    table.CheckConstraint("CK_UsageRecord_PeriodOrder", "\"PeriodEnd\" >= \"PeriodStart\"");
                });

            migrationBuilder.CreateTable(
                name: "usageretentionpolicy",
                schema: "gameguild.resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArchiveAfterDays = table.Column<int>(type: "integer", nullable: false),
                    CompactionIntervalDays = table.Column<int>(type: "integer", nullable: false),
                    Configuration = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DownSamplingStrategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EnableCompaction = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NextExecutionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResourceType = table.Column<int>(type: "integer", nullable: true),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usageretentionpolicy", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "slaimpactanalysis",
                schema: "gameguild.resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceQuotaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualValue = table.Column<long>(type: "bigint", nullable: false),
                    BusinessImpact = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeviationPercentage = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    ExpectedValue = table.Column<long>(type: "bigint", nullable: false),
                    IncidentCreated = table.Column<bool>(type: "boolean", nullable: false),
                    IncidentTicketId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MitigationActions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequiresEscalation = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RootCause = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ViolationEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ViolationStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ViolationType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_slaimpactanalysis", x => x.id);
                    table.ForeignKey(
                        name: "FK_slaimpactanalysis_resource_quotas_ResourceQuotaId",
                        column: x => x.ResourceQuotaId,
                        principalSchema: "resources",
                        principalTable: "resource_quotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    BillingCycle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BillingCycleCount = table.Column<int>(type: "integer", nullable: false),
                    CancellationNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExternalCustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastPaymentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NextBillingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrialEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "financial_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RevenueEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreditAccount = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DebitAccount = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EntryType = table.Column<int>(type: "integer", nullable: false),
                    FiscalPeriod = table.Column<int>(type: "integer", nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    IsReconciled = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReconciledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReconciledBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_ledger_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "revenue_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LedgerEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReferenceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revenue_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_revenue_events_financial_ledger_entries_LedgerEntryId",
                        column: x => x.LedgerEntryId,
                        principalTable: "financial_ledger_entries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsageTracking_TenantId1",
                table: "UsageTracking",
                column: "TenantId1");

            migrationBuilder.CreateIndex(
                name: "IX_TenantStatistics_TenantId1",
                table: "TenantStatistics",
                column: "TenantId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId1",
                table: "TenantSettings",
                column: "TenantId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMembers_TenantId1",
                table: "TenantMembers",
                column: "TenantId1");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_TenantId1",
                table: "TenantDomains",
                column: "TenantId1");

            migrationBuilder.CreateIndex(
                name: "IX_audit_trails_Action",
                table: "audit_trails",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_audit_trails_ChangedAt",
                table: "audit_trails",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_audit_trails_ChangedBy",
                table: "audit_trails",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_audit_trails_EntityId",
                table: "audit_trails",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_trails_EntityType",
                table: "audit_trails",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_CreatedAt",
                table: "financial_ledger_entries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_CreditAccount",
                table: "financial_ledger_entries",
                column: "CreditAccount");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_DebitAccount",
                table: "financial_ledger_entries",
                column: "DebitAccount");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_EntryType",
                table: "financial_ledger_entries",
                column: "EntryType");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_FiscalPeriod",
                table: "financial_ledger_entries",
                column: "FiscalPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_FiscalYear",
                table: "financial_ledger_entries",
                column: "FiscalYear");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_IsReconciled",
                table: "financial_ledger_entries",
                column: "IsReconciled");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_ReferenceNumber",
                table: "financial_ledger_entries",
                column: "ReferenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_RevenueEventId",
                table: "financial_ledger_entries",
                column: "RevenueEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceQuotas_ResourceType",
                schema: "resources",
                table: "resource_quotas",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceQuotas_TenantId_ResourceType",
                schema: "resources",
                table: "resource_quotas",
                columns: new[] { "TenantId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_revenue_events_EventType",
                table: "revenue_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_revenue_events_LedgerEntryId",
                table: "revenue_events",
                column: "LedgerEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_revenue_events_ReferenceId",
                table: "revenue_events",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_revenue_events_Source",
                table: "revenue_events",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_revenue_events_Status",
                table: "revenue_events",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_revenue_events_Timestamp",
                table: "revenue_events",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_revenue_events_UserId",
                table: "revenue_events",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_slaimpactanalysis_ResourceQuotaId",
                schema: "gameguild.resources",
                table: "slaimpactanalysis",
                column: "ResourceQuotaId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_ExternalId",
                table: "SubscriptionPlans",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CancelledAt",
                table: "Subscriptions",
                column: "CancelledAt");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ExternalCustomerId",
                table: "Subscriptions",
                column: "ExternalCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ExternalId",
                table: "Subscriptions",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_LastPaymentAt",
                table: "Subscriptions",
                column: "LastPaymentAt");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_NextBillingDate",
                table: "Subscriptions",
                column: "NextBillingDate");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Status",
                table: "Subscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_SubscriptionPlanId",
                table: "Subscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId",
                table: "Subscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_Status",
                table: "Subscriptions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TrialEndDate",
                table: "Subscriptions",
                column: "TrialEndDate");

            migrationBuilder.CreateIndex(
                name: "IX_UsageRecords_PeriodStart",
                schema: "resources",
                table: "usage_records",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_UsageRecords_Tenant_Resource_Time",
                schema: "resources",
                table: "usage_records",
                columns: new[] { "TenantId", "Type", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageRecords_UsagePeriod",
                schema: "resources",
                table: "usage_records",
                columns: new[] { "PeriodStart", "PeriodEnd" });

            migrationBuilder.AddForeignKey(
                name: "FK_TenantDomains_Tenants_TenantId1",
                table: "TenantDomains",
                column: "TenantId1",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TenantMembers_Tenants_TenantId1",
                table: "TenantMembers",
                column: "TenantId1",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TenantSettings_Tenants_TenantId1",
                table: "TenantSettings",
                column: "TenantId1",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TenantStatistics_Tenants_TenantId1",
                table: "TenantStatistics",
                column: "TenantId1",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UsageTracking_Tenants_TenantId1",
                table: "UsageTracking",
                column: "TenantId1",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_financial_ledger_entries_revenue_events_RevenueEventId",
                table: "financial_ledger_entries",
                column: "RevenueEventId",
                principalTable: "revenue_events",
                principalColumn: "Id");
        }
    }
}
