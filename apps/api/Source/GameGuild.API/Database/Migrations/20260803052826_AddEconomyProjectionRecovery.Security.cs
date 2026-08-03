using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

public partial class AddEconomyProjectionRecovery
{
    private static void InstallProjectionRecovery(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TRIGGER deny_projection_reconciliation_event_mutation
                BEFORE UPDATE OR DELETE ON public.economy_projection_reconciliation_events
                FOR EACH ROW EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();

            CREATE OR REPLACE FUNCTION economy_private.rebuild_wallet_projection_v1(
                p_wallet_id uuid,
                p_as_of timestamptz)
            RETURNS TABLE(was_corrupt boolean, review_state integer, projection_hash text)
            LANGUAGE plpgsql
            VOLATILE
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                existing_projection record;
                existing_found boolean;
                pending_hard bigint;
                pending_soft bigint;
                purchased_hard bigint;
                earned_hard bigint;
                restricted_hard bigint;
                soft_units bigint;
                immature_earned_hard bigint;
                lot_held_hard bigint;
                lot_held_soft bigint;
                active_hold_hard bigint;
                active_hold_soft bigint;
                held_hard bigint;
                held_soft bigint;
                available_hard bigint;
                available_soft bigint;
                withdrawable_hard bigint;
                source_sequence bigint;
                rebuilt_hash text;
            BEGIN
                IF p_wallet_id IS NULL OR p_as_of IS NULL THEN
                    RAISE EXCEPTION 'wallet projection rebuild arguments are required' USING ERRCODE = '22023';
                END IF;

                PERFORM 1
                FROM public.economy_wallets wallet
                WHERE wallet."Id" = p_wallet_id
                FOR SHARE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'wallet does not exist' USING ERRCODE = '23503';
                END IF;

                SELECT projection.*
                INTO existing_projection
                FROM public.economy_wallet_balance_projections projection
                WHERE projection."WalletId" = p_wallet_id
                FOR UPDATE;
                existing_found := FOUND;

                SELECT
                    COALESCE(sum(claim."AuthoritativeUsdMinorUnits") FILTER (WHERE claim."State" = 1), 0),
                    0::bigint
                INTO pending_hard, pending_soft
                FROM public.economy_funding_claims claim
                WHERE claim."WalletId" = p_wallet_id;

                IF EXISTS (
                    SELECT 1
                    FROM public.economy_credit_lots lot
                    LEFT JOIN (
                        SELECT allocation."ParentLotId", sum(allocation."AmountUnits") AS consumed_units
                        FROM public.economy_entry_allocations allocation
                        GROUP BY allocation."ParentLotId"
                    ) consumed ON consumed."ParentLotId" = lot."Id"
                    WHERE lot."WalletId" = p_wallet_id
                      AND COALESCE(consumed.consumed_units, 0) > lot."AmountUnits"
                ) THEN
                    RAISE EXCEPTION 'projection source facts over-consume a credit lot' USING ERRCODE = '23514';
                END IF;

                WITH allocation_totals AS (
                    SELECT allocation."ParentLotId", sum(allocation."AmountUnits") AS consumed_units
                    FROM public.economy_entry_allocations allocation
                    GROUP BY allocation."ParentLotId"
                ),
                remaining_lots AS (
                    SELECT
                        lot."Currency" AS currency,
                        lot."Provenance" AS provenance,
                        lot."State" AS state,
                        lot."CashOutEligible" AS cash_out_eligible,
                        lot."OriginalMaturesAt" AS matures_at,
                        lot."AmountUnits" - COALESCE(allocation_totals.consumed_units, 0) AS remaining_units
                    FROM public.economy_credit_lots lot
                    LEFT JOIN allocation_totals ON allocation_totals."ParentLotId" = lot."Id"
                    WHERE lot."WalletId" = p_wallet_id
                      AND lot."State" IN (1, 2)
                )
                SELECT
                    COALESCE(sum(remaining_units) FILTER (WHERE currency = 1 AND provenance = 1), 0),
                    COALESCE(sum(remaining_units) FILTER (WHERE currency = 1 AND provenance = 2), 0),
                    COALESCE(sum(remaining_units) FILTER (WHERE currency = 1 AND provenance NOT IN (1, 2)), 0),
                    COALESCE(sum(remaining_units) FILTER (WHERE currency = 2), 0),
                    COALESCE(sum(remaining_units) FILTER (
                        WHERE currency = 1 AND provenance = 2 AND matures_at > p_as_of), 0),
                    COALESCE(sum(remaining_units) FILTER (WHERE currency = 1 AND state = 2), 0),
                    COALESCE(sum(remaining_units) FILTER (WHERE currency = 2 AND state = 2), 0),
                    COALESCE(sum(remaining_units) FILTER (WHERE currency = 1 AND state = 1), 0),
                    COALESCE(sum(remaining_units) FILTER (WHERE currency = 2 AND state = 1), 0),
                    COALESCE(sum(remaining_units) FILTER (
                        WHERE currency = 1 AND provenance = 2 AND state = 1
                          AND cash_out_eligible AND matures_at <= p_as_of), 0)
                INTO purchased_hard, earned_hard, restricted_hard, soft_units, immature_earned_hard,
                     lot_held_hard, lot_held_soft, available_hard, available_soft, withdrawable_hard
                FROM remaining_lots;

                SELECT
                    COALESCE(sum(hold."AmountUnits") FILTER (WHERE hold."Currency" = 1), 0),
                    COALESCE(sum(hold."AmountUnits") FILTER (WHERE hold."Currency" = 2), 0)
                INTO active_hold_hard, active_hold_soft
                FROM public.economy_holds hold
                WHERE hold."WalletId" = p_wallet_id
                  AND hold."Status" = 1;

                held_hard := LEAST(purchased_hard + earned_hard + restricted_hard,
                                   lot_held_hard + active_hold_hard);
                held_soft := LEAST(soft_units, lot_held_soft + active_hold_soft);
                available_hard := GREATEST(0, available_hard - active_hold_hard);
                available_soft := GREATEST(0, available_soft - active_hold_soft);
                withdrawable_hard := GREATEST(0, withdrawable_hard - active_hold_hard);

                SELECT COALESCE(head."Sequence", 0)
                INTO source_sequence
                FROM (SELECT 1) singleton
                LEFT JOIN public.economy_chain_head head ON head."Id" = 1;

                rebuilt_hash := encode(public.digest(convert_to(jsonb_build_array(
                    p_wallet_id,
                    pending_hard,
                    pending_soft,
                    purchased_hard,
                    earned_hard,
                    restricted_hard,
                    soft_units,
                    immature_earned_hard,
                    held_hard,
                    held_soft,
                    available_hard,
                    available_soft,
                    withdrawable_hard,
                    source_sequence)::text, 'UTF8'), 'sha256'), 'hex');

                was_corrupt := existing_found AND (
                    existing_projection."PendingHard" IS DISTINCT FROM pending_hard OR
                    existing_projection."PendingSoft" IS DISTINCT FROM pending_soft OR
                    existing_projection."PurchasedHard" IS DISTINCT FROM purchased_hard OR
                    existing_projection."EarnedHard" IS DISTINCT FROM earned_hard OR
                    existing_projection."RestrictedHard" IS DISTINCT FROM restricted_hard OR
                    existing_projection."Soft" IS DISTINCT FROM soft_units OR
                    existing_projection."ImmatureEarnedHard" IS DISTINCT FROM immature_earned_hard OR
                    existing_projection."HeldHard" IS DISTINCT FROM held_hard OR
                    existing_projection."HeldSoft" IS DISTINCT FROM held_soft OR
                    existing_projection."AvailableHardToSpend" IS DISTINCT FROM available_hard OR
                    existing_projection."AvailableSoftToSpend" IS DISTINCT FROM available_soft OR
                    existing_projection."WithdrawableHard" IS DISTINCT FROM withdrawable_hard OR
                    existing_projection."SourceJournalSequence" IS DISTINCT FROM source_sequence OR
                    existing_projection."ProjectionHash" IS DISTINCT FROM rebuilt_hash);
                review_state := CASE WHEN was_corrupt THEN 2 ELSE 1 END;
                projection_hash := rebuilt_hash;

                IF was_corrupt THEN
                    INSERT INTO public.economy_projection_reconciliation_events (
                        "Id", "WalletId", "PreviousHash", "RebuiltHash", "SourceJournalSequence", "DetectedAt")
                    VALUES (
                        gen_random_uuid(), p_wallet_id, existing_projection."ProjectionHash", rebuilt_hash,
                        source_sequence, p_as_of);
                END IF;

                INSERT INTO public.economy_wallet_balance_projections (
                    "WalletId", "PendingHard", "PendingSoft", "PurchasedHard", "EarnedHard", "RestrictedHard",
                    "Soft", "ImmatureEarnedHard", "HeldHard", "HeldSoft", "AvailableHardToSpend",
                    "AvailableSoftToSpend", "WithdrawableHard", "ReviewState", "SourceJournalSequence",
                    "ProjectionHash", "RebuiltAt")
                VALUES (
                    p_wallet_id, pending_hard, pending_soft, purchased_hard, earned_hard, restricted_hard,
                    soft_units, immature_earned_hard, held_hard, held_soft, available_hard, available_soft,
                    withdrawable_hard, review_state, source_sequence, rebuilt_hash, p_as_of)
                ON CONFLICT ("WalletId") DO UPDATE SET
                    "PendingHard" = EXCLUDED."PendingHard",
                    "PendingSoft" = EXCLUDED."PendingSoft",
                    "PurchasedHard" = EXCLUDED."PurchasedHard",
                    "EarnedHard" = EXCLUDED."EarnedHard",
                    "RestrictedHard" = EXCLUDED."RestrictedHard",
                    "Soft" = EXCLUDED."Soft",
                    "ImmatureEarnedHard" = EXCLUDED."ImmatureEarnedHard",
                    "HeldHard" = EXCLUDED."HeldHard",
                    "HeldSoft" = EXCLUDED."HeldSoft",
                    "AvailableHardToSpend" = EXCLUDED."AvailableHardToSpend",
                    "AvailableSoftToSpend" = EXCLUDED."AvailableSoftToSpend",
                    "WithdrawableHard" = EXCLUDED."WithdrawableHard",
                    "ReviewState" = EXCLUDED."ReviewState",
                    "SourceJournalSequence" = EXCLUDED."SourceJournalSequence",
                    "ProjectionHash" = EXCLUDED."ProjectionHash",
                    "RebuiltAt" = EXCLUDED."RebuiltAt";

                RETURN NEXT;
            END
            $function$;

            ALTER FUNCTION economy_private.rebuild_wallet_projection_v1(uuid,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.rebuild_wallet_projection_v1(uuid,timestamptz) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.rebuild_wallet_projection_v1(uuid,timestamptz)
                TO gameguild_economy_writer;

            GRANT SELECT ON TABLE
                public.economy_wallets,
                public.economy_funding_claims,
                public.economy_credit_lots,
                public.economy_entry_allocations,
                public.economy_holds,
                public.economy_chain_head
                TO gameguild_economy_procedure_owner;

            REVOKE ALL ON TABLE public.economy_wallet_balance_projections FROM PUBLIC;
            REVOKE ALL ON TABLE public.economy_wallet_balance_projections FROM gameguild_economy_writer;
            GRANT SELECT ON TABLE public.economy_wallet_balance_projections TO gameguild_economy_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE public.economy_wallet_balance_projections
                TO gameguild_economy_procedure_owner;
            GRANT ALL ON TABLE public.economy_wallet_balance_projections TO gameguild_economy_migration;

            REVOKE ALL ON TABLE public.economy_projection_reconciliation_events FROM PUBLIC;
            REVOKE ALL ON TABLE public.economy_projection_reconciliation_events FROM gameguild_economy_writer;
            GRANT SELECT ON TABLE public.economy_projection_reconciliation_events TO gameguild_economy_runtime;
            GRANT SELECT, INSERT ON TABLE public.economy_projection_reconciliation_events
                TO gameguild_economy_procedure_owner;
            GRANT ALL ON TABLE public.economy_projection_reconciliation_events TO gameguild_economy_migration;
            """);
    }

    private static void RemoveProjectionRecovery(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.rebuild_wallet_projection_v1(uuid,timestamptz);
            DROP TRIGGER IF EXISTS deny_projection_reconciliation_event_mutation
                ON public.economy_projection_reconciliation_events;
            """);
    }
}
