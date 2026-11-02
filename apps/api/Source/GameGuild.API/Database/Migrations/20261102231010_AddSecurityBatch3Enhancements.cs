using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityBatch3Enhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_testing_events_TenantId_Status_StartsAt",
                table: "testing_events");

            migrationBuilder.DropIndex(
                name: "IX_TenantMembers_TenantId_IsActive",
                table: "TenantMembers");

            migrationBuilder.DropIndex(
                name: "IX_projects_CategoryId_Status",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "IX_projects_TenantId_Status",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "idx_feature_flags_key_environment",
                table: "feature_flags");

            migrationBuilder.DropIndex(
                name: "IX_content_interactions_ContentId",
                table: "content_interactions");

            migrationBuilder.DropIndex(
                name: "IX_content_interactions_UserId",
                table: "content_interactions");

            migrationBuilder.DropIndex(
                name: "IX_Assessments_CourseId",
                table: "Assessments");

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_testing_events_TenantId_Status_StartsAt",
                table: "testing_events",
                columns: new[] { "TenantId", "Status", "StartsAt" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_members_user_id_active",
                table: "TenantMembers",
                column: "UserId",
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMembers_TenantId_IsActive",
                table: "TenantMembers",
                columns: new[] { "TenantId", "IsActive" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_projects_CategoryId_Status",
                table: "projects",
                columns: new[] { "CategoryId", "Status" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_projects_TenantId_Status",
                table: "projects",
                columns: new[] { "TenantId", "Status" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flags_key_environment",
                table: "feature_flags",
                columns: new[] { "key", "environment" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_ContentId",
                table: "content_interactions",
                column: "ContentId",
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_UserId",
                table: "content_interactions",
                column: "UserId",
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_CourseId",
                table: "Assessments",
                column: "CourseId",
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_api_keys_Users_user_id",
                schema: "gameguild.authentication",
                table: "api_keys",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_blockchaincertificateanchor_Users_UserId",
                schema: "gameguild.authentication",
                table: "blockchaincertificateanchor",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_externallogin_Users_UserId",
                schema: "gameguild.authentication",
                table: "externallogin",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_identityverification_Users_UserId",
                schema: "gameguild.authentication",
                table: "identityverification",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_mfaattempt_Users_UserId",
                schema: "gameguild.authentication",
                table: "mfaattempt",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_refreshtoken_Users_UserId",
                schema: "gameguild.authentication",
                table: "refreshtoken",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_trusteddevice_Users_UserId",
                schema: "gameguild.authentication",
                table: "trusteddevice",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_user_mfa_configuration_Users_user_id",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_user_role_Users_user_id",
                schema: "gameguild.authentication",
                table: "user_role",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_usersession_Users_UserId",
                schema: "gameguild.authentication",
                table: "usersession",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserWebAuthnCredentials_Users_UserId",
                schema: "auth",
                table: "UserWebAuthnCredentials",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_api_keys_Users_user_id",
                schema: "gameguild.authentication",
                table: "api_keys");

            migrationBuilder.DropForeignKey(
                name: "FK_blockchaincertificateanchor_Users_UserId",
                schema: "gameguild.authentication",
                table: "blockchaincertificateanchor");

            migrationBuilder.DropForeignKey(
                name: "FK_externallogin_Users_UserId",
                schema: "gameguild.authentication",
                table: "externallogin");

            migrationBuilder.DropForeignKey(
                name: "FK_identityverification_Users_UserId",
                schema: "gameguild.authentication",
                table: "identityverification");

            migrationBuilder.DropForeignKey(
                name: "FK_mfaattempt_Users_UserId",
                schema: "gameguild.authentication",
                table: "mfaattempt");

            migrationBuilder.DropForeignKey(
                name: "FK_refreshtoken_Users_UserId",
                schema: "gameguild.authentication",
                table: "refreshtoken");

            migrationBuilder.DropForeignKey(
                name: "FK_trusteddevice_Users_UserId",
                schema: "gameguild.authentication",
                table: "trusteddevice");

            migrationBuilder.DropForeignKey(
                name: "FK_user_mfa_configuration_Users_user_id",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration");

            migrationBuilder.DropForeignKey(
                name: "FK_user_role_Users_user_id",
                schema: "gameguild.authentication",
                table: "user_role");

            migrationBuilder.DropForeignKey(
                name: "FK_usersession_Users_UserId",
                schema: "gameguild.authentication",
                table: "usersession");

            migrationBuilder.DropForeignKey(
                name: "FK_UserWebAuthnCredentials_Users_UserId",
                schema: "auth",
                table: "UserWebAuthnCredentials");

            migrationBuilder.DropIndex(
                name: "IX_testing_events_TenantId_Status_StartsAt",
                table: "testing_events");

            migrationBuilder.DropIndex(
                name: "ix_tenant_members_user_id_active",
                table: "TenantMembers");

            migrationBuilder.DropIndex(
                name: "IX_TenantMembers_TenantId_IsActive",
                table: "TenantMembers");

            migrationBuilder.DropIndex(
                name: "IX_projects_CategoryId_Status",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "IX_projects_TenantId_Status",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "idx_feature_flags_key_environment",
                table: "feature_flags");

            migrationBuilder.DropIndex(
                name: "IX_content_interactions_ContentId",
                table: "content_interactions");

            migrationBuilder.DropIndex(
                name: "IX_content_interactions_UserId",
                table: "content_interactions");

            migrationBuilder.DropIndex(
                name: "IX_Assessments_CourseId",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_testing_events_TenantId_Status_StartsAt",
                table: "testing_events",
                columns: new[] { "TenantId", "Status", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantMembers_TenantId_IsActive",
                table: "TenantMembers",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_projects_CategoryId_Status",
                table: "projects",
                columns: new[] { "CategoryId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_projects_TenantId_Status",
                table: "projects",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "idx_feature_flags_key_environment",
                table: "feature_flags",
                columns: new[] { "key", "environment" });

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_ContentId",
                table: "content_interactions",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_UserId",
                table: "content_interactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_CourseId",
                table: "Assessments",
                column: "CourseId");
        }
    }
}
