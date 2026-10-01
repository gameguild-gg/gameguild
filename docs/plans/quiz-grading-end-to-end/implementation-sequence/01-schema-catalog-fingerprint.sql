-- Canonical fingerprints for the disposable pre-cut baseline database.
-- Requires pgcrypto, which is already part of the baseline.

WITH catalog_rows AS (
    SELECT table_schema || '|' || table_name || '|' || ordinal_position || '|' ||
           column_name || '|' || data_type || '|' ||
           coalesce(character_maximum_length::text, '') || '|' || is_nullable || '|' ||
           coalesce(column_default, '') || '|' || coalesce(collation_name, '') AS value
    FROM information_schema.columns
    WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
)
SELECT 'columns|' || count(*) || '|' ||
       encode(digest(string_agg(value, E'\n' ORDER BY value), 'sha256'), 'hex')
FROM catalog_rows;

WITH catalog_rows AS (
    SELECT namespace.nspname || '|' || relation.relname || '|' || constraint_row.conname ||
           '|' || constraint_row.contype::text || '|' ||
           pg_get_constraintdef(constraint_row.oid, true) AS value
    FROM pg_constraint AS constraint_row
    JOIN pg_class AS relation ON relation.oid = constraint_row.conrelid
    JOIN pg_namespace AS namespace ON namespace.oid = relation.relnamespace
    WHERE namespace.nspname NOT IN ('pg_catalog', 'information_schema')
)
SELECT 'constraints|' || count(*) || '|' ||
       encode(digest(string_agg(value, E'\n' ORDER BY value), 'sha256'), 'hex')
FROM catalog_rows;

WITH catalog_rows AS (
    SELECT namespace.nspname || '|' || routine.proname || '(' ||
           pg_get_function_identity_arguments(routine.oid) || ')|' ||
           pg_get_functiondef(routine.oid) AS value
    FROM pg_proc AS routine
    JOIN pg_namespace AS namespace ON namespace.oid = routine.pronamespace
    WHERE namespace.nspname IN ('public', 'economy_private')
      AND routine.prokind IN ('f', 'p')
      AND NOT EXISTS (
          SELECT 1
          FROM pg_depend AS dependency
          JOIN pg_extension AS extension_row ON extension_row.oid = dependency.refobjid
          WHERE dependency.classid = 'pg_proc'::regclass
            AND dependency.objid = routine.oid
            AND dependency.deptype = 'e'
      )
)
SELECT 'routines|' || count(*) || '|' ||
       encode(digest(string_agg(value, E'\n' ORDER BY value), 'sha256'), 'hex')
FROM catalog_rows;

WITH catalog_rows AS (
    SELECT namespace.nspname || '|' || relation.relname || '|' || trigger_row.tgname ||
           '|' || pg_get_triggerdef(trigger_row.oid, true) AS value
    FROM pg_trigger AS trigger_row
    JOIN pg_class AS relation ON relation.oid = trigger_row.tgrelid
    JOIN pg_namespace AS namespace ON namespace.oid = relation.relnamespace
    WHERE NOT trigger_row.tgisinternal
)
SELECT 'triggers|' || count(*) || '|' ||
       encode(digest(string_agg(value, E'\n' ORDER BY value), 'sha256'), 'hex')
FROM catalog_rows;

WITH catalog_rows AS (
    SELECT schemaname || '|' || tablename || '|' || indexname || '|' || indexdef AS value
    FROM pg_indexes
    WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
      AND (
          indexdef ILIKE '% where %'
          OR indexdef ILIKE '% using gist %'
          OR indexdef ILIKE '%((%'
      )
)
SELECT 'special_indexes|' || count(*) || '|' ||
       encode(digest(string_agg(value, E'\n' ORDER BY value), 'sha256'), 'hex')
FROM catalog_rows;

WITH catalog_rows AS (
    SELECT 'table|' || grantee || '|' || table_schema || '|' || table_name || '|' ||
           privilege_type || '|' || is_grantable AS value
    FROM information_schema.role_table_grants
    WHERE grantee LIKE 'gameguild_%'
    UNION ALL
    SELECT 'routine|' || grantee || '|' || routine_schema || '|' || routine_name || '|' ||
           privilege_type || '|' || is_grantable
    FROM information_schema.role_routine_grants
    WHERE grantee LIKE 'gameguild_%'
)
SELECT 'grants|' || count(*) || '|' ||
       encode(digest(string_agg(value, E'\n' ORDER BY value), 'sha256'), 'hex')
FROM catalog_rows;
