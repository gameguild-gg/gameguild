using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

public partial class PublishAcceptedRegisteredPostings
{
    private static void InstallRegisteredPostingOutbox(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
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
                outbox_payload text;
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
            """);
    }
}