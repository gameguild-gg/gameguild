using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddContentPagesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CategorySlug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Tags = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VideoUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DownloadUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExternalUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LinkedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MetaTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MetaDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OgImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StructuredData = table.Column<string>(type: "jsonb", nullable: true),
                    ReadingTimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    ViewCount = table.Column<long>(type: "bigint", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduledPublishAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomData = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_resources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    MetaTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MetaDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetaKeywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CanonicalUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RobotsDirective = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OgTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    OgDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OgImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OgType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TwitterCard = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TwitterSite = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StructuredData = table.Column<string>(type: "jsonb", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true),
                    CustomData = table.Column<string>(type: "jsonb", nullable: true),
                    ParentPageId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduledPublishAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pages_pages_ParentPageId",
                        column: x => x.ParentPageId,
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "page_sections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PageId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Heading = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Subheading = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Data = table.Column<string>(type: "jsonb", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    CssClasses = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_page_sections_pages_PageId",
                        column: x => x.PageId,
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_resources_AuthorId",
                table: "content_resources",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_content_resources_CategorySlug",
                table: "content_resources",
                column: "CategorySlug");

            migrationBuilder.CreateIndex(
                name: "IX_content_resources_IsFeatured",
                table: "content_resources",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_content_resources_Locale",
                table: "content_resources",
                column: "Locale");

            migrationBuilder.CreateIndex(
                name: "IX_content_resources_PublishedAt",
                table: "content_resources",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_content_resources_ResourceType",
                table: "content_resources",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_content_resources_Slug",
                table: "content_resources",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_resources_Status",
                table: "content_resources",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_page_sections_PageId",
                table: "page_sections",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_page_sections_PageId_SortOrder",
                table: "page_sections",
                columns: new[] { "PageId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_page_sections_SectionType",
                table: "page_sections",
                column: "SectionType");

            migrationBuilder.CreateIndex(
                name: "IX_pages_Locale",
                table: "pages",
                column: "Locale");

            migrationBuilder.CreateIndex(
                name: "IX_pages_PageType",
                table: "pages",
                column: "PageType");

            migrationBuilder.CreateIndex(
                name: "IX_pages_ParentPageId",
                table: "pages",
                column: "ParentPageId");

            migrationBuilder.CreateIndex(
                name: "IX_pages_Slug",
                table: "pages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pages_Status",
                table: "pages",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_resources");

            migrationBuilder.DropTable(
                name: "page_sections");

            migrationBuilder.DropTable(
                name: "pages");
        }
    }
}
