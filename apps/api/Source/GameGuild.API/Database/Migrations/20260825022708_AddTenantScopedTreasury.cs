using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantScopedTreasury : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_economy_admin_withdrawal_runs_active_period",
                table: "economy_admin_withdrawal_runs");

            migrationBuilder.DropIndex(
                name: "ux_economy_admin_withdrawal_runs_idempotency",
                table: "economy_admin_withdrawal_runs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_economy_admin_withdrawal_provider_events",
                table: "economy_admin_withdrawal_provider_events");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "economy_admin_withdrawal_runs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "economy_admin_withdrawal_provider_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "economy_admin_withdrawal_dispatch_outbox",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "economy_admin_withdrawal_audit_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            BackfillAdminWithdrawalTenantScope(migrationBuilder);

            migrationBuilder.AddPrimaryKey(
                name: "PK_economy_admin_withdrawal_provider_events",
                table: "economy_admin_withdrawal_provider_events",
                columns: new[] { "TenantId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "ux_economy_admin_withdrawal_runs_active_period",
                table: "economy_admin_withdrawal_runs",
                columns: new[] { "TenantId", "PeriodStart" },
                unique: true,
                filter: "\"State\" NOT IN (6, 7)");

            migrationBuilder.CreateIndex(
                name: "ux_economy_admin_withdrawal_runs_idempotency",
                table: "economy_admin_withdrawal_runs",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_admin_withdrawal_dispatch_outbox_TenantId_RunId",
                table: "economy_admin_withdrawal_dispatch_outbox",
                columns: new[] { "TenantId", "RunId" },
                unique: true);

            InstallTenantScopedAdminWithdrawalSecurity(migrationBuilder);
            InstallRiskCounterReservationTransitions(migrationBuilder);
            InstallRegisteredEconomyCapabilities(migrationBuilder);
            HardenPayoutFifoEligibility.InstallHardenedPayoutFifoEligibility(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveTenantScopedAdminWithdrawalSecurity(migrationBuilder);
            RemoveRiskCounterReservationTransitions(migrationBuilder);
            RemoveRegisteredEconomyCapabilities(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "ux_economy_admin_withdrawal_runs_active_period",
                table: "economy_admin_withdrawal_runs");

            migrationBuilder.DropIndex(
                name: "ux_economy_admin_withdrawal_runs_idempotency",
                table: "economy_admin_withdrawal_runs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_economy_admin_withdrawal_provider_events",
                table: "economy_admin_withdrawal_provider_events");

            migrationBuilder.DropIndex(
                name: "IX_economy_admin_withdrawal_dispatch_outbox_TenantId_RunId",
                table: "economy_admin_withdrawal_dispatch_outbox");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "economy_admin_withdrawal_runs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "economy_admin_withdrawal_provider_events");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "economy_admin_withdrawal_dispatch_outbox");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "economy_admin_withdrawal_audit_events");

            migrationBuilder.AddPrimaryKey(
                name: "PK_economy_admin_withdrawal_provider_events",
                table: "economy_admin_withdrawal_provider_events",
                column: "EventId");

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
        }
    }
}
