using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceQuotaPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "gameguild.resources");

            migrationBuilder.EnsureSchema(
                name: "resources");

            migrationBuilder.CreateTable(
                name: "cost_allocation_reports",
                schema: "gameguild.resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResourceUsageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalUsage = table.Column<long>(type: "bigint", nullable: false),
                    CostPerUnit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AllocationTags = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CostCenter = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Project = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsExported = table.Column<bool>(type: "boolean", nullable: false),
                    ExportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InvoiceReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cost_allocation_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resource_metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DataType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsSystemManaged = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_metadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "resource_quotas",
                schema: "resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Unique identifier for the resource quota"),
                    Type = table.Column<int>(type: "integer", nullable: false, comment: "Type of resource being limited"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SoftLimit = table.Column<long>(type: "bigint", nullable: true, comment: "Soft limit (warning threshold)"),
                    HardLimit = table.Column<long>(type: "bigint", nullable: true, comment: "Hard limit (enforcement threshold)"),
                    CurrentUsage = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L, comment: "Current usage amount"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Whether this quota is actively enforced"),
                    Period = table.Column<int>(type: "integer", nullable: false, comment: "Period type for quota reset"),
                    LastReset = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResetTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ResetDayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    ResetDayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    NotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationThresholds = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true, comment: "Additional metadata stored as JSON"),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true, comment: "Optimistic concurrency token for quota updates"),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "When the quota was created"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "When the quota was last updated"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Tenant that owns this quota")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_quotas", x => x.Id);
                    table.CheckConstraint("CK_ResourceQuota_CurrentUsage_LessEqual_MaxUsage", "\"HardLimit\" IS NULL OR \"CurrentUsage\" <= \"HardLimit\"");
                    table.CheckConstraint("CK_ResourceQuota_CurrentUsage_NonNegative", "\"CurrentUsage\" >= 0");
                    table.CheckConstraint("CK_ResourceQuota_MaxUsage_NonNegative", "\"HardLimit\" IS NULL OR \"HardLimit\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "resource_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DefaultValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DataType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsSystemManaged = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AllowUserOverride = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValidationRules = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "resource_throttling_policies",
                schema: "gameguild.resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Strategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ThrottlingThresholdPercent = table.Column<int>(type: "integer", nullable: false, defaultValue: 80),
                    MaxRequestsPerWindow = table.Column<int>(type: "integer", nullable: true),
                    WindowDurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    DegradationFactor = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0.5m),
                    PriorityThreshold = table.Column<int>(type: "integer", nullable: true),
                    Configuration = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_throttling_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resource_usage_trends",
                schema: "gameguild.resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AverageUsage = table.Column<double>(type: "double precision", nullable: false),
                    MinUsage = table.Column<long>(type: "bigint", nullable: false),
                    MaxUsage = table.Column<long>(type: "bigint", nullable: false),
                    StandardDeviation = table.Column<double>(type: "double precision", nullable: false),
                    GrowthRate = table.Column<double>(type: "double precision", nullable: false),
                    AnomalyCount = table.Column<int>(type: "integer", nullable: false),
                    PeakUsageTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Pattern = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Steady"),
                    PatternConfidence = table.Column<double>(type: "double precision", nullable: false, defaultValue: 1.0),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_usage_trends", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usage_records",
                schema: "resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Unique identifier for the usage record"),
                    Type = table.Column<int>(type: "integer", nullable: false, comment: "Type of resource used"),
                    Count = table.Column<long>(type: "bigint", nullable: false, comment: "Amount of resource consumed"),
                    UsageAmount = table.Column<long>(type: "bigint", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the usage period started"),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the usage period ended"),
                    AveragePerDay = table.Column<double>(type: "double precision", nullable: true, comment: "Average usage per day"),
                    PeakUsage = table.Column<long>(type: "bigint", nullable: true, comment: "Peak usage during period"),
                    PeakUsageDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When peak usage occurred"),
                    Metadata = table.Column<string>(type: "jsonb", maxLength: 1000, nullable: true, comment: "Additional metadata in JSON format"),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceQuotaId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Associated resource quota"),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Tenant that used the resource")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_records", x => x.Id);
                    table.CheckConstraint("CK_UsageRecord_Count_NonNegative", "\"Count\" >= 0");
                    table.CheckConstraint("CK_UsageRecord_PeakUsage_NonNegative", "\"PeakUsage\" IS NULL OR \"PeakUsage\" >= 0");
                    table.CheckConstraint("CK_UsageRecord_PeriodOrder", "\"PeriodEnd\" >= \"PeriodStart\"");
                });

            migrationBuilder.CreateTable(
                name: "usage_retention_policies",
                schema: "gameguild.resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 90),
                    ArchiveAfterDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    EnableCompaction = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CompactionIntervalDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 7),
                    DownSamplingStrategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "daily"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextExecutionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Configuration = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_retention_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sla_impact_analyses",
                schema: "gameguild.resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceQuotaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ViolationStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ViolationEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ViolationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpectedValue = table.Column<long>(type: "bigint", nullable: false),
                    ActualValue = table.Column<long>(type: "bigint", nullable: false),
                    DeviationPercentage = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BusinessImpact = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RootCause = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MitigationActions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequiresEscalation = table.Column<bool>(type: "boolean", nullable: false),
                    IncidentCreated = table.Column<bool>(type: "boolean", nullable: false),
                    IncidentTicketId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sla_impact_analyses", x => x.id);
                    table.ForeignKey(
                        name: "FK_sla_impact_analyses_resource_quotas_ResourceQuotaId",
                        column: x => x.ResourceQuotaId,
                        principalSchema: "resources",
                        principalTable: "resource_quotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_costallocationreport_period",
                schema: "gameguild.resources",
                table: "cost_allocation_reports",
                columns: new[] { "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "ix_costallocationreport_tenant_id",
                schema: "gameguild.resources",
                table: "cost_allocation_reports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_costallocationreport_type",
                schema: "gameguild.resources",
                table: "cost_allocation_reports",
                column: "ResourceUsageType");

            migrationBuilder.CreateIndex(
                name: "IX_resource_metadata_TenantId_Category",
                table: "resource_metadata",
                columns: new[] { "TenantId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_resource_metadata_TenantId_Key",
                table: "resource_metadata",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource_metadata_UserId_Key",
                table: "resource_metadata",
                columns: new[] { "UserId", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceQuotas_ResourceType",
                schema: "resources",
                table: "resource_quotas",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceQuotas_TenantId_ResourceType",
                schema: "resources",
                table: "resource_quotas",
                columns: new[] { "TenantId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource_settings_TenantId_Category",
                table: "resource_settings",
                columns: new[] { "TenantId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_resource_settings_TenantId_Key",
                table: "resource_settings",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource_settings_UserId_Key",
                table: "resource_settings",
                columns: new[] { "UserId", "Key" });

            migrationBuilder.CreateIndex(
                name: "ix_resourcethrottlingpolicy_tenant_id",
                schema: "gameguild.resources",
                table: "resource_throttling_policies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_resourcethrottlingpolicy_tenant_type",
                schema: "gameguild.resources",
                table: "resource_throttling_policies",
                columns: new[] { "TenantId", "ResourceType" });

            migrationBuilder.CreateIndex(
                name: "ix_resourceusagetrend_period",
                schema: "gameguild.resources",
                table: "resource_usage_trends",
                columns: new[] { "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "ix_resourceusagetrend_tenant_id",
                schema: "gameguild.resources",
                table: "resource_usage_trends",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_resourceusagetrend_tenant_type",
                schema: "gameguild.resources",
                table: "resource_usage_trends",
                columns: new[] { "TenantId", "ResourceType" });

            migrationBuilder.CreateIndex(
                name: "ix_slaimpactanalysis_is_resolved",
                schema: "gameguild.resources",
                table: "sla_impact_analyses",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "ix_slaimpactanalysis_quota_id",
                schema: "gameguild.resources",
                table: "sla_impact_analyses",
                column: "ResourceQuotaId");

            migrationBuilder.CreateIndex(
                name: "ix_slaimpactanalysis_severity",
                schema: "gameguild.resources",
                table: "sla_impact_analyses",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "ix_slaimpactanalysis_start_time",
                schema: "gameguild.resources",
                table: "sla_impact_analyses",
                column: "ViolationStartTime");

            migrationBuilder.CreateIndex(
                name: "ix_slaimpactanalysis_tenant_id",
                schema: "gameguild.resources",
                table: "sla_impact_analyses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageRecords_PeriodStart",
                schema: "resources",
                table: "usage_records",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_UsageRecords_Tenant_Resource_Time",
                schema: "resources",
                table: "usage_records",
                columns: new[] { "TenantId", "Type", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageRecords_UsagePeriod",
                schema: "resources",
                table: "usage_records",
                columns: new[] { "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "ix_usageretentionpolicy_is_active",
                schema: "gameguild.resources",
                table: "usage_retention_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "ix_usageretentionpolicy_next_execution",
                schema: "gameguild.resources",
                table: "usage_retention_policies",
                column: "NextExecutionAt");

            migrationBuilder.CreateIndex(
                name: "ix_usageretentionpolicy_tenant_id",
                schema: "gameguild.resources",
                table: "usage_retention_policies",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cost_allocation_reports",
                schema: "gameguild.resources");

            migrationBuilder.DropTable(
                name: "resource_metadata");

            migrationBuilder.DropTable(
                name: "resource_settings");

            migrationBuilder.DropTable(
                name: "resource_throttling_policies",
                schema: "gameguild.resources");

            migrationBuilder.DropTable(
                name: "resource_usage_trends",
                schema: "gameguild.resources");

            migrationBuilder.DropTable(
                name: "sla_impact_analyses",
                schema: "gameguild.resources");

            migrationBuilder.DropTable(
                name: "usage_records",
                schema: "resources");

            migrationBuilder.DropTable(
                name: "usage_retention_policies",
                schema: "gameguild.resources");

            migrationBuilder.DropTable(
                name: "resource_quotas",
                schema: "resources");
        }
    }
}
