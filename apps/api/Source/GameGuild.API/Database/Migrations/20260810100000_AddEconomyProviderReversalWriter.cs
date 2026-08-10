using System;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260810100000_AddEconomyProviderReversalWriter")]
public partial class AddEconomyProviderReversalWriter : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "economy_provider_reversal_operations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                RootSourceStampId = table.Column<Guid>(type: "uuid", nullable: false),
                CumulativeHardUnits = table.Column<long>(type: "bigint", nullable: false),
                ReversalEpoch = table.Column<long>(type: "bigint", nullable: false),
                IrrecoverableDisposition = table.Column<int>(type: "integer", nullable: false),
                RecoveredHardUnits = table.Column<long>(type: "bigint", nullable: false),
                RecoveredConvertedSoftUnits = table.Column<long>(type: "bigint", nullable: false),
                ResponsibleDebtHardUnits = table.Column<long>(type: "bigint", nullable: false),
                PlatformLossHardUnits = table.Column<long>(type: "bigint", nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_economy_provider_reversal_operations", x => x.Id);
                table.ForeignKey(
                    name: "FK_economy_provider_reversal_operations_economy_source_stamps_RootSourceStampId",
                    column: x => x.RootSourceStampId,
                    principalTable: "economy_source_stamps",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint(
                    "ck_economy_provider_reversal_operations_amounts",
                    "\"CumulativeHardUnits\" > 0 AND \"ReversalEpoch\" >= 0 AND \"IrrecoverableDisposition\" IN (1, 2) AND \"RecoveredHardUnits\" >= 0 AND \"RecoveredConvertedSoftUnits\" >= 0 AND \"ResponsibleDebtHardUnits\" >= 0 AND \"PlatformLossHardUnits\" >= 0");
            });

        migrationBuilder.CreateIndex(
            name: "ux_economy_provider_reversal_operations_idempotency",
            table: "economy_provider_reversal_operations",
            column: "IdempotencyKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_economy_provider_reversal_operations_root",
            table: "economy_provider_reversal_operations",
            column: "RootSourceStampId");

        migrationBuilder.CreateTable(
            name: "economy_provider_reversal_fragments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                PostingGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                ParentLotId = table.Column<Guid>(type: "uuid", nullable: false),
                WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                Currency = table.Column<int>(type: "integer", nullable: false),
                AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                StartInclusive = table.Column<long>(type: "bigint", nullable: false),
                EndExclusive = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_economy_provider_reversal_fragments", x => x.Id);
                table.ForeignKey(
                    name: "FK_economy_provider_reversal_fragments_economy_provider_reversal_operations_OperationId",
                    column: x => x.OperationId,
                    principalTable: "economy_provider_reversal_operations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.CheckConstraint(
                    "ck_economy_provider_reversal_fragments_range",
                    "\"Currency\" IN (1, 2) AND \"AmountUnits\" > 0 AND \"StartInclusive\" >= 0 AND \"EndExclusive\" > \"StartInclusive\"");
            });

        migrationBuilder.CreateIndex(
            name: "ix_economy_provider_reversal_fragments_operation",
            table: "economy_provider_reversal_fragments",
            column: "OperationId");

        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.economy_wallet_debts (
                "Id" uuid PRIMARY KEY,
                "WalletId" uuid NOT NULL REFERENCES public.economy_wallets("Id") ON DELETE RESTRICT,
                "RootSourceStampId" uuid NOT NULL REFERENCES public.economy_source_stamps("Id") ON DELETE RESTRICT,
                "Currency" integer NOT NULL,
                "AmountUnits" bigint NOT NULL,
                "OutstandingUnits" bigint NOT NULL,
                "State" integer NOT NULL,
                "CreatedAt" timestamptz NOT NULL,
                "UpdatedAt" timestamptz NOT NULL,
                CONSTRAINT ck_economy_wallet_debts_amounts CHECK ("Currency" = 1 AND "AmountUnits" > 0 AND "OutstandingUnits" >= 0 AND "OutstandingUnits" <= "AmountUnits")
            );
            CREATE INDEX IF NOT EXISTS ix_economy_wallet_debts_wallet_root
                ON public.economy_wallet_debts("WalletId", "RootSourceStampId");

            CREATE TABLE IF NOT EXISTS public.economy_wallet_debt_events (
                "Id" uuid PRIMARY KEY,
                "DebtId" uuid NOT NULL REFERENCES public.economy_wallet_debts("Id") ON DELETE RESTRICT,
                "OperationId" uuid NOT NULL,
                "Kind" integer NOT NULL,
                "AmountUnits" bigint NOT NULL,
                "OccurredAt" timestamptz NOT NULL,
                CONSTRAINT ck_economy_wallet_debt_events_amount CHECK ("AmountUnits" > 0)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ux_economy_wallet_debt_events_operation
                ON public.economy_wallet_debt_events("OperationId");
            """);

        InstallProviderReversalWriter(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemoveProviderReversalWriter(migrationBuilder);
        migrationBuilder.DropTable(name: "economy_provider_reversal_fragments");
        migrationBuilder.DropTable(name: "economy_provider_reversal_operations");
        migrationBuilder.Sql("DROP TABLE IF EXISTS public.economy_wallet_debt_events;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS public.economy_wallet_debts;");
    }
}
