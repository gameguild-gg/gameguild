using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeletePartialIndexesHotTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_wallet_transactions_wallet_created_live",
                table: "wallet_transactions",
                columns: new[] { "WalletId", "CreatedAt" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_notifications_user_created_live",
                table: "UserNotifications",
                columns: new[] { "UserId", "CreatedAt" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_testing_sessions_tenant_session_live",
                table: "testing_sessions",
                columns: new[] { "TenantId", "SessionDate" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_testing_requests_status_created_live",
                table: "testing_requests",
                columns: new[] { "Status", "CreatedAt" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_testing_participants_request_tenant_live",
                table: "testing_participants",
                columns: new[] { "TestingRequestId", "TenantId" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_testing_events_tenant_starts_live",
                table: "testing_events",
                columns: new[] { "TenantId", "StartsAt" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_social_stories_author_expires_live",
                table: "social_stories",
                columns: new[] { "AuthorId", "ExpiresAt", "CreatedAt" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_social_saved_posts_user_created_live",
                table: "social_saved_posts",
                columns: new[] { "UserId", "CreatedAt", "Id" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_social_blog_posts_status_published_live",
                table: "social_blog_posts",
                columns: new[] { "Status", "PublishedAt", "Id" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_team_members_user_tenant_live",
                table: "project_collaboration_team_members",
                columns: new[] { "UserId", "TenantId" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_programs_status_visibility_live",
                table: "programs",
                columns: new[] { "Status", "Visibility" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_program_contents_program_sort_live",
                table: "program_contents",
                columns: new[] { "ProgramId", "SortOrder" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_posts_author_created_live",
                table: "posts",
                columns: new[] { "AuthorId", "CreatedAt" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_posts_tenant_created_live",
                table: "posts",
                columns: new[] { "TenantId", "CreatedAt" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_post_comments_post_created_live",
                table: "post_comments",
                columns: new[] { "PostId", "CreatedAt" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_tenant_live",
                table: "AuditLogs",
                columns: new[] { "CreatedAt", "TenantId" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_asset_references_parent_created_live",
                schema: "assets",
                table: "asset_references",
                columns: new[] { "ParentResourceType", "ParentResourceId", "CreatedAt" },
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_wallet_transactions_wallet_created_live",
                table: "wallet_transactions");

            migrationBuilder.DropIndex(
                name: "ix_user_notifications_user_created_live",
                table: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "ix_testing_sessions_tenant_session_live",
                table: "testing_sessions");

            migrationBuilder.DropIndex(
                name: "ix_testing_requests_status_created_live",
                table: "testing_requests");

            migrationBuilder.DropIndex(
                name: "ix_testing_participants_request_tenant_live",
                table: "testing_participants");

            migrationBuilder.DropIndex(
                name: "ix_testing_events_tenant_starts_live",
                table: "testing_events");

            migrationBuilder.DropIndex(
                name: "ix_social_stories_author_expires_live",
                table: "social_stories");

            migrationBuilder.DropIndex(
                name: "ix_social_saved_posts_user_created_live",
                table: "social_saved_posts");

            migrationBuilder.DropIndex(
                name: "ix_social_blog_posts_status_published_live",
                table: "social_blog_posts");

            migrationBuilder.DropIndex(
                name: "ix_team_members_user_tenant_live",
                table: "project_collaboration_team_members");

            migrationBuilder.DropIndex(
                name: "ix_programs_status_visibility_live",
                table: "programs");

            migrationBuilder.DropIndex(
                name: "ix_program_contents_program_sort_live",
                table: "program_contents");

            migrationBuilder.DropIndex(
                name: "ix_posts_author_created_live",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "ix_posts_tenant_created_live",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "ix_post_comments_post_created_live",
                table: "post_comments");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_created_tenant_live",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "ix_asset_references_parent_created_live",
                schema: "assets",
                table: "asset_references");
        }
    }
}
