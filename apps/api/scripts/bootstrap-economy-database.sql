\if :{?migration_role}
\else
\echo 'migration_role is required'
\quit 2
\endif

DO $roles$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gameguild_economy_migration') THEN
        CREATE ROLE gameguild_economy_migration NOLOGIN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gameguild_economy_runtime') THEN
        CREATE ROLE gameguild_economy_runtime NOLOGIN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gameguild_economy_writer') THEN
        CREATE ROLE gameguild_economy_writer NOLOGIN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gameguild_economy_procedure_owner') THEN
        CREATE ROLE gameguild_economy_procedure_owner NOLOGIN;
    END IF;
END
$roles$;

SELECT format(
    'GRANT gameguild_economy_migration, gameguild_economy_runtime, gameguild_economy_writer, gameguild_economy_procedure_owner TO %I',
    :'migration_role')
\gexec

CREATE SCHEMA IF NOT EXISTS economy_private
    AUTHORIZATION gameguild_economy_procedure_owner;

REVOKE ALL ON SCHEMA economy_private FROM PUBLIC;
GRANT USAGE, CREATE ON SCHEMA economy_private
    TO gameguild_economy_procedure_owner;

SELECT format(
    'GRANT USAGE, CREATE ON SCHEMA economy_private TO %I',
    :'migration_role')
\gexec
