using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConnectAuthenticationAndAuthorizationModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

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
                name: "PK_Roles",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MfaAttempts",
                table: "MfaAttempts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuthUsers",
                table: "AuthUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuthenticationAttempts",
                table: "AuthenticationAttempts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AbacPolicies",
                table: "AbacPolicies");

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

            migrationBuilder.RenameIndex(
                name: "IX_AbacPolicies_TenantId",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                newName: "IX_abacpolicy_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_AbacPolicies_Priority",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                newName: "IX_abacpolicy_Priority");

            migrationBuilder.RenameIndex(
                name: "IX_AbacPolicies_IsEnabled",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                newName: "IX_abacpolicy_IsEnabled");

            migrationBuilder.AlterColumn<string>(
                name: "preferred_method",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            // Use raw SQL with USING clause for jsonb cast
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

            migrationBuilder.AlterColumn<string>(
                name: "TargetResources",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TargetActions",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SubjectConditions",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResourceConditions",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EnvironmentConditions",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActionConditions",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttributeExpression",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConditionExpression",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveUntil",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationConditions",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Obligations",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceType",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeConditions",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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
                name: "accessreviewcampaign",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    FilterCriteria = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    CertificateType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CertificateHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CertificateData = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    TransactionHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BlockchainNetwork = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BlockNumber = table.Column<long>(type: "bigint", nullable: true),
                    AnchoredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RevocationTransactionHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blockchaincertificateanchor", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "conditionalpolicy",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConditionType = table.Column<int>(type: "integer", nullable: false),
                    PermissionType = table.Column<int>(type: "integer", nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TimeConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EnvironmentConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LocationConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DeviceConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CustomConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EnforcementMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conditionalpolicy", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contenttypepermission",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentTypeName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    VerificationType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VerifiedValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    InitiatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationProvider = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExternalVerificationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DocumentIds = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identityverification", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceInvitation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Permissions = table.Column<string[]>(type: "text[]", maxLength: 4000, nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeclinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeclineReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceInvitation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceUserPermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Permissions = table.Column<string[]>(type: "text[]", maxLength: 4000, nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceUserPermission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenantpermission",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_tenantpermission", x => x.id);
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
                    ResourceType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Permissions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Decision = table.Column<int>(type: "integer", nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastReminderSent = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemindersSent = table.Column<int>(type: "integer", nullable: false),
                    ContextInfo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                name: "IX_accessreviewitem_CampaignId",
                schema: "gameguild.authentication",
                table: "accessreviewitem",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceInvitation_ExpiresAt",
                table: "ResourceInvitation",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceInvitation_Status",
                table: "ResourceInvitation",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceInvitation_TenantId_Email",
                table: "ResourceInvitation",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceInvitation_TenantId_ResourceType_ResourceId",
                table: "ResourceInvitation",
                columns: new[] { "TenantId", "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceUserPermission_ExpiresAt",
                table: "ResourceUserPermission",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceUserPermission_TenantId_ResourceType_ResourceId",
                table: "ResourceUserPermission",
                columns: new[] { "TenantId", "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceUserPermission_TenantId_UserId",
                table: "ResourceUserPermission",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceUserPermission_TenantId_UserId_ResourceType_Resourc~",
                table: "ResourceUserPermission",
                columns: new[] { "TenantId", "UserId", "ResourceType", "ResourceId" });

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
                name: "accessreviewitem",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "blockchaincertificateanchor",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "conditionalpolicy",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "contenttypepermission",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "identityverification",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "ResourceInvitation");

            migrationBuilder.DropTable(
                name: "ResourceUserPermission");

            migrationBuilder.DropTable(
                name: "tenantpermission",
                schema: "gameguild.authentication");

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

            migrationBuilder.DropColumn(
                name: "AttributeExpression",
                schema: "gameguild.authentication",
                table: "abacpolicy");

            migrationBuilder.DropColumn(
                name: "ConditionExpression",
                schema: "gameguild.authentication",
                table: "abacpolicy");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                schema: "gameguild.authentication",
                table: "abacpolicy");

            migrationBuilder.DropColumn(
                name: "EffectiveUntil",
                schema: "gameguild.authentication",
                table: "abacpolicy");

            migrationBuilder.DropColumn(
                name: "LocationConditions",
                schema: "gameguild.authentication",
                table: "abacpolicy");

            migrationBuilder.DropColumn(
                name: "Obligations",
                schema: "gameguild.authentication",
                table: "abacpolicy");

            migrationBuilder.DropColumn(
                name: "ResourceType",
                schema: "gameguild.authentication",
                table: "abacpolicy");

            migrationBuilder.DropColumn(
                name: "Tags",
                schema: "gameguild.authentication",
                table: "abacpolicy");

            migrationBuilder.DropColumn(
                name: "TimeConditions",
                schema: "gameguild.authentication",
                table: "abacpolicy");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "gameguild.authentication",
                table: "abacpolicy");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "gameguild.authentication",
                table: "abacpolicy");

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

            migrationBuilder.RenameIndex(
                name: "IX_abacpolicy_TenantId",
                table: "AbacPolicies",
                newName: "IX_AbacPolicies_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_abacpolicy_Priority",
                table: "AbacPolicies",
                newName: "IX_AbacPolicies_Priority");

            migrationBuilder.RenameIndex(
                name: "IX_abacpolicy_IsEnabled",
                table: "AbacPolicies",
                newName: "IX_AbacPolicies_IsEnabled");

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
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldMaxLength: 4000);

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

            migrationBuilder.AlterColumn<string>(
                name: "TargetResources",
                table: "AbacPolicies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TargetActions",
                table: "AbacPolicies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SubjectConditions",
                table: "AbacPolicies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResourceConditions",
                table: "AbacPolicies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EnvironmentConditions",
                table: "AbacPolicies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActionConditions",
                table: "AbacPolicies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
