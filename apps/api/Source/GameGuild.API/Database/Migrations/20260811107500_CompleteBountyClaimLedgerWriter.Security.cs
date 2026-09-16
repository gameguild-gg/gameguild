using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

public partial class CompleteBountyClaimLedgerWriter
{
    private static void InstallBountyClaimLedgerWriter(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.complete_bounty_claim_v1(
                p_bounty_id uuid,
                p_claimant_id uuid,
                p_claimant_wallet_id uuid,
                p_idempotency_key text,
                p_posting_id uuid,
                p_risk_decision_id uuid,
                p_evidence_hash text,
                p_claimed_at timestamptz)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                bounty public.economy_bounties%ROWTYPE;
                existing public.economy_bounty_terminal_events%ROWTYPE;
                posting record;
                fragment record;
                escrow_lot public.economy_credit_lots%ROWTYPE;
                output_lot_id uuid;
                proceeds_source_id uuid;
                first_output_lot_id uuid;
                output_lots jsonb := '[]'::jsonb;
                fragment_allocated_units bigint;
                total_allocated_units bigint;
                root_range record;
                output_provenance integer;
                output_matures_at timestamptz;
                output_cash_out_eligible boolean;
                output_account integer;
            BEGIN
                IF p_bounty_id IS NULL OR p_claimant_id IS NULL OR p_claimant_wallet_id IS NULL
                   OR p_posting_id IS NULL OR p_risk_decision_id IS NULL
                   OR p_idempotency_key IS NULL OR length(btrim(p_idempotency_key)) = 0
                   OR p_evidence_hash IS NULL OR length(btrim(p_evidence_hash)) = 0
                   OR length(btrim(p_evidence_hash)) > 128 OR p_claimed_at IS NULL THEN
                    RAISE EXCEPTION 'invalid durable bounty claim arguments' USING ERRCODE = '22023';
                END IF;

                SELECT * INTO bounty
                FROM public.economy_bounties
                WHERE "Id" = p_bounty_id
                FOR UPDATE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'bounty escrow was not found' USING ERRCODE = 'P0002';
                END IF;

                SELECT * INTO existing
                FROM public.economy_bounty_terminal_events
                WHERE "BountyId" = p_bounty_id OR "IdempotencyKey" = btrim(p_idempotency_key)
                FOR UPDATE;
                IF FOUND THEN
                    IF existing."BountyId" <> p_bounty_id
                       OR existing."IdempotencyKey" <> btrim(p_idempotency_key)
                       OR existing."Status" <> 3
                       OR existing."ActorId" <> p_claimant_id
                       OR existing."DestinationWalletId" <> p_claimant_wallet_id
                       OR existing."RiskDecisionId" <> p_risk_decision_id THEN
                        RAISE EXCEPTION 'bounty claim idempotency key conflicts with immutable terminal outcome' USING ERRCODE = '23505';
                    END IF;
                    RETURN;
                END IF;

                IF bounty."Status" <> 1 THEN
                    RAISE EXCEPTION 'bounty already has a terminal outcome' USING ERRCODE = '23514';
                END IF;
                IF p_claimed_at >= bounty."ExpiresAt" THEN
                    RAISE EXCEPTION 'bounty can no longer be claimed' USING ERRCODE = '23514';
                END IF;
                IF p_claimant_id = bounty."PosterId"
                   OR p_claimant_wallet_id IN (bounty."PosterWalletId", bounty."EscrowWalletId") THEN
                    RAISE EXCEPTION 'a bounty poster cannot claim their own bounty' USING ERRCODE = '42501';
                END IF;

                SELECT group_record."Id", group_record."TemplateKind", group_record."Authority",
                       group_record."ActorId", group_record."TenantId", group_record."RiskDecisionId",
                       group_record."PolicyVersion", entry."Sequence"
                INTO posting
                FROM public.economy_posting_groups group_record
                JOIN public.economy_journal_entries entry ON entry."PostingGroupId" = group_record."Id"
                WHERE group_record."Id" = p_posting_id
                  AND group_record."IdempotencyKey" = btrim(p_idempotency_key)
                FOR SHARE;
                IF NOT FOUND OR posting."TemplateKind" <> 23 OR posting."Authority" <> 4
                   OR posting."ActorId" <> p_claimant_id OR posting."RiskDecisionId" <> p_risk_decision_id THEN
                    RAISE EXCEPTION 'bounty claim posting does not bind the claimant, authority, and risk decision' USING ERRCODE = '23514';
                END IF;
                IF NOT EXISTS (
                    SELECT 1
                    FROM public.economy_risk_decisions decision
                    WHERE decision."Id" = p_risk_decision_id
                      AND decision."Outcome" = 1
                      AND decision."TemplateKind" = 23
                      AND decision."AmountUnits" = bounty."AmountUnits"
                      AND decision."Currency" = bounty."Currency") THEN
                    RAISE EXCEPTION 'bounty claim risk decision is absent, denied, or mismatched' USING ERRCODE = '42501';
                END IF;

                output_provenance := CASE bounty."Currency" WHEN 1 THEN 2 WHEN 2 THEN 7 ELSE NULL END;
                output_account := CASE bounty."Currency" WHEN 1 THEN 3 WHEN 2 THEN 4 ELSE NULL END;
                IF output_provenance IS NULL THEN
                    RAISE EXCEPTION 'bounty claim requires a supported coin currency' USING ERRCODE = '23514';
                END IF;
                output_matures_at := CASE WHEN bounty."Currency" = 1
                    THEN p_claimed_at + INTERVAL '120 days' ELSE p_claimed_at END;
                output_cash_out_eligible := bounty."Currency" = 1;

                IF NOT EXISTS (
                    SELECT 1
                    FROM public.economy_journal_entries entry
                    JOIN public.economy_journal_lines line ON line."JournalEntryId" = entry."Id"
                    JOIN public.economy_accounts account ON account."Id" = line."AccountId"
                    WHERE entry."PostingGroupId" = p_posting_id
                      AND line."Side" = 1
                      AND account."Code" = CASE bounty."Currency" WHEN 1 THEN 9 ELSE 10 END
                      AND line."WalletId" IS NULL
                      AND line."Currency" = bounty."Currency"
                      AND line."AmountUnits" = bounty."AmountUnits") OR NOT EXISTS (
                    SELECT 1
                    FROM public.economy_journal_entries entry
                    JOIN public.economy_journal_lines line ON line."JournalEntryId" = entry."Id"
                    JOIN public.economy_accounts account ON account."Id" = line."AccountId"
                    WHERE entry."PostingGroupId" = p_posting_id
                      AND line."Side" = 2
                      AND account."Code" = output_account
                      AND line."WalletId" = p_claimant_wallet_id
                      AND line."Currency" = bounty."Currency"
                      AND line."Provenance" = output_provenance
                      AND line."AmountUnits" = bounty."AmountUnits") THEN
                    RAISE EXCEPTION 'bounty claim posting does not match the immutable escrow settlement shape' USING ERRCODE = '23514';
                END IF;

                SELECT COALESCE(sum(allocation."AmountUnits"), 0) INTO total_allocated_units
                FROM public.economy_entry_allocations allocation
                JOIN public.economy_journal_lines line ON line."Id" = allocation."JournalLineId"
                JOIN public.economy_journal_entries entry ON entry."Id" = line."JournalEntryId"
                WHERE entry."PostingGroupId" = p_posting_id AND line."Side" = 1;
                IF total_allocated_units <> bounty."AmountUnits" THEN
                    RAISE EXCEPTION 'bounty claim allocations do not conserve the escrow amount' USING ERRCODE = '23514';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM public.economy_entry_allocations allocation
                    JOIN public.economy_journal_lines line ON line."Id" = allocation."JournalLineId"
                    JOIN public.economy_journal_entries entry ON entry."Id" = line."JournalEntryId"
                    WHERE entry."PostingGroupId" = p_posting_id
                      AND line."Side" = 1
                      AND NOT EXISTS (
                          SELECT 1
                          FROM public.economy_bounty_escrow_fragments fragment
                          WHERE fragment."BountyId" = p_bounty_id
                            AND fragment."EscrowLotId" = allocation."ParentLotId")) THEN
                    RAISE EXCEPTION 'bounty claim posting allocates value outside the materialized escrow lots' USING ERRCODE = '23514';
                END IF;

