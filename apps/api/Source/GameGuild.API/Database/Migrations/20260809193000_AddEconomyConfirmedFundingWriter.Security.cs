using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

public partial class AddEconomyConfirmedFundingWriter
{
    private static void InstallConfirmedFundingWriter(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.observe_hard_coin_top_up_v1(
                p_source_stamp_id uuid,
                p_wallet_id uuid,
                p_provider text,
                p_environment text,
                p_connected_account text,
                p_provider_object text,
                p_provider_monetary_leg text,
                p_authoritative_units bigint,
                p_source_evidence_hash text,
                p_observed_event_hash text,
                p_observed_at timestamptz,
                p_actor_id uuid,
                p_tenant_id uuid,
                p_policy_version bigint)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                IF p_source_stamp_id IS NULL OR p_wallet_id IS NULL OR p_actor_id IS NULL OR p_tenant_id IS NULL
                   OR p_authoritative_units <= 0 OR p_policy_version <= 0 OR p_observed_at IS NULL
                   OR p_provider IS NULL OR p_environment IS NULL OR p_connected_account IS NULL
                   OR p_provider_object IS NULL OR p_provider_monetary_leg IS NULL
                   OR p_source_evidence_hash IS NULL OR p_observed_event_hash IS NULL
                   OR length(btrim(p_provider)) = 0 OR length(btrim(p_environment)) = 0
                   OR length(btrim(p_connected_account)) = 0 OR length(btrim(p_provider_object)) = 0
                   OR length(concat_ws(chr(31), btrim(p_provider), btrim(p_environment), btrim(p_connected_account),
                       btrim(p_provider_object), btrim(p_provider_monetary_leg))) > 256
                   OR length(btrim(p_provider_monetary_leg)) = 0 OR length(btrim(p_source_evidence_hash)) = 0
                   OR length(btrim(p_observed_event_hash)) = 0 THEN
                    RAISE EXCEPTION 'observed hard coin funding arguments are invalid' USING ERRCODE = '22023';
                END IF;

                PERFORM 1
                FROM public.economy_wallets wallet
                WHERE wallet."Id" = p_wallet_id AND wallet."State" = 1
                FOR SHARE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'funding wallet is absent or inactive' USING ERRCODE = '23503';
                END IF;

                INSERT INTO public.economy_source_stamps (
                    "Id", "SourceKind", "InternalSourceId", "SourceLegId", "Provider", "ProviderReference",
                    "EvidenceHash", "Provenance", "State", "ActorId", "TenantId", "PostingReferenceId",
                    "PolicyVersion", "AuthoritativeUnits", "ObservedAt", "ConfirmedAt")
                VALUES (
                    p_source_stamp_id, 'hard-coin-top-up', p_source_stamp_id::text,
                    encode(public.digest(convert_to(concat_ws('|', p_provider, p_environment, p_connected_account,
                        p_provider_object, p_provider_monetary_leg), 'UTF8'), 'sha256'), 'hex'),
                    btrim(p_provider), concat_ws(chr(31), btrim(p_provider), btrim(p_environment),
                        btrim(p_connected_account), btrim(p_provider_object), btrim(p_provider_monetary_leg)),
                    btrim(p_source_evidence_hash), 1, 1, p_actor_id, p_tenant_id, NULL,
                    p_policy_version, p_authoritative_units, p_observed_at, NULL);

                INSERT INTO public.economy_source_stamp_events (
                    "Id", "SourceStampId", "Sequence", "State", "EvidenceHash", "OccurredAt")
                VALUES (gen_random_uuid(), p_source_stamp_id, 1, 1, btrim(p_observed_event_hash), p_observed_at);

                INSERT INTO public.economy_funding_claims (
                    "SourceStampId", "WalletId", "Provider", "Environment", "ConnectedAccount", "ProviderObject",
                    "ProviderMonetaryLeg", "AuthoritativeUsdMinorUnits", "State", "ObservedAt", "ConfirmedAt",
                    "StateChangedAt", "PostingGroupId", "RootCreditLotId", "CumulativeProviderReversalUnits", "Version")
                VALUES (
                    p_source_stamp_id, p_wallet_id, btrim(p_provider), btrim(p_environment), btrim(p_connected_account),
                    btrim(p_provider_object), btrim(p_provider_monetary_leg), p_authoritative_units, 1,
                    p_observed_at, NULL, p_observed_at, NULL, NULL, 0, 1);

                PERFORM economy_private.rebuild_wallet_projection_v1(p_wallet_id, p_observed_at);
            END
            $function$;

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

                UPDATE public.economy_source_stamps
                SET "State" = 2, "ConfirmedAt" = p_requested_at, "PostingReferenceId" = p_posting_id
                WHERE "Id" = p_source_stamp_id;

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

                UPDATE public.economy_funding_claims
                SET "State" = 2,
                    "ConfirmedAt" = p_requested_at,
                    "StateChangedAt" = p_requested_at,
                    "PostingGroupId" = p_posting_id,
                    "RootCreditLotId" = p_credit_lot_id,
                    "Version" = "Version" + 1
                WHERE "SourceStampId" = p_source_stamp_id;

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
            """);
    }

    private static void RemoveConfirmedFundingWriter(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.confirm_observed_hard_coin_top_up_v1(
                uuid,uuid,uuid,uuid,text,integer,integer,integer,bigint,bigint,uuid,text,bigint,
                uuid,text,timestamptz,jsonb,bigint,uuid,text,text);
            DROP FUNCTION IF EXISTS economy_private.observe_hard_coin_top_up_v1(
                uuid,uuid,text,text,text,text,text,bigint,text,text,timestamptz,uuid,uuid,bigint);
            """);
    }
}
