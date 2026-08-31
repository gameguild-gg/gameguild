using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260826033000_FixImmutableEconomyTrigger")]
public sealed class FixImmutableEconomyTrigger : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(SafeDefinition);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PreviousDefinition);
    }

    private const string SafeDefinition = """
        CREATE OR REPLACE FUNCTION economy_private.deny_immutable_mutation_v1()
        RETURNS trigger
        LANGUAGE plpgsql
        SECURITY DEFINER
        SET search_path = pg_catalog, economy_private
        AS $function$
        DECLARE
            old_row jsonb;
            new_row jsonb;
        BEGIN
            -- Only journal rows have canonical hash columns. Converting the
            -- polymorphic trigger records to JSON avoids resolving those
            -- columns for every other immutable Economy relation.
            IF TG_TABLE_NAME = 'economy_journal_entries' AND TG_OP = 'UPDATE' THEN
                old_row := to_jsonb(OLD);
                new_row := to_jsonb(NEW);
                IF (old_row->>'HashAlgorithmVersion')::integer = 0
                   AND old_row->>'CanonicalPayloadHash' IS NULL
                   AND (new_row->>'HashAlgorithmVersion')::integer = 2
                   AND length(btrim(COALESCE(new_row->>'CanonicalPayloadHash', ''))) > 0
                   AND (new_row - 'HashAlgorithmVersion' - 'CanonicalPayloadHash') =
                       (old_row - 'HashAlgorithmVersion' - 'CanonicalPayloadHash')
                   AND EXISTS (
                       SELECT 1
                       FROM public.economy_idempotency_records idempotency
                       WHERE idempotency."PostingGroupId" = (old_row->>'PostingGroupId')::uuid
                         AND idempotency."RequestHash" = new_row->>'CanonicalPayloadHash') THEN
                    RETURN NEW;
                END IF;
            END IF;

            RAISE EXCEPTION 'immutable economy relation % rejects %', TG_TABLE_NAME, TG_OP
                USING ERRCODE = '42501';
        END
        $function$;

        ALTER FUNCTION economy_private.deny_immutable_mutation_v1()
            OWNER TO gameguild_economy_procedure_owner;
        REVOKE ALL ON FUNCTION economy_private.deny_immutable_mutation_v1() FROM PUBLIC;
        """;

    private const string PreviousDefinition = """
        CREATE OR REPLACE FUNCTION economy_private.deny_immutable_mutation_v1()
        RETURNS trigger
        LANGUAGE plpgsql
        SECURITY DEFINER
        SET search_path = pg_catalog, economy_private
        AS $function$
        BEGIN
            IF TG_TABLE_NAME = 'economy_journal_entries'
               AND TG_OP = 'UPDATE'
               AND OLD."HashAlgorithmVersion" = 0
               AND OLD."CanonicalPayloadHash" IS NULL
               AND NEW."HashAlgorithmVersion" = 2
               AND length(btrim(COALESCE(NEW."CanonicalPayloadHash", ''))) > 0
               AND (to_jsonb(NEW) - 'HashAlgorithmVersion' - 'CanonicalPayloadHash') =
                   (to_jsonb(OLD) - 'HashAlgorithmVersion' - 'CanonicalPayloadHash')
               AND EXISTS (
                   SELECT 1
                   FROM public.economy_idempotency_records idempotency
                   WHERE idempotency."PostingGroupId" = OLD."PostingGroupId"
                     AND idempotency."RequestHash" = NEW."CanonicalPayloadHash") THEN
                RETURN NEW;
            END IF;

            RAISE EXCEPTION 'immutable economy relation % rejects %', TG_TABLE_NAME, TG_OP
                USING ERRCODE = '42501';
        END
        $function$;

        ALTER FUNCTION economy_private.deny_immutable_mutation_v1()
            OWNER TO gameguild_economy_procedure_owner;
        REVOKE ALL ON FUNCTION economy_private.deny_immutable_mutation_v1() FROM PUBLIC;
        """;
}
