using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

public partial class AllowBountyFifoReservationPurpose
{
    private static void InstallBountyFifoReservationWriter(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.reserve_bounty_fifo_fragments_v1(
                p_operation_id uuid,
                p_wallet_id uuid,
                p_currency integer,
                p_provenance integer,
                p_required_units bigint,
                p_purpose integer,
                p_reserved_at timestamptz)
            RETURNS TABLE(
                reservation_id uuid,
                parent_lot_id uuid,
                root_source_stamp_id uuid,
                reversal_epoch bigint,
                start_inclusive bigint,
                end_exclusive bigint,
                amount_units bigint)
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                IF p_operation_id IS NULL OR p_wallet_id IS NULL OR p_required_units <= 0
                   OR p_currency NOT IN (1, 2) OR p_provenance NOT IN (1, 2, 3, 4)
                   OR p_purpose <> 6 OR p_reserved_at IS NULL THEN
                    RAISE EXCEPTION 'bounty FIFO reservation arguments are invalid' USING ERRCODE = '22023';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM public.economy_fragment_reservations reservation
                    WHERE reservation."OperationId" = p_operation_id) THEN
                    IF EXISTS (
                        SELECT 1
                        FROM public.economy_fragment_reservations reservation
                        WHERE reservation."OperationId" = p_operation_id
                          AND (reservation."WalletId" <> p_wallet_id
                               OR reservation."Currency" <> p_currency
                               OR reservation."Purpose" <> 6)
                    ) THEN
                        RAISE EXCEPTION 'bounty FIFO reservation operation is bound to another request' USING ERRCODE = '23505';
                    END IF;

                    RETURN QUERY
                    SELECT reservation."Id", reservation."ParentLotId", reservation."RootSourceStampId",
                           reservation."ReversalEpoch", reservation."StartInclusive", reservation."EndExclusive",
                           (reservation."EndExclusive" - reservation."StartInclusive") /
                           CASE WHEN reservation."Currency" = 1 THEN 1000 ELSE 1 END
                    FROM public.economy_fragment_reservations reservation
                    JOIN public.economy_credit_lots lot ON lot."Id" = reservation."ParentLotId"
                    WHERE reservation."OperationId" = p_operation_id
                    ORDER BY lot."ConfirmedAt", lot."JournalSequence", lot."Id", reservation."StartInclusive";
                    RETURN;
                END IF;

                PERFORM *
                FROM economy_private.reserve_fifo_fragments_v1(
                    p_operation_id,
                    p_wallet_id,
                    p_currency,
                    p_provenance,
                    p_required_units,
                    4,
                    p_reserved_at);

                UPDATE public.economy_fragment_reservations
                SET "Purpose" = 6
                WHERE "OperationId" = p_operation_id
                  AND "Purpose" = 4;

                RETURN QUERY
                SELECT reservation."Id", reservation."ParentLotId", reservation."RootSourceStampId",
                       reservation."ReversalEpoch", reservation."StartInclusive", reservation."EndExclusive",
                       (reservation."EndExclusive" - reservation."StartInclusive") /
                       CASE WHEN reservation."Currency" = 1 THEN 1000 ELSE 1 END
                FROM public.economy_fragment_reservations reservation
                JOIN public.economy_credit_lots lot ON lot."Id" = reservation."ParentLotId"
                WHERE reservation."OperationId" = p_operation_id
                ORDER BY lot."ConfirmedAt", lot."JournalSequence", lot."Id", reservation."StartInclusive";
            END
            $function$;

            ALTER FUNCTION economy_private.reserve_bounty_fifo_fragments_v1(uuid,uuid,integer,integer,bigint,integer,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.reserve_bounty_fifo_fragments_v1(uuid,uuid,integer,integer,bigint,integer,timestamptz) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.reserve_bounty_fifo_fragments_v1(uuid,uuid,integer,integer,bigint,integer,timestamptz)
                TO gameguild_economy_writer;
            """);
    }

    private static void RemoveBountyFifoReservationWriter(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.reserve_bounty_fifo_fragments_v1(uuid,uuid,integer,integer,bigint,integer,timestamptz);
            """);
    }
}
