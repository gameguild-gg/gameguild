using System;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260809180000_AddEconomyPayoutOperationPersistence")]
public partial class AddEconomyPayoutOperationPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "economy_payout_operations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                PayeeId = table.Column<Guid>(type: "uuid", nullable: false),
                WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                ProviderAccountId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                DestinationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ProviderBindingHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EligibilityHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                DispatchSnapshotHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                ProviderPayoutId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                State = table.Column<int>(type: "integer", nullable: false),
                Version = table.Column<long>(type: "bigint", nullable: false),
                FencingToken = table.Column<long>(type: "bigint", nullable: false),
                KillSwitchEpoch = table.Column<long>(type: "bigint", nullable: false),
                ReserveVersion = table.Column<long>(type: "bigint", nullable: false),
                ReserveAuthorizationEpoch = table.Column<long>(type: "bigint", nullable: false),
                PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_economy_payout_operations", row => row.Id);
                table.CheckConstraint("ck_economy_payout_operations_dispatch",
                    "(\"State\" = 1 AND \"DispatchSnapshotHash\" IS NULL) OR (\"State\" BETWEEN 2 AND 6 AND \"DispatchSnapshotHash\" IS NOT NULL)");
                table.CheckConstraint("ck_economy_payout_operations_positive_values",
                    "\"AmountUnits\" > 0 AND \"Version\" > 0 AND \"FencingToken\" > 0 AND \"KillSwitchEpoch\" > 0 AND \"ReserveVersion\" > 0 AND \"ReserveAuthorizationEpoch\" > 0 AND \"PolicyVersion\" > 0");
                table.CheckConstraint("ck_economy_payout_operations_state", "\"State\" BETWEEN 1 AND 6");
                table.CheckConstraint("ck_economy_payout_operations_timestamps", "\"UpdatedAt\" >= \"CreatedAt\"");
                table.ForeignKey(
                    name: "FK_economy_payout_operations_economy_wallets_WalletId",
                    column: row => row.WalletId,
                    principalTable: "economy_wallets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "economy_payout_provider_events",
            columns: table => new
            {
                EventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                EventHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                ResultingState = table.Column<int>(type: "integer", nullable: false),
                RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_economy_payout_provider_events", row => row.EventId);
                table.CheckConstraint("ck_economy_payout_provider_events_terminal_state", "\"ResultingState\" IN (4, 5)");
                table.ForeignKey(
                    name: "FK_economy_payout_provider_events_economy_payout_operations_OperationId",
                    column: row => row.OperationId,
                    principalTable: "economy_payout_operations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ux_economy_payout_operations_idempotency",
            table: "economy_payout_operations",
            column: "IdempotencyKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_economy_payout_operations_state_updated",
            table: "economy_payout_operations",
            columns: new[] { "State", "UpdatedAt" });

        migrationBuilder.CreateIndex(
            name: "ix_economy_payout_provider_events_operation_recorded",
            table: "economy_payout_provider_events",
            columns: new[] { "OperationId", "RecordedAt" });

        InstallPayoutOperationSecurity(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemovePayoutOperationSecurity(migrationBuilder);
        migrationBuilder.DropTable(name: "economy_payout_provider_events");
        migrationBuilder.DropTable(name: "economy_payout_operations");
    }
}
