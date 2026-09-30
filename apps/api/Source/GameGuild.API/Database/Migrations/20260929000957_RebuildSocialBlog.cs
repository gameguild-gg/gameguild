using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class RebuildSocialBlog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The legacy social_blog_posts table is an unused skeleton (no UI ever shipped);
            // drop-and-recreate is sanctioned by plan decision #11. Reactions are polymorphic
            // (no FK), so orphaned BlogPost rows are purged in the same migration.
            migrationBuilder.Sql("DELETE FROM \"social_reactions\" WHERE \"TargetType\" = 'BlogPost'");

            migrationBuilder.DropTable(
                name: "social_blog_posts");

            migrationBuilder.CreateTable(
                name: "social_blog_posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrimaryAuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Excerpt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    JsonBody = table.Column<string>(type: "jsonb", nullable: true),
                    Format = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Tags = table.Column<string>(type: "jsonb", nullable: false),
                    MetaTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MetaDescription = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    OgImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CanonicalUrlOverride = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TwitterCard = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StructuredDataOverride = table.Column<string>(type: "text", nullable: true),
                    AllowComments = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ViewsCount = table.Column<int>(type: "integer", nullable: false),
                    CommentsCount = table.Column<int>(type: "integer", nullable: false),
                    ReadTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_blog_posts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "social_blog_ai_conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_blog_ai_conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "social_blog_ai_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: true),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_blog_ai_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "social_blog_ai_proposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    BasePostRevision = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OriginalContent = table.Column<string>(type: "text", nullable: false),
                    ProposedContent = table.Column<string>(type: "text", nullable: false),
                    ProposedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_blog_ai_proposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "social_blog_ai_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    BasePostRevision = table.Column<int>(type: "integer", nullable: false),
                    ProposalKind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Instruction = table.Column<string>(type: "text", nullable: false),
                    Selection = table.Column<string>(type: "text", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Model = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MaximumInputTokens = table.Column<int>(type: "integer", nullable: false),
                    MaximumOutputTokens = table.Column<int>(type: "integer", nullable: false),
                    MaximumEstimatedCost = table.Column<long>(type: "bigint", nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    SettledCost = table.Column<long>(type: "bigint", nullable: false),
                    ReleasedAmount = table.Column<long>(type: "bigint", nullable: false),
                    ResponseText = table.Column<string>(type: "text", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_blog_ai_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "social_blog_ai_stream_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Delta = table.Column<string>(type: "text", nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_blog_ai_stream_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "social_blog_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_blog_comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "social_blog_post_authors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_blog_post_authors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "social_blog_slug_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlogPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousPrimaryAuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousSlug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_blog_slug_history", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_posts_PrimaryAuthorId_Slug",
                table: "social_blog_posts",
                columns: new[] { "PrimaryAuthorId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_posts_PrimaryAuthorId_Status_PublishedAt",
                table: "social_blog_posts",
                columns: new[] { "PrimaryAuthorId", "Status", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_posts_Status_PublishedAt",
                table: "social_blog_posts",
                columns: new[] { "Status", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_ai_conversations_AuthorId_LastMessageAt",
                table: "social_blog_ai_conversations",
                columns: new[] { "AuthorId", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_ai_conversations_TenantId_BlogPostId_AuthorId",
                table: "social_blog_ai_conversations",
                columns: new[] { "TenantId", "BlogPostId", "AuthorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_ai_messages_ConversationId_CreatedAt",
                table: "social_blog_ai_messages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_ai_messages_RunId",
                table: "social_blog_ai_messages",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_ai_proposals_BlogPostId_Status",
                table: "social_blog_ai_proposals",
                columns: new[] { "BlogPostId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_ai_proposals_RunId",
                table: "social_blog_ai_proposals",
                column: "RunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_ai_runs_BlogPostId_ActorId_CreatedAt",
                table: "social_blog_ai_runs",
                columns: new[] { "BlogPostId", "ActorId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_ai_runs_Status",
                table: "social_blog_ai_runs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_ai_runs_TenantId_ActorId_IdempotencyKey",
                table: "social_blog_ai_runs",
                columns: new[] { "TenantId", "ActorId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_ai_stream_events_RunId_Sequence",
                table: "social_blog_ai_stream_events",
                columns: new[] { "RunId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_comments_BlogPostId_CreatedAt",
                table: "social_blog_comments",
                columns: new[] { "BlogPostId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_comments_ParentCommentId",
                table: "social_blog_comments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_post_authors_BlogPostId_UserId",
                table: "social_blog_post_authors",
                columns: new[] { "BlogPostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_post_authors_UserId",
                table: "social_blog_post_authors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_slug_history_BlogPostId",
                table: "social_blog_slug_history",
                column: "BlogPostId");

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_slug_history_PreviousPrimaryAuthorId_PreviousSlug",
                table: "social_blog_slug_history",
                columns: new[] { "PreviousPrimaryAuthorId", "PreviousSlug" },
                unique: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "social_blog_ai_conversations");

            migrationBuilder.DropTable(
                name: "social_blog_ai_messages");

            migrationBuilder.DropTable(
                name: "social_blog_ai_proposals");

            migrationBuilder.DropTable(
                name: "social_blog_ai_runs");

            migrationBuilder.DropTable(
                name: "social_blog_ai_stream_events");

            migrationBuilder.DropTable(
                name: "social_blog_comments");

            migrationBuilder.DropTable(
                name: "social_blog_post_authors");

            migrationBuilder.DropTable(
                name: "social_blog_slug_history");

            migrationBuilder.DropTable(
                name: "social_blog_posts");

            migrationBuilder.CreateTable(
                name: "social_blog_posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Excerpt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CoverImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    AllowComments = table.Column<bool>(type: "boolean", nullable: false),
                    ViewsCount = table.Column<int>(type: "integer", nullable: false),
                    LikesCount = table.Column<int>(type: "integer", nullable: false),
                    CommentsCount = table.Column<int>(type: "integer", nullable: false),
                    ReadTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_blog_posts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_posts_AuthorId",
                table: "social_blog_posts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_posts_IsFeatured",
                table: "social_blog_posts",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_posts_Slug",
                table: "social_blog_posts",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_blog_posts_Status",
                table: "social_blog_posts",
                column: "Status");
        }
    }
}
