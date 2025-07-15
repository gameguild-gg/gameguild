using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class AddPostsEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "post_tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFeatured = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_tags_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PostType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AuthorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsSystemGenerated = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPinned = table.Column<bool>(type: "INTEGER", nullable: false),
                    LikesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CommentsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SharesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RichContent = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_posts_ResourceMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ResourceMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_posts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_posts_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LikesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_comments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_post_comments_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_post_comments_post_comments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "post_comments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_post_comments_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_content_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReferencedResourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReferenceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Context = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_content_references", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_content_references_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_post_content_references_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_followers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NotifyOnComments = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotifyOnLikes = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotifyOnShares = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotifyOnUpdates = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_followers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_followers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_post_followers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_post_followers_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_likes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReactionType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_likes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_likes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_post_likes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_post_likes_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_statistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ViewsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UniqueViewersCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalSharesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageEngagementTime = table.Column<double>(type: "REAL", nullable: false),
                    EngagementScore = table.Column<double>(type: "REAL", nullable: false),
                    TrendingScore = table.Column<double>(type: "REAL", nullable: false),
                    LastCalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_statistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_statistics_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_post_statistics_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_tag_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_tag_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_tag_assignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_post_tag_assignments_post_tags_TagId",
                        column: x => x.TagId,
                        principalTable: "post_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_post_tag_assignments_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_views",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ViewedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Referrer = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEngaged = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_views", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_views_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_post_views_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_post_views_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_post_comments_AuthorId",
                table: "post_comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_post_comments_CreatedAt",
                table: "post_comments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_comments_DeletedAt",
                table: "post_comments",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_comments_ParentCommentId",
                table: "post_comments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_post_comments_PostId",
                table: "post_comments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_post_comments_TenantId",
                table: "post_comments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_post_content_references_CreatedAt",
                table: "post_content_references",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_content_references_DeletedAt",
                table: "post_content_references",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_content_references_PostId",
                table: "post_content_references",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_post_content_references_ReferencedResourceId",
                table: "post_content_references",
                column: "ReferencedResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_post_content_references_TenantId",
                table: "post_content_references",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_post_followers_CreatedAt",
                table: "post_followers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_followers_DeletedAt",
                table: "post_followers",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_followers_PostId",
                table: "post_followers",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_post_followers_TenantId",
                table: "post_followers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_post_followers_UserId",
                table: "post_followers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_post_likes_CreatedAt",
                table: "post_likes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_likes_DeletedAt",
                table: "post_likes",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_likes_PostId",
                table: "post_likes",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_post_likes_PostId_UserId",
                table: "post_likes",
                columns: new[] { "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_post_likes_TenantId",
                table: "post_likes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_post_likes_UserId",
                table: "post_likes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_post_statistics_CreatedAt",
                table: "post_statistics",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_statistics_DeletedAt",
                table: "post_statistics",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_statistics_PostId",
                table: "post_statistics",
                column: "PostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_post_statistics_TenantId",
                table: "post_statistics",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_post_tag_assignments_CreatedAt",
                table: "post_tag_assignments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_tag_assignments_DeletedAt",
                table: "post_tag_assignments",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_tag_assignments_PostId",
                table: "post_tag_assignments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_post_tag_assignments_TagId",
                table: "post_tag_assignments",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_post_tag_assignments_TenantId",
                table: "post_tag_assignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_post_tags_Category",
                table: "post_tags",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_post_tags_CreatedAt",
                table: "post_tags",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_tags_DeletedAt",
                table: "post_tags",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_tags_Name",
                table: "post_tags",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_post_tags_TenantId",
                table: "post_tags",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_post_tags_UsageCount",
                table: "post_tags",
                column: "UsageCount");

            migrationBuilder.CreateIndex(
                name: "IX_post_views_CreatedAt",
                table: "post_views",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_views_DeletedAt",
                table: "post_views",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_post_views_IpAddress",
                table: "post_views",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_post_views_PostId",
                table: "post_views",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_post_views_TenantId",
                table: "post_views",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_post_views_UserId",
                table: "post_views",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_post_views_ViewedAt",
                table: "post_views",
                column: "ViewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_posts_AuthorId",
                table: "posts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_posts_CreatedAt",
                table: "posts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_posts_DeletedAt",
                table: "posts",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_posts_IsPinned",
                table: "posts",
                column: "IsPinned");

            migrationBuilder.CreateIndex(
                name: "IX_posts_IsSystemGenerated",
                table: "posts",
                column: "IsSystemGenerated");

            migrationBuilder.CreateIndex(
                name: "IX_posts_MetadataId",
                table: "posts",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_posts_PostType",
                table: "posts",
                column: "PostType");

            migrationBuilder.CreateIndex(
                name: "IX_posts_TenantId",
                table: "posts",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post_comments");

            migrationBuilder.DropTable(
                name: "post_content_references");

            migrationBuilder.DropTable(
                name: "post_followers");

            migrationBuilder.DropTable(
                name: "post_likes");

            migrationBuilder.DropTable(
                name: "post_statistics");

            migrationBuilder.DropTable(
                name: "post_tag_assignments");

            migrationBuilder.DropTable(
                name: "post_views");

            migrationBuilder.DropTable(
                name: "post_tags");

            migrationBuilder.DropTable(
                name: "posts");
        }
    }
}
