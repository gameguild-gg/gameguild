using System;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260805130000_AddContentJsonBody")]
public partial class AddContentJsonBody : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "JsonBody",
            table: "program_contents",
            type: "jsonb",
            nullable: true);

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION pg_temp.gameguild_is_json_object(body text)
            RETURNS boolean
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF body IS NULL THEN
                    RETURN FALSE;
                END IF;

                RETURN jsonb_typeof(body::jsonb) = 'object';
            EXCEPTION WHEN invalid_text_representation THEN
                RETURN FALSE;
            END;
            $$;

            UPDATE program_contents
            SET "JsonBody" = "Body"::jsonb,
                "Body" = NULL
            WHERE "Body" IS NOT NULL
              AND pg_temp.gameguild_is_json_object("Body")
              AND ("Type" IN (3, 5, 9) OR ("Type" IN (0, 1) AND "LessonFormat" = 1));
            """);

        migrationBuilder.AddCheckConstraint(
            name: "CK_program_contents_Body_JsonBody_Exclusive",
            table: "program_contents",
            sql: "NOT (\"Body\" IS NOT NULL AND \"JsonBody\" IS NOT NULL)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_program_contents_Body_JsonBody_Exclusive",
            table: "program_contents");

        migrationBuilder.Sql(
            "UPDATE program_contents SET \"Body\" = \"JsonBody\"::text WHERE \"JsonBody\" IS NOT NULL;");

        migrationBuilder.DropColumn(name: "JsonBody", table: "program_contents");
    }
}
