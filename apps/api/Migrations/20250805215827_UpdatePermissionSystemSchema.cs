using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePermissionSystemSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModuleRoles_Tenants_TenantId",
                table: "ModuleRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleAssignments_ModuleRoles_RoleId",
                table: "UserRoleAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleAssignments_Tenants_TenantId",
                table: "UserRoleAssignments");

            migrationBuilder.DropTable(
                name: "ProductPermissions");

            migrationBuilder.DropTable(
                name: "ProgramPermissions");

            migrationBuilder.DropTable(
                name: "SessionRegistrationPermissions");

            migrationBuilder.DropTable(
                name: "TestingFeedbackPermissions");

            migrationBuilder.DropTable(
                name: "TestingRequestPermissions");

            migrationBuilder.DropTable(
                name: "TestingSessionPermissions");

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
                name: "IX_ModuleRoles_CreatedAt",
                table: "ModuleRoles");

            migrationBuilder.DropIndex(
                name: "IX_ModuleRoles_DeletedAt",
                table: "ModuleRoles");

            migrationBuilder.DropIndex(
                name: "IX_ModuleRoles_TenantId",
                table: "ModuleRoles");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "UserRoleAssignments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ModuleRoles");

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

            migrationBuilder.CreateTable(
                name: "RoleTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PermissionTemplatesJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleTemplates_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrantedByRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ConstraintsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RoleTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermissions_RoleTemplates_RoleTemplateId",
                        column: x => x.RoleTemplateId,
                        principalTable: "RoleTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserPermissions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_UserId_Module_RoleName",
                table: "UserRoleAssignments",
                columns: new[] { "UserId", "Module", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoles_Name_Module_TenantId",
                table: "ModuleRoles",
                columns: new[] { "Name", "Module", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplates_CreatedAt",
                table: "RoleTemplates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplates_DeletedAt",
                table: "RoleTemplates",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplates_TenantId",
                table: "RoleTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_CreatedAt",
                table: "UserPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_DeletedAt",
                table: "UserPermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_RoleTemplateId",
                table: "UserPermissions",
                column: "RoleTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_TenantId",
                table: "UserPermissions",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleAssignments_ModuleRoles_RoleId",
                table: "UserRoleAssignments",
                column: "RoleId",
                principalTable: "ModuleRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleAssignments_ModuleRoles_RoleId",
                table: "UserRoleAssignments");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "RoleTemplates");

            migrationBuilder.DropIndex(
                name: "IX_UserRoleAssignments_UserId_Module_RoleName",
                table: "UserRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ModuleRoles_Name_Module_TenantId",
                table: "ModuleRoles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserRoleAssignments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserRoleAssignments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ModuleRoles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ModuleRoles");

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

            migrationBuilder.CreateTable(
                name: "ProductPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
                    PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPermissions_Products_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductPermissions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProgramPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
                    PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramPermissions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProgramPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProgramPermissions_programs_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionRegistrationPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
                    PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRegistrationPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionRegistrationPermissions_SessionRegistrations_Resourc~",
                        column: x => x.ResourceId,
                        principalTable: "SessionRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionRegistrationPermissions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SessionRegistrationPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TestingFeedbackPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
                    PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingFeedbackPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestingFeedbackPermissions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestingFeedbackPermissions_TestingFeedback_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "TestingFeedback",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestingFeedbackPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TestingRequestPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
                    PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingRequestPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestingRequestPermissions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestingRequestPermissions_TestingRequests_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "TestingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestingRequestPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TestingSessionPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
                    PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingSessionPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestingSessionPermissions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestingSessionPermissions_TestingSessions_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "TestingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestingSessionPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

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
                name: "IX_ProductPermissions_CreatedAt",
                table: "ProductPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPermissions_DeletedAt",
                table: "ProductPermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPermissions_Expiration",
                table: "ProductPermissions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPermissions_Resource_User",
                table: "ProductPermissions",
                columns: new[] { "ResourceId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPermissions_TenantId",
                table: "ProductPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPermissions_User_Tenant_Resource",
                table: "ProductPermissions",
                columns: new[] { "UserId", "TenantId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramPermissions_CreatedAt",
                table: "ProgramPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramPermissions_DeletedAt",
                table: "ProgramPermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramPermissions_Expiration",
                table: "ProgramPermissions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramPermissions_Resource_User",
                table: "ProgramPermissions",
                columns: new[] { "ResourceId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramPermissions_TenantId",
                table: "ProgramPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramPermissions_User_Tenant_Resource",
                table: "ProgramPermissions",
                columns: new[] { "UserId", "TenantId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionRegistrationPermissions_CreatedAt",
                table: "SessionRegistrationPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRegistrationPermissions_DeletedAt",
                table: "SessionRegistrationPermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRegistrationPermissions_ResourceId",
                table: "SessionRegistrationPermissions",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRegistrationPermissions_TenantId",
                table: "SessionRegistrationPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRegistrationPermissions_User_Tenant_Resource",
                table: "SessionRegistrationPermissions",
                columns: new[] { "UserId", "TenantId", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedbackPermissions_CreatedAt",
                table: "TestingFeedbackPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedbackPermissions_DeletedAt",
                table: "TestingFeedbackPermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedbackPermissions_ResourceId",
                table: "TestingFeedbackPermissions",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedbackPermissions_TenantId",
                table: "TestingFeedbackPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedbackPermissions_User_Tenant_Resource",
                table: "TestingFeedbackPermissions",
                columns: new[] { "UserId", "TenantId", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequestPermissions_CreatedAt",
                table: "TestingRequestPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequestPermissions_DeletedAt",
                table: "TestingRequestPermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequestPermissions_ResourceId",
                table: "TestingRequestPermissions",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequestPermissions_TenantId",
                table: "TestingRequestPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequestPermissions_UserId",
                table: "TestingRequestPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessionPermissions_CreatedAt",
                table: "TestingSessionPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessionPermissions_DeletedAt",
                table: "TestingSessionPermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessionPermissions_Expiration",
                table: "TestingSessionPermissions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessionPermissions_Resource_User",
                table: "TestingSessionPermissions",
                columns: new[] { "ResourceId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessionPermissions_TenantId",
                table: "TestingSessionPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessionPermissions_User_Tenant_Resource",
                table: "TestingSessionPermissions",
                columns: new[] { "UserId", "TenantId", "ResourceId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ModuleRoles_Tenants_TenantId",
                table: "ModuleRoles",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleAssignments_ModuleRoles_RoleId",
                table: "UserRoleAssignments",
                column: "RoleId",
                principalTable: "ModuleRoles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleAssignments_Tenants_TenantId",
                table: "UserRoleAssignments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");
        }
    }
}
