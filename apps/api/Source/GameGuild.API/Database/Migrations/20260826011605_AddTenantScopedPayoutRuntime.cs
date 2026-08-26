using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantScopedPayoutRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_economy_payout_operations_idempotency",
                table: "economy_payout_operations");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "economy_payout_operations",
                type: "uuid",
                nullable: true);

            BackfillPayoutOperationTenantScope(migrationBuilder);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "economy_payout_operations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_payout_operations_positive_values",
                table: "economy_payout_operations");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_payout_operations_positive_values",
                table: "economy_payout_operations",
                sql: "\"AmountUnits\" > 0 AND \"Version\" > 0 AND \"FencingToken\" > 0 AND \"KillSwitchEpoch\" >= 0 AND \"ReserveVersion\" > 0 AND \"ReserveAuthorizationEpoch\" > 0 AND \"PolicyVersion\" > 0");

            migrationBuilder.CreateIndex(
                name: "ix_economy_payout_operations_tenant_state_updated",
                table: "economy_payout_operations",
                columns: new[] { "TenantId", "State", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "ux_economy_payout_operations_tenant_idempotency",
                table: "economy_payout_operations",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            InstallTenantScopedPayoutRuntime(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveTenantScopedPayoutRuntime(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "ix_economy_payout_operations_tenant_state_updated",
                table: "economy_payout_operations");

            migrationBuilder.DropIndex(
                name: "ux_economy_payout_operations_tenant_idempotency",
                table: "economy_payout_operations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "economy_payout_operations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_payout_operations_positive_values",
                table: "economy_payout_operations");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_payout_operations_positive_values",
                table: "economy_payout_operations",
                sql: "\"AmountUnits\" > 0 AND \"Version\" > 0 AND \"FencingToken\" > 0 AND \"KillSwitchEpoch\" > 0 AND \"ReserveVersion\" > 0 AND \"ReserveAuthorizationEpoch\" > 0 AND \"PolicyVersion\" > 0");

            migrationBuilder.CreateIndex(
                name: "ux_economy_payout_operations_idempotency",
                table: "economy_payout_operations",
                column: "IdempotencyKey",
                unique: true);
        }
    }
}
