using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

public partial class RepairEconomyFundingWriterPermissions
{
    private static void RepairFundingWriterPermissions(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("GRANT INSERT ON TABLE public.economy_funding_claims TO gameguild_economy_procedure_owner;");

    private static void RepairWithdrawalAuditSequenceLookup(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.append_admin_withdrawal_audit_event_v1(
                p_run_id uuid, p_kind text, p_actor_id uuid, p_evidence text, p_occurred_at timestamptz)
            RETURNS TABLE(
                "RunId" uuid, "Sequence" bigint, "Kind" text, "ActorId" uuid, "Evidence" text,
                "OccurredAt" timestamptz, "PreviousHash" text, "Hash" text)
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                previous_hash text;
                next_sequence bigint;
                next_hash text;
            BEGIN
                IF pg_catalog.length(pg_catalog.btrim(p_kind)) = 0
                   OR pg_catalog.length(pg_catalog.btrim(p_evidence)) = 0 THEN
                    RAISE EXCEPTION 'admin withdrawal audit evidence is required' USING ERRCODE = '23514';
                END IF;
                PERFORM 1 FROM public.economy_admin_withdrawal_runs
                WHERE "Id" = p_run_id FOR UPDATE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'admin withdrawal run was not found' USING ERRCODE = 'P0002';
                END IF;
                SELECT COALESCE(MAX(audit_event."Sequence"), 0) + 1,
                       COALESCE((array_agg(audit_event."Hash" ORDER BY audit_event."Sequence" DESC))[1], repeat('0', 64))
                INTO next_sequence, previous_hash
                FROM public.economy_admin_withdrawal_audit_events AS audit_event
                WHERE audit_event."RunId" = p_run_id;
                next_hash := encode(public.digest(convert_to(
                    replace(p_run_id::text, '-', '') || '|' || next_sequence::text || '|' ||
                    pg_catalog.btrim(p_kind) || '|' ||
                    COALESCE(replace(p_actor_id::text, '-', ''), '') || '|' ||
                    pg_catalog.btrim(p_evidence) || '|' ||
                    to_char(p_occurred_at AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.US"Z"') || '|' ||
                    previous_hash, 'UTF8'), 'sha256'), 'hex');

                INSERT INTO public.economy_admin_withdrawal_audit_events (
                    "RunId", "Sequence", "Kind", "ActorId", "Evidence", "OccurredAt", "PreviousHash", "Hash")
                VALUES (
                    p_run_id, next_sequence, pg_catalog.btrim(p_kind), p_actor_id, pg_catalog.btrim(p_evidence),
                    p_occurred_at, previous_hash, next_hash);

                RETURN QUERY SELECT p_run_id, next_sequence, pg_catalog.btrim(p_kind), p_actor_id,
                    pg_catalog.btrim(p_evidence), p_occurred_at, previous_hash, next_hash;
            END
            $function$;
            """);

    private static void RemoveFundingWriterPermissions(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("REVOKE INSERT ON TABLE public.economy_funding_claims FROM gameguild_economy_procedure_owner;");
}
