using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "gameguild.sla");

            migrationBuilder.CreateTable(
                name: "service_level_objectives",
                schema: "gameguild.sla",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetPercentage = table.Column<double>(type: "double precision", nullable: false),
                    TimeWindowDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    ErrorBudgetPercentage = table.Column<double>(type: "double precision", nullable: false),
                    AlertThresholdPercentage = table.Column<double>(type: "double precision", nullable: false, defaultValue: 50.0),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastEvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CurrentActualPercentage = table.Column<double>(type: "double precision", nullable: true),
                    RemainingErrorBudget = table.Column<double>(type: "double precision", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_level_objectives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "service_level_indicators",
                schema: "gameguild.sla",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceLevelObjectiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_level_indicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_level_indicators_service_level_objectives_ServiceLe~",
                        column: x => x.ServiceLevelObjectiveId,
                        principalSchema: "gameguild.sla",
                        principalTable: "service_level_objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "slo_violations",
                schema: "gameguild.sla",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceLevelObjectiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActualValue = table.Column<double>(type: "double precision", nullable: false),
                    TargetValue = table.Column<double>(type: "double precision", nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AlertTriggered = table.Column<bool>(type: "boolean", nullable: false),
                    AlertSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    AcknowledgedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_slo_violations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_slo_violations_service_level_objectives_ServiceLevelObjecti~",
                        column: x => x.ServiceLevelObjectiveId,
                        principalSchema: "gameguild.sla",
                        principalTable: "service_level_objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sli_slo_id",
                schema: "gameguild.sla",
                table: "service_level_indicators",
                column: "ServiceLevelObjectiveId");

            migrationBuilder.CreateIndex(
                name: "ix_sli_slo_timestamp",
                schema: "gameguild.sla",
                table: "service_level_indicators",
                columns: new[] { "ServiceLevelObjectiveId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_sli_timestamp",
                schema: "gameguild.sla",
                table: "service_level_indicators",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_slo_is_enabled",
                schema: "gameguild.sla",
                table: "service_level_objectives",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "ix_slo_service_name",
                schema: "gameguild.sla",
                table: "service_level_objectives",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "ix_slo_status",
                schema: "gameguild.sla",
                table: "service_level_objectives",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_slo_tenant_id",
                schema: "gameguild.sla",
                table: "service_level_objectives",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_sloviolation_severity",
                schema: "gameguild.sla",
                table: "slo_violations",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "ix_sloviolation_slo_id",
                schema: "gameguild.sla",
                table: "slo_violations",
                column: "ServiceLevelObjectiveId");

            migrationBuilder.CreateIndex(
                name: "ix_sloviolation_started_at",
                schema: "gameguild.sla",
                table: "slo_violations",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "ix_sloviolation_tenant_id",
                schema: "gameguild.sla",
                table: "slo_violations",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_level_indicators",
                schema: "gameguild.sla");

            migrationBuilder.DropTable(
                name: "slo_violations",
                schema: "gameguild.sla");

            migrationBuilder.DropTable(
                name: "service_level_objectives",
                schema: "gameguild.sla");
        }
    }
}
