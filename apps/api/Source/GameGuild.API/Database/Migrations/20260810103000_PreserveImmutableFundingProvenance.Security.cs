using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

public partial class PreserveImmutableFundingProvenance
{
    private static void InstallImmutableFundingConfirmation(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(
            """
            ALTER TABLE public.economy_funding_claims
                ALTER CONSTRAINT "FK_economy_funding_claims_economy_posting_groups_PostingGroupId"
                DEFERRABLE INITIALLY IMMEDIATE;

            CREATE OR REPLACE FUNCTION economy_private.validate_provider_fact_allocation_v1()
            RETURNS trigger
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                PERFORM pg_advisory_xact_lock(hashtextextended(NEW."SourceStampId"::text, 0));
                IF NOT EXISTS (
                    SELECT 1
                    FROM public.economy_source_stamps source
                    JOIN public.economy_funding_claims funding
                      ON funding."SourceStampId" = source."Id"
                    WHERE source."Id" = NEW."SourceStampId"
                      AND funding."State" = 2
                      AND funding."ConfirmedAt" IS NOT NULL
                      AND funding."Provider" = NEW."Provider"
                      AND funding."ProviderObject" = NEW."ProviderObject"
                      AND funding."AuthoritativeUsdMinorUnits" = NEW."AuthoritativeUnits"
                      AND source."Provider" = NEW."Provider"
                      AND source."ProviderReference" = concat_ws(chr(31), NEW."Provider", NEW."Environment",
                          NEW."ConnectedAccount", NEW."ProviderObject", NEW."ProviderMonetaryLeg")
                      AND source."AuthoritativeUnits" = NEW."AuthoritativeUnits") THEN
                    RAISE EXCEPTION 'provider fact is not bound to a confirmed authoritative funding claim' USING ERRCODE = '23514';
                END IF;
                RETURN NEW;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.post_registered_posting_v1(
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
            BEGIN
                IF p_posting_id IS NULL OR p_actor_id IS NULL OR p_tenant_id IS NULL
                   OR p_idempotency_key IS NULL OR length(btrim(p_idempotency_key)) = 0
                   OR p_template_version <> 1 OR p_policy_version <= 0 OR p_reserve_version <= 0
                   OR p_expected_counter_version <= 0
                   OR jsonb_typeof(p_lines) <> 'array'
                   OR jsonb_typeof(p_allocations) <> 'array'
                   OR jsonb_typeof(p_root_ranges) <> 'array'
                   OR jsonb_typeof(p_expected_reversal_epochs) <> 'array' THEN
                    RAISE EXCEPTION 'invalid registered posting arguments' USING ERRCODE = '22023';
                END IF;

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

                IF NOT economy_private.validate_posting_lines_v1(p_template_kind, p_lines) THEN
                    RAISE EXCEPTION 'posting lines do not match the registered template' USING ERRCODE = '23514';
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
                   OR risk_record."Currency" <> (p_lines->0->>'currency')::integer
                   OR risk_record."AmountUnits" <> (p_lines->0->>'amount_units')::bigint
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
                      AND reservation."AmountUnits" = risk_record."AmountUnits"
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
                ) THEN
                    RAISE EXCEPTION 'posting line does not match its registered account partition' USING ERRCODE = '23514';
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

                IF p_template_kind IN (1, 2, 3) AND p_source_stamp_id IS NULL THEN
                    RAISE EXCEPTION 'registered template requires source evidence' USING ERRCODE = '23514';
                END IF;
                IF p_source_stamp_id IS NOT NULL THEN
                    PERFORM 1
                    FROM public.economy_source_stamps source
                    WHERE source."Id" = p_source_stamp_id
                      AND source."EvidenceHash" = p_source_evidence_hash
                      AND source."PolicyVersion" = p_policy_version
                      AND (p_template_kind <> 1 OR EXISTS (
                          SELECT 1
                          FROM public.economy_funding_claims funding
                          WHERE funding."SourceStampId" = source."Id"
                            AND funding."State" = 2
                            AND funding."ConfirmedAt" IS NOT NULL
                            AND funding."ConfirmedAt" <= p_requested_at
                            AND funding."PostingGroupId" = p_posting_id
                            AND funding."AuthoritativeUsdMinorUnits" >= (p_lines->0->>'amount_units')::bigint))
                    FOR SHARE;
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'source evidence is absent or mismatched' USING ERRCODE = '23514';
                    END IF;
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
                    p_reserve_version, p_source_stamp_id, p_requested_at);

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

                posting_id := p_posting_id;
                duplicate := false;
                RETURN NEXT;
            END
            $function$;

            ALTER FUNCTION economy_private.post_registered_posting_v1(
                uuid,uuid,uuid,uuid,text,integer,integer,integer,bigint,bigint,uuid,text,bigint,
                uuid,text,timestamptz,jsonb,jsonb,jsonb,jsonb,text)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.post_registered_posting_v1(
                uuid,uuid,uuid,uuid,text,integer,integer,integer,bigint,bigint,uuid,text,bigint,
                uuid,text,timestamptz,jsonb,jsonb,jsonb,jsonb,text) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.post_registered_posting_v1(
                uuid,uuid,uuid,uuid,text,integer,integer,integer,bigint,bigint,uuid,text,bigint,
                uuid,text,timestamptz,jsonb,jsonb,jsonb,jsonb,text) TO gameguild_economy_writer;

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
                outbox_payload text;
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

                outbox_payload := json_build_object(
                    'PostingId', p_posting_id,
                    'Hash', receipt.journal_hash,
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

                posting_id := receipt.posting_id;
                journal_sequence := receipt.journal_sequence;
                journal_hash := receipt.journal_hash;
                duplicate := false;
                RETURN NEXT;
            END
            $function$;

            ALTER FUNCTION economy_private.observe_hard_coin_top_up_v1(
                uuid,uuid,text,text,text,text,text,bigint,text,text,timestamptz,uuid,uuid,bigint)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.confirm_observed_hard_coin_top_up_v1(
                uuid,uuid,uuid,uuid,text,integer,integer,integer,bigint,bigint,uuid,text,bigint,
                uuid,text,timestamptz,jsonb,bigint,uuid,text,text)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.observe_hard_coin_top_up_v1(
                uuid,uuid,text,text,text,text,text,bigint,text,text,timestamptz,uuid,uuid,bigint) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.confirm_observed_hard_coin_top_up_v1(
                uuid,uuid,uuid,uuid,text,integer,integer,integer,bigint,bigint,uuid,text,bigint,
                uuid,text,timestamptz,jsonb,bigint,uuid,text,text) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.observe_hard_coin_top_up_v1(
                uuid,uuid,text,text,text,text,text,bigint,text,text,timestamptz,uuid,uuid,bigint),
                economy_private.confirm_observed_hard_coin_top_up_v1(
                    uuid,uuid,uuid,uuid,text,integer,integer,integer,bigint,bigint,uuid,text,bigint,
                    uuid,text,timestamptz,jsonb,bigint,uuid,text,text)
                TO gameguild_economy_writer;
            ALTER FUNCTION economy_private.validate_provider_fact_allocation_v1()
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.validate_provider_fact_allocation_v1() FROM PUBLIC;
            """
        );
}
