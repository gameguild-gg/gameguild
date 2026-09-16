using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEconomyCoreReserveAuthority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_risk_decisions_versions_positive",
                table: "economy_risk_decisions");

            migrationBuilder.AddColumn<long>(
                name: "ReserveAuthorizationEpoch",
                table: "economy_risk_decisions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReserveAuthorizationEpoch",
                table: "economy_posting_groups",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReserveAuthorizationEpoch",
                table: "economy_dispatch_snapshots",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE economy_risk_decisions SET "ReserveAuthorizationEpoch" = 0 WHERE "ReserveAuthorizationEpoch" IS NULL;
                UPDATE economy_posting_groups SET "ReserveAuthorizationEpoch" = 0 WHERE "ReserveAuthorizationEpoch" IS NULL;
                UPDATE economy_dispatch_snapshots SET "ReserveAuthorizationEpoch" = 0 WHERE "ReserveAuthorizationEpoch" IS NULL;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "ReserveAuthorizationEpoch",
                table: "economy_risk_decisions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ReserveAuthorizationEpoch",
                table: "economy_posting_groups",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ReserveAuthorizationEpoch",
                table: "economy_dispatch_snapshots",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "economy_reserve_heads",
                columns: table => new
                {
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    AuthorizationEpoch = table.Column<long>(type: "bigint", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HardFaceValueUsdMinor = table.Column<long>(type: "bigint", nullable: false),
                    RequiredHardReserveUsdMinor = table.Column<long>(type: "bigint", nullable: false),
                    SoftFaceValueUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    StressedExpectedRedemptionCostUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    RequiredSoftReserveUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    HardBackingUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    SoftBackingUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    Coverage = table.Column<int>(type: "integer", nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_reserve_heads", x => x.Version);
                    table.CheckConstraint("ck_economy_reserve_heads_amounts_nonnegative", "\"HardFaceValueUsdMinor\" >= 0 AND \"RequiredHardReserveUsdMinor\" >= 0 AND \"SoftFaceValueUsdNanos\" >= 0 AND \"StressedExpectedRedemptionCostUsdNanos\" >= 0 AND \"RequiredSoftReserveUsdNanos\" >= 0 AND \"HardBackingUsdNanos\" >= 0 AND \"SoftBackingUsdNanos\" >= 0");
                    table.CheckConstraint("ck_economy_reserve_heads_values_valid", "\"Coverage\" IN (1, 2) AND length(btrim(\"EvidenceHash\")) > 0");
                    table.CheckConstraint("ck_economy_reserve_heads_versions_positive", "\"Version\" > 0 AND \"PolicyVersion\" > 0 AND \"AuthorizationEpoch\" > 0");
                    table.CheckConstraint("ck_economy_reserve_heads_window", "\"ExpiresAt\" > \"ObservedAt\" AND \"ActivatedAt\" >= \"ObservedAt\"");
                });

            migrationBuilder.CreateTable(
                name: "economy_reserve_asset_allocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReserveVersion = table.Column<long>(type: "bigint", nullable: false),
                    AssetKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    EligibleUsdNanos = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_reserve_asset_allocations", x => x.Id);
                    table.CheckConstraint("ck_economy_reserve_asset_allocations_value_positive", "\"EligibleUsdNanos\" > 0");
                    table.CheckConstraint("ck_economy_reserve_asset_allocations_values_valid", "\"Purpose\" IN (1, 2) AND length(btrim(\"AssetKey\")) > 0");
                    table.ForeignKey(
                        name: "FK_economy_reserve_asset_allocations_economy_reserve_heads_Res~",
                        column: x => x.ReserveVersion,
                        principalTable: "economy_reserve_heads",
                        principalColumn: "Version",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                ALTER TABLE economy_risk_decisions
                    ADD CONSTRAINT ck_economy_risk_decisions_versions_positive
                    CHECK ("PolicyVersion" > 0 AND "ReserveVersion" > 0 AND "ReserveAuthorizationEpoch" > 0 AND "FeatureVersion" > 0 AND "CounterVersion" > 0 AND "EntityGraphVersion" >= 0) NOT VALID;
                ALTER TABLE economy_posting_groups
                    ADD CONSTRAINT ck_economy_posting_groups_reserve_authorization
                    CHECK ("ReserveVersion" > 0 AND "ReserveAuthorizationEpoch" > 0 AND "RiskDecisionId" IS NOT NULL) NOT VALID;
                ALTER TABLE economy_dispatch_snapshots
                    ADD CONSTRAINT ck_economy_dispatch_snapshots_reserve_authorization
                    CHECK ("ReserveVersion" > 0 AND "ReserveAuthorizationEpoch" > 0) NOT VALID;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_economy_reserve_asset_allocations_version_asset",
                table: "economy_reserve_asset_allocations",
                columns: new[] { "ReserveVersion", "AssetKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_reserve_heads_active",
                table: "economy_reserve_heads",
                column: "IsActive",
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "ux_economy_reserve_heads_authorization_epoch",
                table: "economy_reserve_heads",
                column: "AuthorizationEpoch",
                unique: true);

            AddCoreReserveSecurity(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveCoreReserveSecurity(migrationBuilder);

            migrationBuilder.DropTable(
                name: "economy_reserve_asset_allocations");

            migrationBuilder.DropTable(
                name: "economy_reserve_heads");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_risk_decisions_versions_positive",
                table: "economy_risk_decisions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_posting_groups_reserve_authorization",
                table: "economy_posting_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_dispatch_snapshots_reserve_authorization",
                table: "economy_dispatch_snapshots");

            migrationBuilder.DropColumn(
                name: "ReserveAuthorizationEpoch",
                table: "economy_risk_decisions");

            migrationBuilder.DropColumn(
                name: "ReserveAuthorizationEpoch",
                table: "economy_posting_groups");

            migrationBuilder.DropColumn(
                name: "ReserveAuthorizationEpoch",
                table: "economy_dispatch_snapshots");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_risk_decisions_versions_positive",
                table: "economy_risk_decisions",
                sql: "\"PolicyVersion\" > 0 AND \"ReserveVersion\" > 0 AND \"FeatureVersion\" > 0 AND \"CounterVersion\" > 0 AND \"EntityGraphVersion\" >= 0");
        }
    }
}
