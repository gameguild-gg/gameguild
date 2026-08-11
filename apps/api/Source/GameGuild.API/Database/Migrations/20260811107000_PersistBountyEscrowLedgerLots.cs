using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811107000_PersistBountyEscrowLedgerLots")]
public partial class PersistBountyEscrowLedgerLots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.economy_bounty_escrow_fragments
                ADD COLUMN IF NOT EXISTS "EscrowLotId" uuid NULL;

            CREATE UNIQUE INDEX IF NOT EXISTS ux_economy_bounty_escrow_fragments_bounty_escrow_lot
                ON public.economy_bounty_escrow_fragments ("BountyId", "EscrowLotId")
                WHERE "EscrowLotId" IS NOT NULL;

            ALTER TABLE public.economy_bounty_escrow_fragments
                ADD CONSTRAINT fk_economy_bounty_escrow_fragments_escrow_lot
                FOREIGN KEY ("EscrowLotId") REFERENCES public.economy_credit_lots ("Id")
                ON DELETE RESTRICT;
            """);

        InstallBountyEscrowLedgerLotsWriter(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemoveBountyEscrowLedgerLotsWriter(migrationBuilder);

        migrationBuilder.Sql(
            """
            ALTER TABLE public.economy_bounty_escrow_fragments
                DROP CONSTRAINT IF EXISTS fk_economy_bounty_escrow_fragments_escrow_lot;
            DROP INDEX IF EXISTS public.ux_economy_bounty_escrow_fragments_bounty_escrow_lot;
            ALTER TABLE public.economy_bounty_escrow_fragments
                DROP COLUMN IF EXISTS "EscrowLotId";
            """);
    }
}
