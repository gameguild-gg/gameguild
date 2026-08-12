using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <summary>
    /// Adds a functional BTREE index on program_contents."JsonBody"->>'type' and backfills
    /// v1 CodingAssignmentContent (PascalCase) into ProgramContent.JsonBody from v2
    /// Assessment.DefinitionPayload (camelCase) for coding assignments on schema version 2.
    ///
    /// Idempotent: index uses IF NOT EXISTS; backfill UPDATE is guarded by
    /// "JsonBody" IS NULL OR NOT ("JsonBody" ? 'type'), so re-running Up is a no-op.
    /// </summary>
    public partial class AddCodingAssignmentJsonBodyV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // (1) Functional BTREE on the JsonBody type discriminator. NOT GIN — operator-class
            // BTREE on a text expression is exactly what equality + range scans on this
            // discriminator want, and it stays cheap to maintain on write.
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_program_contents_JsonBody_type"
                    ON program_contents (("JsonBody"->>'type'));
                """);

            // (2) Idempotent v2 → v1 backfill. The v2 DefinitionPayload is camelCase JSON
            // matching Learning.Assessments.CodingAssignmentDefinition; the v1 JsonBody is
            // PascalCase matching Learning.Courses.CodingAssignmentContent. Only stdio and
            // stdio-file test cases map to v1 StandardTest — doctest / clang-query / custom
            // are intentionally dropped here (they remain in the old DefinitionPayload for
            // rollback until Task 3 removes the endpoints that consume them).
            //
            // The PL/pgSQL helper is created in pg_temp so it vanishes at session end and
            // never collides between migrations or runs.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION pg_temp.migrate_coding_v2_to_v1(payload jsonb)
                RETURNS jsonb
                LANGUAGE plpgsql
                IMMUTABLE
                AS $$
                DECLARE
                    files jsonb := COALESCE(payload #> '{workspaceConfig,files}', '{}'::jsonb);
                    cases jsonb := COALESCE(payload #> '{testPlan,cases}', '[]'::jsonb);
                    v1_files jsonb;
                    v1_public jsonb;
                    v1_private jsonb;
                BEGIN
                    SELECT COALESCE(jsonb_object_agg(
                        f.key,
                        jsonb_build_object(
                            'Content',   COALESCE(f.value->>'content', ''),
                            'Encoding',  COALESCE(f.value->>'encoding', 'text'),
                            'Visibility','Public',
                            'Modifiable', true
                        )
                    ), '{}'::jsonb)
                    INTO v1_files
                    FROM jsonb_each(files) AS f(key, value);

                    SELECT COALESCE(jsonb_agg(
                        CASE
                            WHEN c.value->>'kind' = 'stdio' THEN jsonb_build_object(
                                'kind',    'standard',
                                'Weight',  COALESCE((c.value->>'weight')::numeric, 1.0),
                                'Stdin',   c.value->'stdin',
                                'Stdout',  c.value->'expectedStdout',
                                'Stderr',  c.value->'expectedStderr',
                                'ExitCode',c.value->'expectedExit'
                            )
                            WHEN c.value->>'kind' = 'stdio-file' THEN jsonb_build_object(
                                'kind',    'standard',
                                'Weight',  COALESCE((c.value->>'weight')::numeric, 1.0),
                                'Stdin',   files->(c.value->>'inFile')->'content',
                                'Stdout',  files->(c.value->>'expectedOutFile')->'content'
                            )
                        END
                    ), '[]'::jsonb)
                    INTO v1_public
                    FROM jsonb_array_elements(cases) AS c(value)
                    WHERE c.value->>'kind' IN ('stdio', 'stdio-file')
                      AND NOT COALESCE((c.value->>'hidden')::boolean, false);

                    SELECT COALESCE(jsonb_agg(
                        CASE
                            WHEN c.value->>'kind' = 'stdio' THEN jsonb_build_object(
                                'kind',    'standard',
                                'Weight',  COALESCE((c.value->>'weight')::numeric, 1.0),
                                'Stdin',   c.value->'stdin',
                                'Stdout',  c.value->'expectedStdout',
                                'Stderr',  c.value->'expectedStderr',
                                'ExitCode',c.value->'expectedExit'
                            )
                            WHEN c.value->>'kind' = 'stdio-file' THEN jsonb_build_object(
                                'kind',    'standard',
                                'Weight',  COALESCE((c.value->>'weight')::numeric, 1.0),
                                'Stdin',   files->(c.value->>'inFile')->'content',
                                'Stdout',  files->(c.value->>'expectedOutFile')->'content'
                            )
                        END
                    ), '[]'::jsonb)
                    INTO v1_private
                    FROM jsonb_array_elements(cases) AS c(value)
                    WHERE c.value->>'kind' IN ('stdio', 'stdio-file')
                      AND COALESCE((c.value->>'hidden')::boolean, false);

                    RETURN jsonb_build_object(
                        'Type',    'coding-assignment',
                        'Version', 1,
                        'Environment', jsonb_build_object(
                            'Language',              COALESCE(payload->>'language', 'cpp'),
                            'Tools',                 'clang',
                            'LibBundle',             NULL::text,
                            'AllowStudentCreateFiles', false
                        ),
                        'Data', jsonb_build_object('Files', v1_files),
                        'Tests', jsonb_build_object(
                            'Public',  v1_public,
                            'Private', v1_private
                        ),
                        'Grading', jsonb_build_object(
                            'MaxScore',     COALESCE((payload->>'maxScore')::int, 0),
                            'PassingScore', COALESCE((payload->>'passingScore')::int, 0)
                        )
                    );
                END;
                $$;

                UPDATE program_contents AS pc
                SET "JsonBody" = pg_temp.migrate_coding_v2_to_v1(a."DefinitionPayload")
                FROM "Assessments" AS a
                WHERE a."ContentId" = pc."Id"
                  AND a."DefinitionSchemaVersion" = 2
                  AND a."DefinitionPayload"->>'kind' = 'coding'
                  AND a."ContentId" IS NOT NULL
                  AND (pc."JsonBody" IS NULL OR NOT (pc."JsonBody" ? 'type'));

                DROP FUNCTION IF EXISTS pg_temp.migrate_coding_v2_to_v1(jsonb);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // JsonBody writes are additive — original DefinitionPayload stays intact on
            // "Assessments", so we do NOT undo them. Only the index is reverted.
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_program_contents_JsonBody_type";
                """);
        }
    }
}
