using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileApplicationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analytics_dashboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analytics_dashboards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "analytics_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Properties = table.Column<string>(type: "jsonb", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Referrer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analytics_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "analytics_kpi_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EventName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AggregationFunction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AggregateField = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analytics_kpi_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "consent_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PolicyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consent_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "content_version_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Feedback = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Suggestions = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_version_reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "content_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubmittedForReviewAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduledPublishAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCurrentVersion = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "data_subject_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProcessingNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProcessedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_subject_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "document_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SupportedEntityType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    PlaceholderSchema = table.Column<string>(type: "jsonb", nullable: true),
                    IsSystemTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "analytics_dashboard_widgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Configuration = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analytics_dashboard_widgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_analytics_dashboard_widgets_analytics_dashboards_DashboardId",
                        column: x => x.DashboardId,
                        principalTable: "analytics_dashboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "consent_policy_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consent_policy_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_consent_policy_versions_consent_policies_ConsentPolicyId",
                        column: x => x.ConsentPolicyId,
                        principalTable: "consent_policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_consents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsGranted = table.Column<bool>(type: "boolean", nullable: false),
                    ConsentGivenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsentRevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConsentMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_consents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_consents_consent_policy_versions_PolicyVersionId",
                        column: x => x.PolicyVersionId,
                        principalTable: "consent_policy_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analytics_dashboard_widgets_DashboardId",
                table: "analytics_dashboard_widgets",
                column: "DashboardId");

            migrationBuilder.CreateIndex(
                name: "IX_analytics_dashboards_Slug",
                table: "analytics_dashboards",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_EventName",
                table: "analytics_events",
                column: "EventName");

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_EventName_Timestamp",
                table: "analytics_events",
                columns: new[] { "EventName", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_TenantId",
                table: "analytics_events",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_Timestamp",
                table: "analytics_events",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_UserId",
                table: "analytics_events",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_UserId_Timestamp",
                table: "analytics_events",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_analytics_kpi_definitions_Name",
                table: "analytics_kpi_definitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consent_policies_IsActive",
                table: "consent_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_consent_policies_PolicyType",
                table: "consent_policies",
                column: "PolicyType");

            migrationBuilder.CreateIndex(
                name: "IX_consent_policy_versions_ConsentPolicyId",
                table: "consent_policy_versions",
                column: "ConsentPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_consent_policy_versions_EffectiveFrom",
                table: "consent_policy_versions",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_consent_policy_versions_VersionNumber",
                table: "consent_policy_versions",
                column: "VersionNumber");

            migrationBuilder.CreateIndex(
                name: "IX_content_version_reviews_ContentVersionId",
                table: "content_version_reviews",
                column: "ContentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_content_version_reviews_ReviewerId",
                table: "content_version_reviews",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_content_versions_CreatedAt",
                table: "content_versions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_content_versions_EntityId_EntityType",
                table: "content_versions",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_content_versions_EntityId_EntityType_VersionNumber",
                table: "content_versions",
                columns: new[] { "EntityId", "EntityType", "VersionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_content_versions_ScheduledPublishAt",
                table: "content_versions",
                column: "ScheduledPublishAt");

            migrationBuilder.CreateIndex(
                name: "IX_content_versions_Status",
                table: "content_versions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_data_subject_requests_RequestType",
                table: "data_subject_requests",
                column: "RequestType");

            migrationBuilder.CreateIndex(
                name: "IX_data_subject_requests_Status",
                table: "data_subject_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_data_subject_requests_UserId",
                table: "data_subject_requests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_document_templates_Category",
                table: "document_templates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_document_templates_SupportedEntityType",
                table: "document_templates",
                column: "SupportedEntityType");

            migrationBuilder.CreateIndex(
                name: "IX_document_templates_TemplateKey",
                table: "document_templates",
                column: "TemplateKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_consents_ConsentGivenAt",
                table: "user_consents",
                column: "ConsentGivenAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_consents_PolicyVersionId",
                table: "user_consents",
                column: "PolicyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_consents_UserId",
                table: "user_consents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_consents_UserId_PolicyVersionId",
                table: "user_consents",
                columns: new[] { "UserId", "PolicyVersionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analytics_dashboard_widgets");

            migrationBuilder.DropTable(
                name: "analytics_events");

            migrationBuilder.DropTable(
                name: "analytics_kpi_definitions");

            migrationBuilder.DropTable(
                name: "content_version_reviews");

            migrationBuilder.DropTable(
                name: "content_versions");

            migrationBuilder.DropTable(
                name: "data_subject_requests");

            migrationBuilder.DropTable(
                name: "document_templates");

            migrationBuilder.DropTable(
                name: "user_consents");

            migrationBuilder.DropTable(
                name: "analytics_dashboards");

            migrationBuilder.DropTable(
                name: "consent_policy_versions");

            migrationBuilder.DropTable(
                name: "consent_policies");
        }
    }
}
