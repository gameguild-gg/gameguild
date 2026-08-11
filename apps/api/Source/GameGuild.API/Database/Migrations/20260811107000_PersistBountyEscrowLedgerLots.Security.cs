using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

public partial class PersistBountyEscrowLedgerLots
{
    private static void InstallBountyEscrowLedgerLotsWriter(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.validate_bounty_escrow_posting_lines_v1(p_lines jsonb)
            RETURNS boolean
            LANGUAGE plpgsql
            IMMUTABLE
            AS $function$
            DECLARE
                line_count integer;
                currency integer;
                debit_total bigint;
                credit_total bigint;
            BEGIN
                IF p_lines IS NULL OR jsonb_typeof(p_lines) <> 'array' THEN
                    RETURN false;
                END IF;

                line_count := jsonb_array_length(p_lines);
                IF line_count < 2 THEN
                    RETURN false;
                END IF;

                SELECT (p_lines->0->>'currency')::integer INTO currency;
                IF currency NOT IN (1, 2) THEN
                    RETURN false;
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM jsonb_array_elements(p_lines) WITH ORDINALITY AS item(line, ordinal)
                    WHERE NULLIF(line->>'id', '')::uuid IS NULL
                       OR NULLIF(line->>'account_id', '')::uuid IS NULL
                       OR (line->>'currency')::integer <> currency
                       OR (line->>'amount_units')::bigint <= 0
                       OR (ordinal < line_count AND (line->>'side')::integer <> 1)
                       OR (ordinal = line_count AND (line->>'side')::integer <> 2)) THEN
                    RETURN false;
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM jsonb_array_elements(p_lines) WITH ORDINALITY AS item(line, ordinal)
                    WHERE ordinal = line_count
                      AND ((line->>'wallet_id') IS NOT NULL AND NULLIF(line->>'wallet_id', '') IS NOT NULL
                           OR (line->>'credit_lot_id') IS NOT NULL AND NULLIF(line->>'credit_lot_id', '') IS NOT NULL
                           OR NULLIF(line->>'provenance', '') IS NOT NULL
                           OR (currency = 1 AND (line->>'account_code')::integer <> 9)
                           OR (currency = 2 AND (line->>'account_code')::integer <> 10))) THEN
                    RETURN false;
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM jsonb_array_elements(p_lines) WITH ORDINALITY AS item(line, ordinal)
                    WHERE ordinal < line_count
                      AND (NULLIF(line->>'wallet_id', '')::uuid IS NULL
                           OR NULLIF(line->>'credit_lot_id', '')::uuid IS NOT NULL
                           OR NULLIF(line->>'provenance', '')::integer IS NULL
                           OR (currency = 1 AND NOT (
                               ((line->>'provenance')::integer = 2 AND (line->>'account_code')::integer = 3)
                               OR ((line->>'provenance')::integer <> 2 AND (line->>'account_code')::integer = 2)))
                           OR (currency = 2 AND NOT (
                               (line->>'provenance')::integer BETWEEN 3 AND 7
                               AND (line->>'account_code')::integer = 4))) THEN
                    RETURN false;
                END IF;

                SELECT COALESCE(sum((line->>'amount_units')::bigint) FILTER (WHERE (line->>'side')::integer = 1), 0),
                       COALESCE(sum((line->>'amount_units')::bigint) FILTER (WHERE (line->>'side')::integer = 2), 0)
                INTO debit_total, credit_total
                FROM jsonb_array_elements(p_lines) line;

                RETURN debit_total > 0 AND debit_total = credit_total;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.post_registered_posting_v2(
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
                p_allocations jsonb,
                p_root_ranges jsonb,
                p_expected_reversal_epochs jsonb,
                p_dispatch_snapshot_hash text)
            RETURNS TABLE(posting_id uuid, journal_sequence bigint, journal_hash text, duplicate boolean)
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                risk_record record;
                chain_record record;
                canonical text;
                request_hash text;
                existing_request_hash text;
                outbox_payload text;
                risk_amount bigint;
                posting_currency integer;
            BEGIN
                IF p_template_kind <> 22
                   OR p_posting_id IS NULL OR p_actor_id IS NULL OR p_tenant_id IS NULL
                   OR p_idempotency_key IS NULL OR length(btrim(p_idempotency_key)) = 0
                   OR p_template_version <> 1 OR p_authority <> 2
                   OR p_policy_version <= 0 OR p_reserve_version <= 0
                   OR p_expected_counter_version <= 0
                   OR p_source_stamp_id IS NOT NULL OR p_source_evidence_hash IS NOT NULL
                   OR jsonb_typeof(p_lines) <> 'array'
                   OR jsonb_typeof(p_allocations) <> 'array'
                   OR jsonb_typeof(p_root_ranges) <> 'array'
                   OR jsonb_typeof(p_expected_reversal_epochs) <> 'array' THEN
                    RAISE EXCEPTION 'invalid bounty escrow posting arguments' USING ERRCODE = '22023';
                END IF;

                IF NOT economy_private.validate_bounty_escrow_posting_lines_v1(p_lines) THEN
                    RAISE EXCEPTION 'bounty escrow lines do not match the registered template' USING ERRCODE = '23514';
                END IF;

                SELECT (p_lines->0->>'currency')::integer,
                       sum((line->>'amount_units')::bigint)
                INTO posting_currency, risk_amount
                FROM jsonb_array_elements(p_lines) line
                WHERE (line->>'side')::integer = 1
                GROUP BY (p_lines->0->>'currency')::integer;

                request_hash := encode(public.digest(convert_to(jsonb_build_object(
                    'capabilityId', p_capability_id,
                    'actorId', p_actor_id,
                    'tenantId', p_tenant_id,
                    'postingId', p_posting_id,
                    'idempotencyKey', p_idempotency_key,
                    'templateKind', p_template_kind,
                    'templateVersion', p_template_version,
                    'authority', p_authority,
                    'policyVersion', p_policy_version,
                    'reserveVersion', p_reserve_version,
                    'riskDecisionId', p_risk_decision_id,
                    'riskOperationFingerprint', p_risk_operation_fingerprint,
                    'counterVersion', p_expected_counter_version,
                    'sourceStampId', p_source_stamp_id,
                    'sourceEvidenceHash', p_source_evidence_hash,
                    'requestedAt', p_requested_at,
                    'lines', p_lines,
                    'allocations', p_allocations,
                    'rootRanges', p_root_ranges,
                    'reversalEpochs', p_expected_reversal_epochs,
                    'dispatchSnapshotHash', p_dispatch_snapshot_hash)::text, 'UTF8'), 'sha256'), 'hex');

                SELECT pg."Id", je."Sequence", je."Hash", idempotency."RequestHash"
                INTO posting_id, journal_sequence, journal_hash, existing_request_hash
                FROM public.economy_posting_groups pg
                JOIN public.economy_journal_entries je ON je."PostingGroupId" = pg."Id"
                JOIN public.economy_idempotency_records idempotency ON idempotency."PostingGroupId" = pg."Id"
                WHERE pg."IdempotencyKey" = p_idempotency_key;
                IF FOUND THEN
                    IF posting_id <> p_posting_id OR existing_request_hash <> request_hash THEN
                        RAISE EXCEPTION 'idempotency key is bound to another request' USING ERRCODE = '23505';
                    END IF;
                    duplicate := true;
                    RETURN NEXT;
                    RETURN;
                END IF;

                PERFORM 1
                FROM public.economy_registered_capabilities capability
                WHERE capability."Id" = p_capability_id
                  AND capability."IsEnabled"
                  AND capability."RevokedAt" IS NULL
                  AND capability."AllowedTemplateKinds" @> jsonb_build_array(p_template_kind)
                FOR SHARE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'caller capability is absent, disabled, or unauthorized' USING ERRCODE = '42501';
                END IF;

                SELECT * INTO risk_record
                FROM public.economy_risk_decisions decision
                WHERE decision."Id" = p_risk_decision_id
                FOR UPDATE;
                IF NOT FOUND
                   OR risk_record."Outcome" <> 1
                   OR risk_record."OperationFingerprint" <> p_risk_operation_fingerprint
                   OR risk_record."TemplateKind" <> p_template_kind
                   OR risk_record."PolicyVersion" <> p_policy_version
                   OR risk_record."ReserveVersion" <> p_reserve_version
                   OR risk_record."CounterVersion" <> p_expected_counter_version
                   OR risk_record."Currency" <> posting_currency
                   OR risk_record."AmountUnits" <> risk_amount
                   OR risk_record."IssuedAt" > p_requested_at
                   OR risk_record."ExpiresAt" <= p_requested_at THEN
                    RAISE EXCEPTION 'risk decision is missing, stale, denied, or operation-mismatched' USING ERRCODE = '42501';
                END IF;

                IF EXISTS (
                    SELECT 1 FROM public.economy_risk_decision_consumptions
                    WHERE "RiskDecisionId" = p_risk_decision_id
                ) THEN
                    RAISE EXCEPTION 'risk decision has already been consumed' USING ERRCODE = '23505';
                END IF;
                IF NOT EXISTS (
                    SELECT 1
                    FROM public.economy_risk_counter_reservations reservation
                    JOIN public.economy_risk_counters counter ON counter."Id" = reservation."RiskCounterId"
                    WHERE reservation."RiskDecisionId" = p_risk_decision_id
                      AND reservation."AmountUnits" = risk_amount
                      AND counter."CounterVersion" = p_expected_counter_version
                ) THEN
                    RAISE EXCEPTION 'risk decision has no persisted aggregate-counter reservation' USING ERRCODE = '42501';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM jsonb_array_elements(p_lines) line
                    LEFT JOIN public.economy_accounts account ON account."Id" = (line->>'account_id')::uuid
                    WHERE account."Id" IS NULL
                       OR account."Code" <> (line->>'account_code')::integer
                       OR account."Currency" <> (line->>'currency')::integer
                       OR account."WalletId" IS DISTINCT FROM NULLIF(line->>'wallet_id', '')::uuid
                       OR account."Provenance" IS DISTINCT FROM NULLIF(line->>'provenance', '')::integer
                ) THEN
                    RAISE EXCEPTION 'bounty escrow line does not match its registered account partition' USING ERRCODE = '23514';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM jsonb_array_elements(p_expected_reversal_epochs) expected
                    LEFT JOIN public.economy_root_reversal_states reversal
                        ON reversal."RootSourceStampId" = (expected->>'root_source_stamp_id')::uuid
                    WHERE COALESCE(reversal."Epoch", 0) <> (expected->>'expected_epoch')::bigint
                ) OR EXISTS (
                    SELECT 1
                    FROM jsonb_array_elements(p_root_ranges) root_range
                    WHERE NOT EXISTS (
                        SELECT 1 FROM jsonb_array_elements(p_expected_reversal_epochs) expected
                        WHERE expected->>'root_source_stamp_id' = root_range->>'root_source_stamp_id'
                          AND (expected->>'expected_epoch')::bigint = (root_range->>'reversal_epoch')::bigint)
                ) THEN
                    RAISE EXCEPTION 'root range uses a stale or absent reversal epoch fence' USING ERRCODE = '23514';
                END IF;

                INSERT INTO public.economy_chain_head ("Id", "Sequence", "Hash", "UpdatedAt")
                VALUES (1, 0, repeat('0', 64), p_requested_at)
                ON CONFLICT ("Id") DO NOTHING;
                SELECT "Sequence", "Hash" INTO chain_record
                FROM public.economy_chain_head WHERE "Id" = 1 FOR UPDATE;

                journal_sequence := chain_record."Sequence" + 1;
                canonical := concat_ws('|', chain_record."Hash", p_posting_id::text,
                    journal_sequence::text, request_hash);
                journal_hash := encode(public.digest(convert_to(canonical, 'UTF8'), 'sha256'), 'hex');

                INSERT INTO public.economy_posting_groups (
                    "Id", "IdempotencyKey", "TemplateKind", "TemplateVersion", "Authority", "Status",
                    "CapabilityId", "ActorId", "TenantId", "RiskDecisionId", "PolicyVersion", "ReserveVersion",
                    "SourceStampId", "RecordedAt")
                VALUES (
                    p_posting_id, p_idempotency_key, p_template_kind, p_template_version, p_authority, 1,
                    p_capability_id, p_actor_id, p_tenant_id, p_risk_decision_id, p_policy_version,
                    p_reserve_version, NULL, p_requested_at);

                INSERT INTO public.economy_journal_entries (
                    "Id", "PostingGroupId", "Sequence", "PreviousHash", "Hash", "RecordedAt")
                VALUES (gen_random_uuid(), p_posting_id, journal_sequence, chain_record."Hash", journal_hash, p_requested_at);

                WITH entry AS (
                    SELECT "Id" FROM public.economy_journal_entries WHERE "PostingGroupId" = p_posting_id
                )
                INSERT INTO public.economy_journal_lines (
                    "Id", "JournalEntryId", "AccountId", "WalletId", "CreditLotId", "Sequence",
                    "Side", "Currency", "AmountUnits", "Provenance")
                SELECT
                    (line->>'id')::uuid,
                    entry."Id",
                    (line->>'account_id')::uuid,
                    NULLIF(line->>'wallet_id', '')::uuid,
                    NULLIF(line->>'credit_lot_id', '')::uuid,
                    ordinal::integer,
                    (line->>'side')::integer,
                    (line->>'currency')::integer,
                    (line->>'amount_units')::bigint,
                    NULLIF(line->>'provenance', '')::integer
                FROM jsonb_array_elements(p_lines) WITH ORDINALITY AS item(line, ordinal)
                CROSS JOIN entry;

                INSERT INTO public.economy_entry_allocations (
                    "Id", "JournalLineId", "ParentLotId", "AmountUnits")
                SELECT
                    (allocation->>'id')::uuid,
                    (allocation->>'journal_line_id')::uuid,
                    (allocation->>'parent_lot_id')::uuid,
                    (allocation->>'amount_units')::bigint
                FROM jsonb_array_elements(p_allocations) allocation;

                INSERT INTO public.economy_fragment_root_ranges (
                    "Id", "RootSourceStampId", "CreditLotId", "EntryAllocationId",
                    "StartInclusive", "EndExclusive", "ReversalEpoch")
                SELECT
                    (root_range->>'id')::uuid,
                    (root_range->>'root_source_stamp_id')::uuid,
                    NULLIF(root_range->>'credit_lot_id', '')::uuid,
                    NULLIF(root_range->>'entry_allocation_id', '')::uuid,
                    (root_range->>'start_inclusive')::bigint,
                    (root_range->>'end_exclusive')::bigint,
                    (root_range->>'reversal_epoch')::bigint
                FROM jsonb_array_elements(p_root_ranges) root_range;

                INSERT INTO public.economy_risk_decision_consumptions (
                    "Id", "RiskDecisionId", "PostingGroupId", "OperationFingerprint", "ConsumedAt")
                VALUES (gen_random_uuid(), p_risk_decision_id, p_posting_id, p_risk_operation_fingerprint, p_requested_at);

                INSERT INTO public.economy_idempotency_records (
                    "Id", "Key", "RequestHash", "PostingGroupId", "CreatedAt")
                VALUES (gen_random_uuid(), p_idempotency_key, request_hash, p_posting_id, p_requested_at);

                UPDATE public.economy_chain_head
                SET "Sequence" = journal_sequence, "Hash" = journal_hash, "UpdatedAt" = p_requested_at
                WHERE "Id" = 1;

                INSERT INTO public.economy_risk_audit_evidence (
                    "Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt")
                VALUES (
                    gen_random_uuid(), p_risk_decision_id, 'posting-authorized', p_risk_operation_fingerprint,
                    journal_hash, jsonb_build_object('postingId', p_posting_id, 'sequence', journal_sequence), p_requested_at);

                outbox_payload := json_build_object(
                    'PostingId', p_posting_id,
                    'Hash', journal_hash,
                    'RecordedAt', p_requested_at,
                    'JournalLineIds', (
                        SELECT json_agg(line."Id" ORDER BY line."Sequence")
                        FROM public.economy_journal_entries entry
                        JOIN public.economy_journal_lines line ON line."JournalEntryId" = entry."Id"
                        WHERE entry."PostingGroupId" = p_posting_id))::text;
                INSERT INTO public.economy_outbox_messages (
                    "Id", "PostingGroupId", "Type", "Payload", "PayloadHash", "OccurredAt")
                VALUES (
                    gen_random_uuid(), p_posting_id, 'economy.posting.accepted.v1', outbox_payload,
                    encode(public.digest(convert_to(outbox_payload, 'UTF8'), 'sha256'), 'hex'), p_requested_at);
                posting_id := p_posting_id;
                duplicate := false;
                RETURN NEXT;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.create_bounty_escrow_v3(
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
                p_fragments jsonb,
                p_posting_id uuid)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                fragment record;
                parent_lot public.economy_credit_lots%ROWTYPE;
                posting_record record;
                actual_ranges jsonb;
                allocated_units bigint;
                child_lot_id uuid;
                root_range record;
            BEGIN
                IF p_posting_id IS NULL THEN
                    RAISE EXCEPTION 'bounty escrow posting is required' USING ERRCODE = '22023';
                END IF;

                SELECT posting."Id", posting."ActorId", posting."TemplateKind", posting."Authority", entry."Sequence"
                INTO posting_record
                FROM public.economy_posting_groups posting
                JOIN public.economy_journal_entries entry ON entry."PostingGroupId" = posting."Id"
                WHERE posting."Id" = p_posting_id
                  AND posting."IdempotencyKey" = replace(p_bounty_id::text, '-', '') || ':escrow'
                FOR SHARE;
                IF NOT FOUND OR posting_record."ActorId" <> p_poster_id
                   OR posting_record."TemplateKind" <> 22 OR posting_record."Authority" <> 2 THEN
                    RAISE EXCEPTION 'bounty escrow posting does not bind the poster and template' USING ERRCODE = '23514';
                END IF;

                PERFORM economy_private.create_bounty_escrow_v2(
                    p_bounty_id, p_poster_id, p_poster_wallet_id, p_escrow_wallet_id, p_currency,
                    p_amount_units, p_reclaim_fee_ppm, p_requires_prerequisite, p_minimum_reputation,
                    p_requires_instructor_verification, p_idempotency_key, p_request_hash, p_posted_at,
                    p_expires_at, p_fragments);

                IF EXISTS (
                    SELECT 1
                    FROM public.economy_entry_allocations allocation
                    JOIN public.economy_journal_lines line ON line."Id" = allocation."JournalLineId"
                    JOIN public.economy_journal_entries entry ON entry."Id" = line."JournalEntryId"
                    WHERE entry."PostingGroupId" = p_posting_id
                      AND line."Side" = 1
                      AND NOT EXISTS (
                          SELECT 1 FROM public.economy_bounty_escrow_fragments fragment
                          WHERE fragment."BountyId" = p_bounty_id
                            AND fragment."ParentLotId" = allocation."ParentLotId")) THEN
                    RAISE EXCEPTION 'bounty escrow posting contains an unbound parent allocation' USING ERRCODE = '23514';
                END IF;

                FOR fragment IN
                    SELECT *
                    FROM public.economy_bounty_escrow_fragments
                    WHERE "BountyId" = p_bounty_id
                    ORDER BY "ParentLotId"
                    FOR UPDATE
                LOOP
                    SELECT COALESCE(sum(allocation."AmountUnits"), 0),
                           COALESCE(jsonb_agg(jsonb_build_object(
                               'RootSourceStampId', range."RootSourceStampId",
                               'StartInclusive', range."StartInclusive",
                               'EndExclusive', range."EndExclusive",
                               'ReversalEpoch', range."ReversalEpoch")
                               ORDER BY range."RootSourceStampId", range."StartInclusive", range."EndExclusive", range."ReversalEpoch"), '[]'::jsonb)
                    INTO allocated_units, actual_ranges
                    FROM public.economy_entry_allocations allocation
                    JOIN public.economy_journal_lines line ON line."Id" = allocation."JournalLineId"
                    JOIN public.economy_journal_entries entry ON entry."Id" = line."JournalEntryId"
                    LEFT JOIN public.economy_fragment_root_ranges range ON range."EntryAllocationId" = allocation."Id"
                    WHERE entry."PostingGroupId" = p_posting_id
                      AND line."Side" = 1
                      AND allocation."ParentLotId" = fragment."ParentLotId";

                    IF allocated_units <> fragment."AmountUnits"
                       OR actual_ranges <> fragment."SelectedRootRanges" THEN
                        RAISE EXCEPTION 'bounty escrow posting allocations do not match reserved FIFO fragments' USING ERRCODE = '23514';
                    END IF;

                    IF fragment."EscrowLotId" IS NOT NULL THEN
                        CONTINUE;
                    END IF;

                    SELECT * INTO parent_lot
                    FROM public.economy_credit_lots lot
                    WHERE lot."Id" = fragment."ParentLotId"
                    FOR SHARE;
                    IF NOT FOUND OR parent_lot."WalletId" <> p_poster_wallet_id
                       OR parent_lot."Currency" <> fragment."Currency"
                       OR parent_lot."Provenance" <> fragment."Provenance" THEN
                        RAISE EXCEPTION 'bounty escrow parent lot is no longer valid' USING ERRCODE = '23514';
                    END IF;

                    child_lot_id := gen_random_uuid();
                    INSERT INTO public.economy_credit_lots (
                        "Id", "WalletId", "RootSourceStampId", "Currency", "AmountUnits", "Provenance",
                        "CreditedAt", "ConfirmedAt", "OriginalMaturesAt", "CashOutEligible", "JournalSequence",
                        "State", "ReversalEpoch")
                    VALUES (
                        child_lot_id, p_escrow_wallet_id,
                        (actual_ranges->0->>'RootSourceStampId')::uuid,
                        fragment."Currency", fragment."AmountUnits", fragment."Provenance", p_posted_at,
                        parent_lot."ConfirmedAt", parent_lot."OriginalMaturesAt", parent_lot."CashOutEligible",
                        posting_record."Sequence", 1, parent_lot."ReversalEpoch");

                    INSERT INTO public.economy_lot_lineage_edges (
                        "Id", "ParentLotId", "ChildLotId", "Currency", "AmountUnits")
                    VALUES (
                        gen_random_uuid(), fragment."ParentLotId", child_lot_id,
                        fragment."Currency", fragment."AmountUnits");

                    FOR root_range IN
                        SELECT *
                        FROM jsonb_to_recordset(actual_ranges) AS range(
                            "RootSourceStampId" uuid,
                            "StartInclusive" bigint,
                            "EndExclusive" bigint,
                            "ReversalEpoch" bigint)
                    LOOP
                        INSERT INTO public.economy_fragment_root_ranges (
                            "Id", "RootSourceStampId", "CreditLotId", "EntryAllocationId",
                            "StartInclusive", "EndExclusive", "ReversalEpoch")
                        VALUES (
                            gen_random_uuid(), root_range."RootSourceStampId", child_lot_id, NULL,
                            root_range."StartInclusive", root_range."EndExclusive", root_range."ReversalEpoch");
                    END LOOP;

                    UPDATE public.economy_bounty_escrow_fragments
                    SET "EscrowLotId" = child_lot_id
                    WHERE "Id" = fragment."Id";
                END LOOP;

                IF EXISTS (
                    SELECT 1 FROM public.economy_bounty_escrow_fragments
                    WHERE "BountyId" = p_bounty_id AND "EscrowLotId" IS NULL) THEN
                    RAISE EXCEPTION 'bounty escrow ledger lot creation is incomplete' USING ERRCODE = '23514';
                END IF;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_bounty_escrow_fragments_v3(p_bounty_id uuid)
            RETURNS TABLE(
                "ParentLotId" uuid,
                "EscrowLotId" uuid,
                "Currency" integer,
                "Provenance" integer,
                "AmountUnits" bigint,
                "TraceUnitsPerCoinUnit" bigint,
                "SelectedRootRanges" jsonb)
            LANGUAGE sql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT fragment."ParentLotId", fragment."EscrowLotId", fragment."Currency", fragment."Provenance",
                       fragment."AmountUnits", fragment."TraceUnitsPerCoinUnit", fragment."SelectedRootRanges"
                FROM public.economy_bounty_escrow_fragments fragment
                WHERE fragment."BountyId" = p_bounty_id
                ORDER BY fragment."ParentLotId"
            $function$;

            ALTER FUNCTION economy_private.validate_bounty_escrow_posting_lines_v1(jsonb)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.post_registered_posting_v2(
                uuid,uuid,uuid,uuid,text,integer,integer,integer,bigint,bigint,uuid,text,bigint,
                uuid,text,timestamptz,jsonb,jsonb,jsonb,jsonb,text)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.create_bounty_escrow_v3(
                uuid,uuid,uuid,uuid,integer,bigint,integer,boolean,integer,boolean,text,text,timestamptz,timestamptz,jsonb,uuid)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_bounty_escrow_fragments_v3(uuid)
                OWNER TO gameguild_economy_procedure_owner;

            REVOKE ALL ON FUNCTION economy_private.validate_bounty_escrow_posting_lines_v1(jsonb) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.post_registered_posting_v2(
                uuid,uuid,uuid,uuid,text,integer,integer,integer,bigint,bigint,uuid,text,bigint,
                uuid,text,timestamptz,jsonb,jsonb,jsonb,jsonb,text) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.create_bounty_escrow_v3(
                uuid,uuid,uuid,uuid,integer,bigint,integer,boolean,integer,boolean,text,text,timestamptz,timestamptz,jsonb,uuid) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_bounty_escrow_fragments_v3(uuid) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.post_registered_posting_v2(
                uuid,uuid,uuid,uuid,text,integer,integer,integer,bigint,bigint,uuid,text,bigint,
                uuid,text,timestamptz,jsonb,jsonb,jsonb,jsonb,text),
                economy_private.create_bounty_escrow_v3(
                    uuid,uuid,uuid,uuid,integer,bigint,integer,boolean,integer,boolean,text,text,timestamptz,timestamptz,jsonb,uuid),
                economy_private.read_bounty_escrow_fragments_v3(uuid)
                TO gameguild_economy_writer;
            """);
    }

    private static void RemoveBountyEscrowLedgerLotsWriter(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.read_bounty_escrow_fragments_v3(uuid);
            DROP FUNCTION IF EXISTS economy_private.create_bounty_escrow_v3(uuid,uuid,uuid,uuid,integer,bigint,integer,boolean,integer,boolean,text,text,timestamptz,timestamptz,jsonb,uuid);
            DROP FUNCTION IF EXISTS economy_private.post_registered_posting_v2(uuid,uuid,uuid,uuid,text,integer,integer,integer,bigint,bigint,uuid,text,bigint,uuid,text,timestamptz,jsonb,jsonb,jsonb,jsonb,text);
            DROP FUNCTION IF EXISTS economy_private.validate_bounty_escrow_posting_lines_v1(jsonb);
            """);
    }
}
