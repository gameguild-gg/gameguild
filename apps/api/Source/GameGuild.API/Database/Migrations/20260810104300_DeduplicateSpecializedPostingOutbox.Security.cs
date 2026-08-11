using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

public partial class DeduplicateSpecializedPostingOutbox
{
    private static void InstallSpecializedPostingOutboxDeduplication(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.confirm_observed_hard_coin_top_up_v1(
                p_capability_id uuid,
                p_actor_id uuid,
                p_tenant_id uuid,
                p_posting_id uuid,
                p_idempotency_key text,
                p_template_kind integer,
                p_template_version integer,
                p_authority integer,
                p_policy_version bigint,
                p_reserve_version bigint,
                p_risk_decision_id uuid,
                p_risk_operation_fingerprint text,
                p_expected_counter_version bigint,
                p_source_stamp_id uuid,
                p_source_evidence_hash text,
                p_requested_at timestamptz,
                p_lines jsonb,
                p_funding_claim_version bigint,
                p_credit_lot_id uuid,
                p_confirmation_event_hash text,
                p_dispatch_snapshot_hash text)
            RETURNS TABLE(posting_id uuid, journal_sequence bigint, journal_hash text, duplicate boolean)
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                funding record;
                source record;
                receipt record;
                next_sequence bigint;
                credit_line_id uuid;
            BEGIN
                IF p_source_stamp_id IS NULL OR p_credit_lot_id IS NULL OR p_funding_claim_version <= 0
                   OR length(btrim(p_confirmation_event_hash)) = 0 OR jsonb_typeof(p_lines) <> 'array'
                   OR NOT EXISTS (
                       SELECT 1 FROM jsonb_array_elements(p_lines) line
                       WHERE (line->>'credit_lot_id')::uuid = p_credit_lot_id) THEN
                    RAISE EXCEPTION 'confirmed hard coin funding arguments are invalid' USING ERRCODE = '22023';
                END IF;

                SELECT * INTO funding
                FROM public.economy_funding_claims claim
                WHERE claim."SourceStampId" = p_source_stamp_id
                FOR UPDATE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'funding claim was not found' USING ERRCODE = 'P0002';
                END IF;

                IF funding."State" = 2 THEN
                    IF funding."PostingGroupId" IS DISTINCT FROM p_posting_id
                       OR funding."RootCreditLotId" IS DISTINCT FROM p_credit_lot_id THEN
                        RAISE EXCEPTION 'confirmed funding claim is bound to a different mint' USING ERRCODE = '23505';
                    END IF;
                    SELECT pg."Id", entry."Sequence", entry."Hash", true
                    INTO posting_id, journal_sequence, journal_hash, duplicate
                    FROM public.economy_posting_groups pg
                    JOIN public.economy_journal_entries entry ON entry."PostingGroupId" = pg."Id"
                    WHERE pg."Id" = p_posting_id;
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'confirmed funding claim has no journal receipt' USING ERRCODE = '23514';
                    END IF;
                    RETURN NEXT;
                    RETURN;
                END IF;

                IF funding."State" <> 1 OR funding."Version" <> p_funding_claim_version THEN
                    RAISE EXCEPTION 'funding claim is stale or not observable' USING ERRCODE = '40001';
                END IF;

                SELECT * INTO source
                FROM public.economy_source_stamps stamp
                WHERE stamp."Id" = p_source_stamp_id
                FOR UPDATE;
                IF NOT FOUND
                   OR source."State" <> 1
                   OR source."EvidenceHash" <> p_source_evidence_hash
                   OR source."ActorId" <> p_actor_id
                   OR source."TenantId" <> p_tenant_id
                   OR source."PolicyVersion" <> p_policy_version
                   OR p_requested_at < source."ObservedAt" THEN
                    RAISE EXCEPTION 'funding source is stale or does not match the confirmation' USING ERRCODE = '23514';
                END IF;
                IF funding."AuthoritativeUsdMinorUnits" <> (p_lines->0->>'amount_units')::bigint THEN
                    RAISE EXCEPTION 'funding amount does not match the posting' USING ERRCODE = '23514';
                END IF;

                INSERT INTO public.economy_chain_head ("Id", "Sequence", "Hash", "UpdatedAt")
                VALUES (1, 0, repeat('0', 64), p_requested_at)
                ON CONFLICT ("Id") DO NOTHING;
                SELECT "Sequence" + 1 INTO next_sequence
                FROM public.economy_chain_head
                WHERE "Id" = 1
                FOR UPDATE;

                SET CONSTRAINTS public."FK_economy_funding_claims_economy_posting_groups_PostingGroupId" DEFERRED;

                INSERT INTO public.economy_credit_lots (
                    "Id", "WalletId", "RootSourceStampId", "Currency", "AmountUnits", "Provenance", "CreditedAt",
                    "ConfirmedAt", "OriginalMaturesAt", "CashOutEligible", "JournalSequence", "State", "ReversalEpoch")
                VALUES (
                    p_credit_lot_id, funding."WalletId", p_source_stamp_id, 1, funding."AuthoritativeUsdMinorUnits", 1,
                    p_requested_at, p_requested_at, p_requested_at, false, next_sequence, 1, 0);

                INSERT INTO public.economy_root_reversal_states (
                    "RootSourceStampId", "Epoch", "CumulativeProviderUnits", "ReversedUnits", "State", "TargetedRanges", "UpdatedAt")
                VALUES (p_source_stamp_id, 0, 0, 0, 'active', '[]'::jsonb, p_requested_at)
                ON CONFLICT ("RootSourceStampId") DO NOTHING;

                UPDATE public.economy_funding_claims
                SET "State" = 2,
                    "ConfirmedAt" = p_requested_at,
                    "StateChangedAt" = p_requested_at,
                    "PostingGroupId" = p_posting_id,
                    "RootCreditLotId" = p_credit_lot_id,
                    "Version" = "Version" + 1
                WHERE "SourceStampId" = p_source_stamp_id
                  AND "State" = 1
                  AND "Version" = p_funding_claim_version;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'funding claim became stale before mint posting' USING ERRCODE = '40001';
                END IF;

                SELECT * INTO receipt
                FROM economy_private.post_registered_posting_v1(
                    p_capability_id, p_actor_id, p_tenant_id, p_posting_id, p_idempotency_key, p_template_kind,
                    p_template_version, p_authority, p_policy_version, p_reserve_version, p_risk_decision_id,
                    p_risk_operation_fingerprint, p_expected_counter_version, p_source_stamp_id,
                    p_source_evidence_hash, p_requested_at, p_lines, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb,
                    p_dispatch_snapshot_hash);
                IF receipt.duplicate THEN
                    RAISE EXCEPTION 'unexpected duplicate before funding confirmation completed' USING ERRCODE = '40001';
                END IF;

                SET CONSTRAINTS public."FK_economy_funding_claims_economy_posting_groups_PostingGroupId" IMMEDIATE;

                IF NOT EXISTS (
                    SELECT 1
                    FROM public.economy_funding_claims funding_claim
                    WHERE funding_claim."SourceStampId" = p_source_stamp_id
                      AND funding_claim."State" = 2
                      AND funding_claim."PostingGroupId" = p_posting_id
                      AND funding_claim."RootCreditLotId" = p_credit_lot_id) THEN
                    RAISE EXCEPTION 'funding claim was not bound to the mint posting' USING ERRCODE = '23514';
                END IF;

                INSERT INTO public.economy_source_stamp_events (
                    "Id", "SourceStampId", "Sequence", "State", "EvidenceHash", "OccurredAt")
                VALUES (gen_random_uuid(), p_source_stamp_id, 2, 2, btrim(p_confirmation_event_hash), p_requested_at);

                INSERT INTO public.economy_fragment_root_ranges (
                    "Id", "RootSourceStampId", "CreditLotId", "EntryAllocationId", "StartInclusive", "EndExclusive", "ReversalEpoch")
                VALUES (
                    gen_random_uuid(), p_source_stamp_id, p_credit_lot_id, NULL, 0,
                    funding."AuthoritativeUsdMinorUnits" * 1000, 0);

                SELECT line."Id" INTO credit_line_id
                FROM public.economy_journal_entries entry
                JOIN public.economy_journal_lines line ON line."JournalEntryId" = entry."Id"
                WHERE entry."PostingGroupId" = p_posting_id AND line."CreditLotId" = p_credit_lot_id;
                IF credit_line_id IS NULL THEN
                    RAISE EXCEPTION 'funding mint has no credit journal line' USING ERRCODE = '23514';
                END IF;

                INSERT INTO public.economy_provider_fact_allocations (
                    "Id", "SourceStampId", "JournalLineId", "Provider", "Environment", "ConnectedAccount", "ProviderObject",
                    "ProviderMonetaryLeg", "Currency", "AllocatedUnits", "CumulativeCreditedUnits", "AuthoritativeUnits")
                VALUES (
                    gen_random_uuid(), p_source_stamp_id, credit_line_id, funding."Provider", funding."Environment",
                    funding."ConnectedAccount", funding."ProviderObject", funding."ProviderMonetaryLeg", 1,
                    funding."AuthoritativeUsdMinorUnits", funding."AuthoritativeUsdMinorUnits", funding."AuthoritativeUsdMinorUnits");

                PERFORM economy_private.rebuild_wallet_projection_v1(funding."WalletId", p_requested_at);

                posting_id := receipt.posting_id;
                journal_sequence := receipt.journal_sequence;
                journal_hash := receipt.journal_hash;
                duplicate := false;
                RETURN NEXT;
            END
            $function$;
            """);

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.post_fifo_transfer_v1(
                p_capability_id uuid,
                p_actor_id uuid,
                p_tenant_id uuid,
                p_posting_id uuid,
                p_idempotency_key text,
                p_policy_version bigint,
                p_reserve_version bigint,
                p_risk_decision_id uuid,
                p_risk_operation_fingerprint text,
                p_expected_counter_version bigint,
                p_source_wallet_id uuid,
                p_destination_wallet_id uuid,
                p_currency integer,
                p_provenance integer,
                p_amount_units bigint,
                p_requested_at timestamptz,
                p_dispatch_snapshot_hash text)
            RETURNS TABLE(posting_id uuid, journal_sequence bigint, journal_hash text, duplicate boolean)
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                existing_operation public.economy_fifo_transfer_operations%ROWTYPE;
                existing_posting record;
                receipt record;
                reservation record;
                parent_lot record;
                account_code integer;
                source_account_id uuid;
                destination_account_id uuid;
                source_line_id uuid;
                destination_line_id uuid;
                child_lot_id uuid;
                allocation_id uuid;
                next_sequence bigint;
                trace_scale bigint;
                request_hash text;
                lines jsonb;
                allocations jsonb := '[]'::jsonb;
                root_ranges jsonb := '[]'::jsonb;
                expected_epochs jsonb := '[]'::jsonb;
            BEGIN
                IF p_capability_id IS NULL OR p_actor_id IS NULL OR p_tenant_id IS NULL OR p_posting_id IS NULL
                   OR p_risk_decision_id IS NULL OR p_source_wallet_id IS NULL OR p_destination_wallet_id IS NULL
                   OR p_source_wallet_id = p_destination_wallet_id OR p_amount_units <= 0 OR p_requested_at IS NULL
                   OR p_policy_version <= 0 OR p_reserve_version <= 0 OR p_expected_counter_version <= 0
                   OR p_currency NOT IN (1, 2) OR p_provenance NOT IN (1, 2, 3, 4, 5, 6, 7)
                   OR length(btrim(p_idempotency_key)) = 0 OR length(btrim(p_risk_operation_fingerprint)) = 0 THEN
                    RAISE EXCEPTION 'FIFO transfer arguments are invalid' USING ERRCODE = '22023';
                END IF;
                IF (p_currency = 1 AND p_provenance NOT IN (1, 2))
                   OR (p_currency = 2 AND p_provenance NOT IN (3, 4, 5, 6, 7)) THEN
                    RAISE EXCEPTION 'FIFO transfer currency and provenance are incompatible' USING ERRCODE = '23514';
                END IF;

                PERFORM pg_advisory_xact_lock(hashtextextended(btrim(p_idempotency_key), 0));
                PERFORM pg_advisory_xact_lock(hashtextextended(p_posting_id::text, 0));
                request_hash := encode(public.digest(convert_to(jsonb_build_object(
                    'postingId', p_posting_id,
                    'idempotencyKey', btrim(p_idempotency_key),
                    'capabilityId', p_capability_id,
                    'actorId', p_actor_id,
                    'tenantId', p_tenant_id,
                    'policyVersion', p_policy_version,
                    'reserveVersion', p_reserve_version,
                    'riskDecisionId', p_risk_decision_id,
                    'riskOperationFingerprint', btrim(p_risk_operation_fingerprint),
                    'expectedCounterVersion', p_expected_counter_version,
                    'sourceWalletId', p_source_wallet_id,
                    'destinationWalletId', p_destination_wallet_id,
                    'currency', p_currency,
                    'provenance', p_provenance,
                    'amountUnits', p_amount_units,
                    'requestedAt', p_requested_at,
                    'dispatchSnapshotHash', p_dispatch_snapshot_hash)::text, 'UTF8'), 'sha256'), 'hex');

                SELECT * INTO existing_operation
                FROM public.economy_fifo_transfer_operations operation
                WHERE operation."IdempotencyKey" = btrim(p_idempotency_key)
                FOR UPDATE;
                IF FOUND THEN
                    IF existing_operation."Id" <> p_posting_id
                       OR existing_operation."RequestHash" <> request_hash THEN
                        RAISE EXCEPTION 'FIFO transfer idempotency key is bound to another request' USING ERRCODE = '23505';
                    END IF;
                    SELECT entry."Sequence", entry."Hash"
                    INTO existing_posting
                    FROM public.economy_journal_entries entry
                    WHERE entry."PostingGroupId" = p_posting_id;
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'FIFO transfer operation has no immutable posting' USING ERRCODE = '23514';
                    END IF;
                    posting_id := p_posting_id;
                    journal_sequence := existing_posting."Sequence";
                    journal_hash := existing_posting."Hash";
                    duplicate := true;
                    RETURN NEXT;
                    RETURN;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM public.economy_posting_groups posting
                    WHERE posting."IdempotencyKey" = btrim(p_idempotency_key) OR posting."Id" = p_posting_id
                ) THEN
                    RAISE EXCEPTION 'posting identity is already bound to another economy operation' USING ERRCODE = '23505';
                END IF;

                PERFORM 1
                FROM public.economy_wallets wallet
                WHERE wallet."Id" = p_destination_wallet_id AND wallet."State" = 1
                FOR SHARE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'FIFO transfer destination wallet is absent or inactive' USING ERRCODE = '23503';
                END IF;
                PERFORM 1
                FROM public.economy_risk_decisions decision
                WHERE decision."Id" = p_risk_decision_id
                  AND decision."TemplateKind" = 4
                  AND decision."SourceWalletId" = p_source_wallet_id
                  AND decision."DestinationWalletId" = p_destination_wallet_id
                  AND decision."Currency" = p_currency
                  AND decision."AmountUnits" = p_amount_units
                FOR SHARE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'FIFO transfer risk decision does not bind the requested wallet movement' USING ERRCODE = '42501';
                END IF;

                account_code := CASE p_provenance WHEN 1 THEN 2 WHEN 2 THEN 3 ELSE 4 END;
                SELECT account."Id" INTO source_account_id
                FROM public.economy_accounts account
                WHERE account."WalletId" = p_source_wallet_id
                  AND account."Code" = account_code
                  AND account."Currency" = p_currency
                  AND account."Provenance" = p_provenance;
                SELECT account."Id" INTO destination_account_id
                FROM public.economy_accounts account
                WHERE account."WalletId" = p_destination_wallet_id
                  AND account."Code" = account_code
                  AND account."Currency" = p_currency
                  AND account."Provenance" = p_provenance;
                IF source_account_id IS NULL OR destination_account_id IS NULL THEN
                    RAISE EXCEPTION 'FIFO transfer account partitions are not provisioned' USING ERRCODE = '23503';
                END IF;

                source_line_id := gen_random_uuid();
                destination_line_id := gen_random_uuid();
                lines := jsonb_build_array(
                    jsonb_build_object(
                        'id', source_line_id, 'account_id', source_account_id, 'account_code', account_code,
                        'wallet_id', p_source_wallet_id, 'credit_lot_id', NULL, 'side', 1, 'currency', p_currency,
                        'amount_units', p_amount_units, 'provenance', p_provenance),
                    jsonb_build_object(
                        'id', destination_line_id, 'account_id', destination_account_id, 'account_code', account_code,
                        'wallet_id', p_destination_wallet_id, 'credit_lot_id', NULL, 'side', 2, 'currency', p_currency,
                        'amount_units', p_amount_units, 'provenance', p_provenance));

                INSERT INTO public.economy_chain_head ("Id", "Sequence", "Hash", "UpdatedAt")
                VALUES (1, 0, repeat('0', 64), p_requested_at)
                ON CONFLICT ("Id") DO NOTHING;
                SELECT head."Sequence" + 1 INTO next_sequence
                FROM public.economy_chain_head head WHERE head."Id" = 1 FOR UPDATE;

                trace_scale := CASE p_currency WHEN 1 THEN 1000 ELSE 1 END;
                FOR reservation IN
                    SELECT * FROM economy_private.reserve_fifo_fragments_v1(
                        p_posting_id, p_source_wallet_id, p_currency, p_provenance, p_amount_units, 4, p_requested_at)
                LOOP
                    SELECT * INTO parent_lot
                    FROM public.economy_credit_lots lot
                    WHERE lot."Id" = reservation.parent_lot_id
                    FOR SHARE;
                    IF NOT FOUND
                       OR parent_lot."WalletId" <> p_source_wallet_id
                       OR parent_lot."Currency" <> p_currency
                       OR parent_lot."Provenance" <> p_provenance
                       OR parent_lot."State" <> 1 THEN
                        RAISE EXCEPTION 'FIFO reservation parent lot is no longer eligible' USING ERRCODE = '40001';
                    END IF;

                    child_lot_id := gen_random_uuid();
                    allocation_id := gen_random_uuid();
                    INSERT INTO public.economy_credit_lots (
                        "Id", "WalletId", "RootSourceStampId", "Currency", "AmountUnits", "Provenance",
                        "CreditedAt", "ConfirmedAt", "OriginalMaturesAt", "CashOutEligible", "JournalSequence",
                        "State", "ReversalEpoch")
                    VALUES (
                        child_lot_id, p_destination_wallet_id, reservation.root_source_stamp_id, p_currency,
                        reservation.amount_units, p_provenance, p_requested_at, parent_lot."ConfirmedAt",
                        parent_lot."OriginalMaturesAt", parent_lot."CashOutEligible", next_sequence, 1,
                        reservation.reversal_epoch);
                    INSERT INTO public.economy_lot_lineage_edges (
                        "Id", "ParentLotId", "ChildLotId", "Currency", "AmountUnits")
                    VALUES (
                        gen_random_uuid(), reservation.parent_lot_id, child_lot_id, p_currency, reservation.amount_units);
                    INSERT INTO public.economy_fragment_root_ranges (
                        "Id", "RootSourceStampId", "CreditLotId", "EntryAllocationId", "StartInclusive", "EndExclusive", "ReversalEpoch")
                    VALUES (
                        gen_random_uuid(), reservation.root_source_stamp_id, child_lot_id, NULL,
                        reservation.start_inclusive, reservation.end_exclusive, reservation.reversal_epoch);

                    allocations := allocations || jsonb_build_array(jsonb_build_object(
                        'id', allocation_id, 'journal_line_id', source_line_id,
                        'parent_lot_id', reservation.parent_lot_id, 'amount_units', reservation.amount_units));
                    root_ranges := root_ranges || jsonb_build_array(jsonb_build_object(
                        'id', gen_random_uuid(), 'root_source_stamp_id', reservation.root_source_stamp_id,
                        'credit_lot_id', NULL, 'entry_allocation_id', allocation_id,
                        'start_inclusive', reservation.start_inclusive, 'end_exclusive', reservation.end_exclusive,
                        'reversal_epoch', reservation.reversal_epoch));
                    IF NOT EXISTS (
                        SELECT 1 FROM jsonb_array_elements(expected_epochs) item
                        WHERE (item->>'root_source_stamp_id')::uuid = reservation.root_source_stamp_id
                    ) THEN
                        expected_epochs := expected_epochs || jsonb_build_array(jsonb_build_object(
                            'root_source_stamp_id', reservation.root_source_stamp_id,
                            'expected_epoch', reservation.reversal_epoch));
                    END IF;
                END LOOP;

                PERFORM economy_private.transition_fifo_fragment_reservations_v1(p_posting_id, 1, 3, p_requested_at);
                SELECT * INTO receipt
                FROM economy_private.post_registered_posting_v1(
                    p_capability_id, p_actor_id, p_tenant_id, p_posting_id, btrim(p_idempotency_key), 4, 1, 2,
                    p_policy_version, p_reserve_version, p_risk_decision_id, btrim(p_risk_operation_fingerprint),
                    p_expected_counter_version, NULL, NULL, p_requested_at, lines, allocations, root_ranges,
                    expected_epochs, p_dispatch_snapshot_hash);
                IF receipt.duplicate THEN
                    RAISE EXCEPTION 'FIFO transfer reached an unexpected duplicate posting' USING ERRCODE = '40001';
                END IF;

                INSERT INTO public.economy_fifo_transfer_operations (
                    "Id", "IdempotencyKey", "RequestHash", "SourceWalletId", "DestinationWalletId", "Currency",
                    "Provenance", "AmountUnits", "CreatedAt")
                VALUES (
                    p_posting_id, btrim(p_idempotency_key), request_hash, p_source_wallet_id, p_destination_wallet_id,
                    p_currency, p_provenance, p_amount_units, p_requested_at);

                PERFORM economy_private.rebuild_wallet_projection_v1(p_source_wallet_id, p_requested_at);
                PERFORM economy_private.rebuild_wallet_projection_v1(p_destination_wallet_id, p_requested_at);

                posting_id := receipt.posting_id;
                journal_sequence := receipt.journal_sequence;
                journal_hash := receipt.journal_hash;
                duplicate := false;
                RETURN NEXT;
            END
            $function$;
            """);
    }
}