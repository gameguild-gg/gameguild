using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811106000_PreserveBountyEscrowProvenance")]
public partial class PreserveBountyEscrowProvenance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.economy_bounty_escrow_fragments
                ADD COLUMN IF NOT EXISTS "Provenance" integer NULL;

            UPDATE public.economy_bounty_escrow_fragments fragment
            SET "Provenance" = lot."Provenance"
            FROM public.economy_credit_lots lot
            WHERE lot."Id" = fragment."ParentLotId"
              AND fragment."Provenance" IS NULL;

            DO $block$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM public.economy_bounty_escrow_fragments
                    WHERE "Provenance" IS NULL) THEN
                    RAISE EXCEPTION 'cannot preserve bounty escrow provenance: parent lot is missing';
                END IF;
            END
            $block$;

            ALTER TABLE public.economy_bounty_escrow_fragments
                ALTER COLUMN "Provenance" SET NOT NULL;
            ALTER TABLE public.economy_bounty_escrow_fragments
                ADD CONSTRAINT ck_economy_bounty_escrow_fragments_provenance
                CHECK ("Provenance" BETWEEN 1 AND 7);
            """);

        InstallBountyEscrowProvenanceWriter(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemoveBountyEscrowProvenanceWriter(migrationBuilder);

        migrationBuilder.Sql(
            """
            ALTER TABLE public.economy_bounty_escrow_fragments
                DROP CONSTRAINT IF EXISTS ck_economy_bounty_escrow_fragments_provenance;
            ALTER TABLE public.economy_bounty_escrow_fragments
                DROP COLUMN IF EXISTS "Provenance";
            """);
    }
}
