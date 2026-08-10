using System;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260809203000_AddEconomyFifoReservationWriter")]
public partial class AddEconomyFifoReservationWriter : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_economy_credit_lots_root_source",
            table: "economy_credit_lots");

        migrationBuilder.CreateIndex(
            name: "ix_economy_credit_lots_root_source",
            table: "economy_credit_lots",
            column: "RootSourceStampId");

        migrationBuilder.Sql(
            """
            ALTER TABLE public.economy_fragment_root_ranges
                DROP CONSTRAINT IF EXISTS ex_economy_fragment_root_ranges_no_overlap;

            CREATE TABLE public.economy_fragment_reservations (
                "Id" uuid NOT NULL,
                "OperationId" uuid NOT NULL,
                "ParentLotId" uuid NOT NULL,
                "WalletId" uuid NOT NULL,
                "Currency" integer NOT NULL,
                "Purpose" integer NOT NULL,
                "Status" integer NOT NULL,
                "RootSourceStampId" uuid NOT NULL,
                "ReversalEpoch" bigint NOT NULL,
                "StartInclusive" bigint NOT NULL,
                "EndExclusive" bigint NOT NULL,
                "ReservedAt" timestamptz NOT NULL,
                "TerminalAt" timestamptz NULL,
                CONSTRAINT "PK_economy_fragment_reservations" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_economy_fragment_reservations_economy_credit_lots_ParentLotId"
                    FOREIGN KEY ("ParentLotId") REFERENCES public.economy_credit_lots ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_economy_fragment_reservations_economy_wallets_WalletId"
                    FOREIGN KEY ("WalletId") REFERENCES public.economy_wallets ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_economy_fragment_reservations_economy_source_stamps_RootSourceStampId"
                    FOREIGN KEY ("RootSourceStampId") REFERENCES public.economy_source_stamps ("Id") ON DELETE RESTRICT,
                CONSTRAINT "ck_economy_fragment_reservations_range"
                    CHECK ("StartInclusive" >= 0 AND "EndExclusive" > "StartInclusive"),
                CONSTRAINT "ck_economy_fragment_reservations_lifecycle"
                    CHECK (("Status" = 1 AND "TerminalAt" IS NULL) OR ("Status" IN (2, 3) AND "TerminalAt" IS NOT NULL)),
                CONSTRAINT "ck_economy_fragment_reservations_state"
                    CHECK ("Purpose" BETWEEN 1 AND 5 AND "Status" BETWEEN 1 AND 3)
            );

            CREATE INDEX "ix_economy_fragment_reservations_operation" ON public.economy_fragment_reservations ("OperationId");
            CREATE INDEX "ix_economy_fragment_reservations_parent_status" ON public.economy_fragment_reservations ("ParentLotId", "Status");
            ALTER TABLE public.economy_fragment_reservations
                ADD CONSTRAINT "ex_economy_fragment_reservations_active_no_overlap"
                EXCLUDE USING gist (
                    "ParentLotId" WITH =,
                    int8range("StartInclusive", "EndExclusive", '[)') WITH &&
                ) WHERE ("Status" = 1);
            """);

        InstallFifoReservationWriter(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemoveFifoReservationWriter(migrationBuilder);
        migrationBuilder.DropTable(name: "economy_fragment_reservations");
        migrationBuilder.DropIndex(name: "ix_economy_credit_lots_root_source", table: "economy_credit_lots");
        migrationBuilder.CreateIndex(
            name: "ux_economy_credit_lots_root_source",
            table: "economy_credit_lots",
            column: "RootSourceStampId",
            unique: true);
        migrationBuilder.Sql(
            """
            ALTER TABLE public.economy_fragment_root_ranges
                ADD CONSTRAINT ex_economy_fragment_root_ranges_no_overlap
                EXCLUDE USING gist (
                    "RootSourceStampId" WITH =,
                    "ReversalEpoch" WITH =,
                    int8range("StartInclusive", "EndExclusive", '[)') WITH &&
                );
            """);
    }
}
