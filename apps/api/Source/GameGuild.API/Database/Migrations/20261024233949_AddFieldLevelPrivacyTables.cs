using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldLevelPrivacyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BulkExportEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    RecordCount = table.Column<long>(type: "bigint", nullable: false),
                    Threshold = table.Column<long>(type: "bigint", nullable: false),
                    ThresholdExceeded = table.Column<bool>(type: "boolean", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Destination = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AlertRaised = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkExportEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrossBorderTransferLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationJurisdiction = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    DataCategories = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RecordCount = table.Column<long>(type: "bigint", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrossBorderTransferLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldAccessAnomalies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnomalyType = table.Column<int>(type: "integer", nullable: false),
                    ObservedValue = table.Column<double>(type: "double precision", nullable: false),
                    BaselineValue = table.Column<double>(type: "double precision", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    WindowStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WindowEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldAccessAnomalies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldAccessAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccessType = table.Column<int>(type: "integer", nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSensitiveField = table.Column<bool>(type: "boolean", nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    MaskedOldValue = table.Column<string>(type: "text", nullable: true),
                    MaskedNewValue = table.Column<string>(type: "text", nullable: true),
                    SensitivityLevel = table.Column<int>(type: "integer", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApiEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LegalBasis = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ConsentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequiresNotification = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSent = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldAccessAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GdprProcessingActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivityName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    LegalBasis = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CategoriesOfDataSubjects = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CategoriesOfPersonalData = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Recipients = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    InvolvesCrossBorderTransfers = table.Column<bool>(type: "boolean", nullable: false),
                    TransferDestinations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RetentionPeriod = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SecurityMeasures = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsAutoDerived = table.Column<bool>(type: "boolean", nullable: false),
                    LastReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GdprProcessingActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SensitiveDataRetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfiguredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastEnforcedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastEnforcedCount = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensitiveDataRetentionPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SensitiveFieldClassifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Sensitivity = table.Column<int>(type: "integer", nullable: false),
                    DetectionRule = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    ObservationCount = table.Column<int>(type: "integer", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensitiveFieldClassifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulkExportEvents_TenantId_OccurredAtUtc",
                table: "BulkExportEvents",
                columns: new[] { "TenantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkExportEvents_TenantId_UserId_OccurredAtUtc",
                table: "BulkExportEvents",
                columns: new[] { "TenantId", "UserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkExportEvents_ThresholdExceeded",
                table: "BulkExportEvents",
                column: "ThresholdExceeded");

            migrationBuilder.CreateIndex(
                name: "IX_CrossBorderTransferLogs_Status",
                table: "CrossBorderTransferLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CrossBorderTransferLogs_TenantId_OccurredAtUtc",
                table: "CrossBorderTransferLogs",
                columns: new[] { "TenantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccessAnomalies_TenantId_DetectedAtUtc",
                table: "FieldAccessAnomalies",
                columns: new[] { "TenantId", "DetectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccessAnomalies_TenantId_Status",
                table: "FieldAccessAnomalies",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccessAnomalies_UserId",
                table: "FieldAccessAnomalies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccessAudits_EntityId",
                table: "FieldAccessAudits",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccessAudits_EntityType_FieldName",
                table: "FieldAccessAudits",
                columns: new[] { "EntityType", "FieldName" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccessAudits_TenantId_AccessedAt",
                table: "FieldAccessAudits",
                columns: new[] { "TenantId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccessAudits_UserId",
                table: "FieldAccessAudits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GdprProcessingActivities_TenantId_ActivityName",
                table: "GdprProcessingActivities",
                columns: new[] { "TenantId", "ActivityName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveDataRetentionPolicies_TenantId_Category",
                table: "SensitiveDataRetentionPolicies",
                columns: new[] { "TenantId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveFieldClassifications_Category",
                table: "SensitiveFieldClassifications",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveFieldClassifications_TenantId_EntityType_FieldName",
                table: "SensitiveFieldClassifications",
                columns: new[] { "TenantId", "EntityType", "FieldName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulkExportEvents");

            migrationBuilder.DropTable(
                name: "CrossBorderTransferLogs");

            migrationBuilder.DropTable(
                name: "FieldAccessAnomalies");

            migrationBuilder.DropTable(
                name: "FieldAccessAudits");

            migrationBuilder.DropTable(
                name: "GdprProcessingActivities");

            migrationBuilder.DropTable(
                name: "SensitiveDataRetentionPolicies");

            migrationBuilder.DropTable(
                name: "SensitiveFieldClassifications");
        }
    }
}
