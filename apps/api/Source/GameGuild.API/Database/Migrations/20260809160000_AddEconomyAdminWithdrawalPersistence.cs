using System;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260809160000_AddEconomyAdminWithdrawalPersistence")]
public partial class AddEconomyAdminWithdrawalPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "economy_admin_withdrawal_runs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                PlatformFeeWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                SourceAssetKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                DestinationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                Version = table.Column<long>(type: "bigint", nullable: false),
                FencingToken = table.Column<long>(type: "bigint", nullable: false),
                ExecutionEpoch = table.Column<long>(type: "bigint", nullable: false),
                ReserveVersion = table.Column<long>(type: "bigint", nullable: false),
                ReserveAuthorizationEpoch = table.Column<long>(type: "bigint", nullable: false),
                PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                DispatchSnapshotHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                ProviderTransferId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_economy_admin_withdrawal_runs", x => x.Id);
                table.CheckConstraint("ck_economy_admin_withdrawal_runs_amount_positive", "\"AmountUnits\" > 0");
                table.CheckConstraint("ck_economy_admin_withdrawal_runs_approval",
                    "(\"State\" = 1 AND \"ApprovedBy\" IS NULL) OR (\"State\" BETWEEN 2 AND 7 AND \"ApprovedBy\" IS NOT NULL)");
                table.CheckConstraint("ck_economy_admin_withdrawal_runs_dispatch_snapshot",
                    "(\"State\" IN (1, 2) AND \"DispatchSnapshotHash\" IS NULL) OR (\"State\" BETWEEN 3 AND 7 AND \"DispatchSnapshotHash\" IS NOT NULL)");
                table.CheckConstraint("ck_economy_admin_withdrawal_runs_positive_versions",
                    "\"Version\" > 0 AND \"FencingToken\" > 0 AND \"ExecutionEpoch\" > 0 AND \"ReserveVersion\" > 0 AND \"ReserveAuthorizationEpoch\" > 0 AND \"PolicyVersion\" > 0");
                table.CheckConstraint("ck_economy_admin_withdrawal_runs_state", "\"State\" BETWEEN 1 AND 7");
                table.CheckConstraint("ck_economy_admin_withdrawal_runs_timestamps", "\"UpdatedAt\" >= \"CreatedAt\"");
                table.ForeignKey(
                    name: "FK_economy_admin_withdrawal_runs_economy_wallets_PlatformFeeWalletId",
                    column: x => x.PlatformFeeWalletId,
                    principalTable: "economy_wallets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "economy_admin_withdrawal_audit_events",
            columns: table => new
            {
                RunId = table.Column<Guid>(type: "uuid", nullable: false),
                Sequence = table.Column<long>(type: "bigint", nullable: false),
                Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                Evidence = table.Column<string>(type: "text", nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                PreviousHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_economy_admin_withdrawal_audit_events", x => new { x.RunId, x.Sequence });
                table.CheckConstraint("ck_economy_admin_withdrawal_audit_events_sequence", "\"Sequence\" > 0");
                table.ForeignKey(
                    name: "FK_economy_admin_withdrawal_audit_events_economy_admin_withdrawal_runs_RunId",
                    column: x => x.RunId,
                    principalTable: "economy_admin_withdrawal_runs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "economy_admin_withdrawal_provider_events",
            columns: table => new
            {
                EventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                EventHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                RunId = table.Column<Guid>(type: "uuid", nullable: false),
                RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_economy_admin_withdrawal_provider_events", x => x.EventId);
                table.ForeignKey(
                    name: "FK_economy_admin_withdrawal_provider_events_economy_admin_withdrawal_runs_RunId",
                    column: x => x.RunId,
                    principalTable: "economy_admin_withdrawal_runs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ux_economy_admin_withdrawal_audit_events_hash",
            table: "economy_admin_withdrawal_audit_events",
            column: "Hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_economy_admin_withdrawal_provider_events_run_recorded",
            table: "economy_admin_withdrawal_provider_events",
            columns: new[] { "RunId", "RecordedAt" });

        migrationBuilder.CreateIndex(
            name: "ux_economy_admin_withdrawal_runs_active_period",
            table: "economy_admin_withdrawal_runs",
            column: "PeriodStart",
            unique: true,
            filter: "\"State\" NOT IN (6, 7)");

        migrationBuilder.CreateIndex(
            name: "ux_economy_admin_withdrawal_runs_idempotency",
            table: "economy_admin_withdrawal_runs",
            column: "IdempotencyKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_economy_admin_withdrawal_runs_state_updated",
            table: "economy_admin_withdrawal_runs",
            columns: new[] { "State", "UpdatedAt" });

        InstallAdminWithdrawalSecurity(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemoveAdminWithdrawalSecurity(migrationBuilder);

        migrationBuilder.DropTable(name: "economy_admin_withdrawal_audit_events");
        migrationBuilder.DropTable(name: "economy_admin_withdrawal_provider_events");
        migrationBuilder.DropTable(name: "economy_admin_withdrawal_runs");
    }
}
