using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicRolesWithDenyPermissions : Migration
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
                name: "PK_IdentityVerifications",
                table: "IdentityVerifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContentTypePermissions",
                table: "ContentTypePermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BlockchainCertificateAnchors",
                table: "BlockchainCertificateAnchors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuthenticationAttempts",
                table: "AuthenticationAttempts");

            migrationBuilder.EnsureSchema(
                name: "gameguild.authentication");

            migrationBuilder.EnsureSchema(
                name: "auth");

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
                name: "IdentityVerifications",
                newName: "identityverification",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "ContentTypePermissions",
                newName: "contenttypepermission",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "BlockchainCertificateAnchors",
                newName: "blockchaincertificateanchor",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameTable(
                name: "AuthenticationAttempts",
                newName: "authenticationattempt",
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
                table: "identityverification",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "contenttypepermission",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "blockchaincertificateanchor",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "authenticationattempt",
                newName: "id");

            migrationBuilder.AddColumn<int>(
                name: "TokenVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string[]>(
                name: "DenyPermissions",
                table: "TenantPermissions",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<bool>(
                name: "IsOwner",
                table: "ResourceUserPermission",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "preferred_method",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "permissions",
                schema: "gameguild.authentication",
                table: "role",
                type: "jsonb",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

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
                name: "PK_identityverification",
                schema: "gameguild.authentication",
                table: "identityverification",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_contenttypepermission",
                schema: "gameguild.authentication",
                table: "contenttypepermission",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_blockchaincertificateanchor",
                schema: "gameguild.authentication",
                table: "blockchaincertificateanchor",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_authenticationattempt",
                schema: "gameguild.authentication",
                table: "authenticationattempt",
                column: "id");

            migrationBuilder.CreateTable(
                name: "DynamicRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentRoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Permissions = table.Column<string[]>(type: "text[]", nullable: false),
                    DenyPermissions = table.Column<string[]>(type: "text[]", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    MutuallyExclusiveRoleIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    PrerequisiteRoleIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    MaxAssignments = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DynamicRole_DynamicRole_ParentRoleId",
                        column: x => x.ParentRoleId,
                        principalTable: "DynamicRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermissionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Permissions = table.Column<string[]>(type: "text[]", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MinimumTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "service_accounts",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    client_secret_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scopes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    secret_rotated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    secret_rotation_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_authenticated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_authenticated_from_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    authentication_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    failed_authentication_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    allowed_ip_addresses = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "UserWebAuthnCredentials",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    PublicKey = table.Column<string>(type: "text", nullable: false),
                    AaGuid = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    SignatureCounter = table.Column<long>(type: "bigint", nullable: false),
                    FriendlyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CredentialType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "public-key"),
                    AuthenticatorType = table.Column<int>(type: "integer", nullable: false),
                    Transports = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsPasswordless = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    UserVerified = table.Column<bool>(type: "boolean", nullable: false),
                    BackedUp = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RegisteredFromIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    RegisteredUserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWebAuthnCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DynamicRoleAssignment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicRoleAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DynamicRoleAssignment_DynamicRole_RoleId",
                        column: x => x.RoleId,
                        principalTable: "DynamicRole",
                        principalColumn: "Id",
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
                name: "IX_DynamicRole_IsActive",
                table: "DynamicRole",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRole_Name",
                table: "DynamicRole",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRole_ParentRoleId",
                table: "DynamicRole",
                column: "ParentRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRole_TenantId",
                table: "DynamicRole",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRole_TenantId_Name",
                table: "DynamicRole",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRoleAssignment_IsActive",
                table: "DynamicRoleAssignment",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRoleAssignment_RoleId",
                table: "DynamicRoleAssignment",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRoleAssignment_StartsAt_ExpiresAt",
                table: "DynamicRoleAssignment",
                columns: new[] { "StartsAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRoleAssignment_TenantId",
                table: "DynamicRoleAssignment",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRoleAssignment_UserId",
                table: "DynamicRoleAssignment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRoleAssignment_UserId_RoleId_TenantId",
                table: "DynamicRoleAssignment",
                columns: new[] { "UserId", "RoleId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplates_IsSystemTemplate",
                table: "PermissionTemplates",
                column: "IsSystemTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplates_Name",
                table: "PermissionTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_service_accounts_client_id",
                schema: "gameguild.authentication",
                table: "service_accounts",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_service_accounts_tenant_active",
                schema: "gameguild.authentication",
                table: "service_accounts",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "idx_service_accounts_tenant_id",
                schema: "gameguild.authentication",
                table: "service_accounts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_UserWebAuthnCredentials_CredentialId",
                schema: "auth",
                table: "UserWebAuthnCredentials",
                column: "CredentialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserWebAuthnCredentials_UserId",
                schema: "auth",
                table: "UserWebAuthnCredentials",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWebAuthnCredentials_UserId_IsActive",
                schema: "auth",
                table: "UserWebAuthnCredentials",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_TenantMembers_Users_UserId",
                table: "TenantMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_TenantMembers_Users_UserId",
                table: "TenantMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_user_role_role_role_id",
                schema: "gameguild.authentication",
                table: "user_role");

            migrationBuilder.DropTable(
                name: "DynamicRoleAssignment");

            migrationBuilder.DropTable(
                name: "PermissionTemplates");

            migrationBuilder.DropTable(
                name: "service_accounts",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "UserWebAuthnCredentials",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "DynamicRole");

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
                name: "PK_identityverification",
                schema: "gameguild.authentication",
                table: "identityverification");

            migrationBuilder.DropPrimaryKey(
                name: "PK_contenttypepermission",
                schema: "gameguild.authentication",
                table: "contenttypepermission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_blockchaincertificateanchor",
                schema: "gameguild.authentication",
                table: "blockchaincertificateanchor");

            migrationBuilder.DropPrimaryKey(
                name: "PK_authenticationattempt",
                schema: "gameguild.authentication",
                table: "authenticationattempt");

            migrationBuilder.DropColumn(
                name: "TokenVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DenyPermissions",
                table: "TenantPermissions");

            migrationBuilder.DropColumn(
                name: "IsOwner",
                table: "ResourceUserPermission");

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
                name: "identityverification",
                schema: "gameguild.authentication",
                newName: "IdentityVerifications");

            migrationBuilder.RenameTable(
                name: "contenttypepermission",
                schema: "gameguild.authentication",
                newName: "ContentTypePermissions");

            migrationBuilder.RenameTable(
                name: "blockchaincertificateanchor",
                schema: "gameguild.authentication",
                newName: "BlockchainCertificateAnchors");

            migrationBuilder.RenameTable(
                name: "authenticationattempt",
                schema: "gameguild.authentication",
                newName: "AuthenticationAttempts");

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
                table: "IdentityVerifications",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ContentTypePermissions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "BlockchainCertificateAnchors",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AuthenticationAttempts",
                newName: "Id");

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
                name: "PK_IdentityVerifications",
                table: "IdentityVerifications",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContentTypePermissions",
                table: "ContentTypePermissions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BlockchainCertificateAnchors",
                table: "BlockchainCertificateAnchors",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuthenticationAttempts",
                table: "AuthenticationAttempts",
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