                FOR fragment IN
                    SELECT *
                    FROM public.economy_bounty_escrow_fragments
                    WHERE "BountyId" = p_bounty_id
                    ORDER BY "EscrowLotId"
                    FOR UPDATE
                LOOP
                    IF fragment."EscrowLotId" IS NULL THEN
                        RAISE EXCEPTION 'bounty claim requires materialized escrow lots' USING ERRCODE = '23514';
                    END IF;
                    SELECT * INTO escrow_lot
                    FROM public.economy_credit_lots lot
                    WHERE lot."Id" = fragment."EscrowLotId"
                    FOR UPDATE;
                    IF NOT FOUND OR escrow_lot."WalletId" <> bounty."EscrowWalletId"
                       OR escrow_lot."Currency" <> bounty."Currency"
                       OR escrow_lot."AmountUnits" <> fragment."AmountUnits"
                       OR escrow_lot."State" <> 1 THEN
                        RAISE EXCEPTION 'bounty escrow lot is not active and bound to the open bounty' USING ERRCODE = '23514';
                    END IF;

                    SELECT COALESCE(sum(allocation."AmountUnits"), 0) INTO fragment_allocated_units
                    FROM public.economy_entry_allocations allocation
                    JOIN public.economy_journal_lines line ON line."Id" = allocation."JournalLineId"
                    JOIN public.economy_journal_entries entry ON entry."Id" = line."JournalEntryId"
                    WHERE entry."PostingGroupId" = p_posting_id
                      AND line."Side" = 1
                      AND allocation."ParentLotId" = fragment."EscrowLotId";
                    IF fragment_allocated_units <> fragment."AmountUnits" THEN
                        RAISE EXCEPTION 'bounty claim posting is not exactly bound to every escrow lot' USING ERRCODE = '23514';
                    END IF;
                END LOOP;

