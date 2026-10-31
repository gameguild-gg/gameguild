using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceLocalizationAndFxAnomalyGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnomalousRatesSkipped",
                table: "fx_rate_sync_runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AnomalyDetails",
                table: "fx_rate_sync_runs",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "price_localizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    LocalizedPrice = table.Column<decimal>(type: "numeric(28,12)", nullable: false),
                    IsAutoCalculated = table.Column<bool>(type: "boolean", nullable: false),
                    LastCalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExchangeRateUsed = table.Column<decimal>(type: "numeric(28,12)", nullable: true),
                    EffectiveDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_localizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "price_recalculation_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalItems = table.Column<int>(type: "integer", nullable: false),
                    CompletedItems = table.Column<int>(type: "integer", nullable: false),
                    UpdatedPrices = table.Column<int>(type: "integer", nullable: false),
                    PreservedOverrides = table.Column<int>(type: "integer", nullable: false),
                    SkippedForMissingRate = table.Column<int>(type: "integer", nullable: false),
                    BeforeSnapshot = table.Column<string>(type: "character varying(1000000)", maxLength: 1000000, nullable: false),
                    TriggeredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TriggerReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RolledBackAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RolledBackByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_recalculation_runs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_price_localizations_CurrencyCode",
                table: "price_localizations",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_price_localizations_EffectiveDateUtc",
                table: "price_localizations",
                column: "EffectiveDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_price_localizations_IsAutoCalculated",
                table: "price_localizations",
                column: "IsAutoCalculated");

            migrationBuilder.CreateIndex(
                name: "IX_price_localizations_PlanId",
                table: "price_localizations",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "ix_price_localizations_tenant_plan_currency_effective",
                table: "price_localizations",
                columns: new[] { "TenantId", "PlanId", "CurrencyCode", "EffectiveDateUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_price_localizations_TenantId",
                table: "price_localizations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_price_recalculation_runs_StartedAtUtc",
                table: "price_recalculation_runs",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_price_recalculation_runs_Status",
                table: "price_recalculation_runs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_price_recalculation_runs_TenantId",
                table: "price_recalculation_runs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_price_recalculation_runs_TriggerReason",
                table: "price_recalculation_runs",
                column: "TriggerReason");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "price_localizations");

            migrationBuilder.DropTable(
                name: "price_recalculation_runs");

            migrationBuilder.DropColumn(
                name: "AnomalousRatesSkipped",
                table: "fx_rate_sync_runs");

            migrationBuilder.DropColumn(
                name: "AnomalyDetails",
                table: "fx_rate_sync_runs");
        }
    }
}
