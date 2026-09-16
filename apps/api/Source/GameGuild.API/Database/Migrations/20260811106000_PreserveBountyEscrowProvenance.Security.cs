using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

public partial class PreserveBountyEscrowProvenance
{
    private static void InstallBountyEscrowProvenanceWriter(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.create_bounty_escrow_v2(
                p_bounty_id uuid,
                p_poster_id uuid,
                p_poster_wallet_id uuid,
                p_escrow_wallet_id uuid,
                p_currency integer,
                p_amount_units bigint,
                p_reclaim_fee_ppm integer,
                p_requires_prerequisite boolean,
                p_minimum_reputation integer,
                p_requires_instructor_verification boolean,
                p_idempotency_key text,
                p_request_hash text,
                p_posted_at timestamptz,
                p_expires_at timestamptz,
                p_fragments jsonb)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                existing_bounty public.economy_bounties%ROWTYPE;
            BEGIN
                IF p_fragments IS NULL OR jsonb_typeof(p_fragments) <> 'array'
                   OR jsonb_array_length(p_fragments) = 0 THEN
                    RAISE EXCEPTION 'bounty escrow provenance payload is invalid' USING ERRCODE = '22023';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM jsonb_to_recordset(p_fragments) AS fragment(
                        "ParentLotId" uuid,
                        "Currency" integer,
                        "Provenance" integer)
                    LEFT JOIN public.economy_credit_lots lot ON lot."Id" = fragment."ParentLotId"
                    WHERE fragment."ParentLotId" IS NULL
                       OR fragment."Provenance" NOT BETWEEN 1 AND 7
                       OR lot."Id" IS NULL
                       OR lot."Currency" <> fragment."Currency"
                       OR lot."Provenance" <> fragment."Provenance") THEN
                    RAISE EXCEPTION 'bounty escrow provenance does not match its parent lot' USING ERRCODE = '23514';
                END IF;

                SELECT * INTO existing_bounty
                FROM public.economy_bounties
                WHERE "IdempotencyKey" = btrim(p_idempotency_key)
                FOR UPDATE;
                IF FOUND THEN
                    IF existing_bounty."Id" <> p_bounty_id
                       OR existing_bounty."RequestHash" <> btrim(p_request_hash) THEN
                        RAISE EXCEPTION 'bounty idempotency key is bound to another request' USING ERRCODE = '23505';
                    END IF;
                    IF (SELECT count(*)
                        FROM public.economy_bounty_escrow_fragments stored
                        WHERE stored."BountyId" = p_bounty_id) <>
                       (SELECT count(*)
                        FROM jsonb_to_recordset(p_fragments) AS supplied("ParentLotId" uuid))
                       OR EXISTS (
                        SELECT 1
                        FROM public.economy_bounty_escrow_fragments stored
                        LEFT JOIN jsonb_to_recordset(p_fragments) AS supplied(
                            "ParentLotId" uuid,
                            "Provenance" integer)
                            ON supplied."ParentLotId" = stored."ParentLotId"
                        WHERE stored."BountyId" = p_bounty_id
                          AND (supplied."ParentLotId" IS NULL OR stored."Provenance" <> supplied."Provenance")) THEN
                        RAISE EXCEPTION 'bounty post replay does not match immutable escrow provenance' USING ERRCODE = '23505';
                    END IF;
                    RETURN;
                END IF;

                PERFORM economy_private.create_bounty_escrow_v1(
                    p_bounty_id,
                    p_poster_id,
                    p_poster_wallet_id,
                    p_escrow_wallet_id,
                    p_currency,
                    p_amount_units,
                    p_reclaim_fee_ppm,
                    p_requires_prerequisite,
                    p_minimum_reputation,
                    p_requires_instructor_verification,
                    p_idempotency_key,
                    p_request_hash,
                    p_posted_at,
                    p_expires_at,
                    p_fragments);

                UPDATE public.economy_bounty_escrow_fragments stored
                SET "Provenance" = supplied."Provenance"
                FROM jsonb_to_recordset(p_fragments) AS supplied(
                    "ParentLotId" uuid,
                    "Provenance" integer)
                WHERE stored."BountyId" = p_bounty_id
                  AND stored."ParentLotId" = supplied."ParentLotId";
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_bounty_escrow_fragments_v2(p_bounty_id uuid)
            RETURNS TABLE(
                "ParentLotId" uuid,
                "Currency" integer,
                "Provenance" integer,
                "AmountUnits" bigint,
                "TraceUnitsPerCoinUnit" bigint,
                "SelectedRootRanges" jsonb)
            LANGUAGE sql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT fragment."ParentLotId", fragment."Currency", fragment."Provenance", fragment."AmountUnits",
                       fragment."TraceUnitsPerCoinUnit", fragment."SelectedRootRanges"
                FROM public.economy_bounty_escrow_fragments fragment
                WHERE fragment."BountyId" = p_bounty_id
                ORDER BY fragment."ParentLotId"
            $function$;

            ALTER FUNCTION economy_private.create_bounty_escrow_v2(uuid,uuid,uuid,uuid,integer,bigint,integer,boolean,integer,boolean,text,text,timestamptz,timestamptz,jsonb)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_bounty_escrow_fragments_v2(uuid)
                OWNER TO gameguild_economy_procedure_owner;

            REVOKE EXECUTE ON FUNCTION economy_private.create_bounty_escrow_v1(uuid,uuid,uuid,uuid,integer,bigint,integer,boolean,integer,boolean,text,text,timestamptz,timestamptz,jsonb)
                FROM gameguild_economy_writer;
            REVOKE ALL ON FUNCTION economy_private.create_bounty_escrow_v2(uuid,uuid,uuid,uuid,integer,bigint,integer,boolean,integer,boolean,text,text,timestamptz,timestamptz,jsonb)
                FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_bounty_escrow_fragments_v2(uuid) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.create_bounty_escrow_v2(uuid,uuid,uuid,uuid,integer,bigint,integer,boolean,integer,boolean,text,text,timestamptz,timestamptz,jsonb),
                economy_private.read_bounty_escrow_fragments_v2(uuid)
                TO gameguild_economy_writer;
            """);
    }

    private static void RemoveBountyEscrowProvenanceWriter(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.create_bounty_escrow_v2(uuid,uuid,uuid,uuid,integer,bigint,integer,boolean,integer,boolean,text,text,timestamptz,timestamptz,jsonb);
            DROP FUNCTION IF EXISTS economy_private.read_bounty_escrow_fragments_v2(uuid);
            GRANT EXECUTE ON FUNCTION economy_private.create_bounty_escrow_v1(uuid,uuid,uuid,uuid,integer,bigint,integer,boolean,integer,boolean,text,text,timestamptz,timestamptz,jsonb)
                TO gameguild_economy_writer;
            """);
    }
}