                proceeds_source_id := gen_random_uuid();
                INSERT INTO public.economy_source_stamps (
                    "Id", "SourceKind", "InternalSourceId", "SourceLegId", "Provider", "ProviderReference",
                    "EvidenceHash", "Provenance", "State", "ActorId", "TenantId", "PostingReferenceId",
                    "PolicyVersion", "AuthoritativeUnits", "ObservedAt", "ConfirmedAt")
                VALUES (
                    proceeds_source_id, 'bounty-claim', p_bounty_id::text, p_posting_id::text, NULL, NULL,
                    btrim(p_evidence_hash), output_provenance, 2, p_claimant_id, posting."TenantId", p_posting_id,
                    posting."PolicyVersion", bounty."AmountUnits", p_claimed_at, p_claimed_at);
                INSERT INTO public.economy_source_stamp_events (
                    "Id", "SourceStampId", "Sequence", "State", "EvidenceHash", "OccurredAt")
                VALUES (gen_random_uuid(), proceeds_source_id, 1, 2, btrim(p_evidence_hash), p_claimed_at);

                FOR fragment IN
                    SELECT *
                    FROM public.economy_bounty_escrow_fragments
                    WHERE "BountyId" = p_bounty_id
                    ORDER BY "EscrowLotId"
                    FOR UPDATE
                LOOP
                    SELECT * INTO escrow_lot
                    FROM public.economy_credit_lots lot
                    WHERE lot."Id" = fragment."EscrowLotId"
                    FOR UPDATE;
                    output_lot_id := gen_random_uuid();
                    INSERT INTO public.economy_credit_lots (
                        "Id", "WalletId", "RootSourceStampId", "Currency", "AmountUnits", "Provenance",
                        "CreditedAt", "ConfirmedAt", "OriginalMaturesAt", "CashOutEligible", "JournalSequence",
                        "State", "ReversalEpoch")
                    VALUES (
                        output_lot_id, p_claimant_wallet_id, escrow_lot."RootSourceStampId", bounty."Currency",
                        fragment."AmountUnits", output_provenance, p_claimed_at, p_claimed_at, output_matures_at,
                        output_cash_out_eligible, posting."Sequence", 1, escrow_lot."ReversalEpoch");
                    INSERT INTO public.economy_lot_lineage_edges (
                        "Id", "ParentLotId", "ChildLotId", "Currency", "AmountUnits")
                    VALUES (
                        gen_random_uuid(), escrow_lot."Id", output_lot_id, bounty."Currency", fragment."AmountUnits");
                    FOR root_range IN
                        SELECT * FROM public.economy_fragment_root_ranges
                        WHERE "CreditLotId" = escrow_lot."Id"
                        ORDER BY "RootSourceStampId", "StartInclusive", "EndExclusive"
                    LOOP
                        INSERT INTO public.economy_fragment_root_ranges (
                            "Id", "RootSourceStampId", "CreditLotId", "EntryAllocationId",
                            "StartInclusive", "EndExclusive", "ReversalEpoch")
                        VALUES (
                            gen_random_uuid(), root_range."RootSourceStampId", output_lot_id, NULL,
                            root_range."StartInclusive", root_range."EndExclusive", root_range."ReversalEpoch");
                    END LOOP;
                    UPDATE public.economy_credit_lots SET "State" = 3 WHERE "Id" = escrow_lot."Id";
                    first_output_lot_id := COALESCE(first_output_lot_id, output_lot_id);
                    output_lots := output_lots || jsonb_build_array(jsonb_build_object(
                        'LotId', output_lot_id,
                        'WalletId', p_claimant_wallet_id,
                        'Currency', bounty."Currency",
                        'AmountUnits', fragment."AmountUnits",
                        'Provenance', output_provenance,
                        'RootSourceStampId', escrow_lot."RootSourceStampId",
                        'ConfirmedAt', p_claimed_at,
                        'OriginalMaturesAt', output_matures_at,
                        'CashOutEligible', output_cash_out_eligible));
                END LOOP;

