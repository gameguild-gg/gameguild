using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantDomainUserGroupSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantUserGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentGroupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUserGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantUserGroups_TenantUserGroups_ParentGroupId",
                        column: x => x.ParentGroupId,
                        principalTable: "TenantUserGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TenantUserGroups_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TopLevelDomain = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Subdomain = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsMainDomain = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSecondaryDomain = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserGroupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDomains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantDomains_TenantUserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "TenantUserGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TenantDomains_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantUserGroupMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsAutoAssigned = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUserGroupMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantUserGroupMemberships_TenantUserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "TenantUserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantUserGroupMemberships_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TenantUserGroupMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_CreatedAt",
                table: "TenantDomains",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_DeletedAt",
                table: "TenantDomains",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_TenantId_IsMainDomain",
                table: "TenantDomains",
                columns: new[] { "TenantId", "IsMainDomain" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_TopLevelDomain_Subdomain",
                table: "TenantDomains",
                columns: new[] { "TopLevelDomain", "Subdomain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_UserGroupId",
                table: "TenantDomains",
                column: "UserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroupMemberships_CreatedAt",
                table: "TenantUserGroupMemberships",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroupMemberships_DeletedAt",
                table: "TenantUserGroupMemberships",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroupMemberships_TenantId",
                table: "TenantUserGroupMemberships",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroupMemberships_UserGroupId",
                table: "TenantUserGroupMemberships",
                column: "UserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroupMemberships_UserId_UserGroupId",
                table: "TenantUserGroupMemberships",
                columns: new[] { "UserId", "UserGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroups_CreatedAt",
                table: "TenantUserGroups",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroups_DeletedAt",
                table: "TenantUserGroups",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroups_ParentGroupId",
                table: "TenantUserGroups",
                column: "ParentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroups_TenantId_Name",
                table: "TenantUserGroups",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantDomains");

            migrationBuilder.DropTable(
                name: "TenantUserGroupMemberships");

            migrationBuilder.DropTable(
                name: "TenantUserGroups");
        }
    }
}