                IF first_output_lot_id IS NULL OR jsonb_array_length(output_lots) = 0 THEN
                    RAISE EXCEPTION 'bounty claim did not materialize output lots' USING ERRCODE = '23514';
                END IF;
                UPDATE public.economy_bounties
                SET "Status" = 3, "Version" = "Version" + 1
                WHERE "Id" = p_bounty_id;
                INSERT INTO public.economy_bounty_terminal_events (
                    "Id", "BountyId", "Status", "ActorId", "DestinationWalletId", "IdempotencyKey",
                    "RiskDecisionId", "ProceedsSourceStampId", "ProceedsLotId", "ReturnedUnits", "FeeUnits",
                    "FirstJournalSequence", "OutputLots", "OccurredAt")
                VALUES (
                    gen_random_uuid(), p_bounty_id, 3, p_claimant_id, p_claimant_wallet_id, btrim(p_idempotency_key),
                    p_risk_decision_id, proceeds_source_id, first_output_lot_id, 0, 0,
                    posting."Sequence", output_lots, p_claimed_at);
            END
            $function$;

            ALTER FUNCTION economy_private.complete_bounty_claim_v1(uuid,uuid,uuid,text,uuid,uuid,text,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.complete_bounty_claim_v1(uuid,uuid,uuid,text,uuid,uuid,text,timestamptz) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.complete_bounty_claim_v1(uuid,uuid,uuid,text,uuid,uuid,text,timestamptz)
                TO gameguild_economy_writer;

            GRANT SELECT, INSERT, UPDATE ON TABLE public.economy_bounties,
                public.economy_bounty_escrow_fragments,
                public.economy_bounty_terminal_events,
                public.economy_source_stamps,
                public.economy_source_stamp_events,
                public.economy_credit_lots,
                public.economy_lot_lineage_edges,
                public.economy_fragment_root_ranges
                TO gameguild_economy_procedure_owner;
            """);
    }

    private static void RemoveBountyClaimLedgerWriter(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.complete_bounty_claim_v1(uuid,uuid,uuid,text,uuid,uuid,text,timestamptz);
            """);
    }
}
