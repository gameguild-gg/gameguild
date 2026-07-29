using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class RestorePermissionTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "PermissionTemplates" (
                    "Id" uuid NOT NULL,
                    "Name" character varying(100) NOT NULL,
                    "Description" character varying(500) NOT NULL,
                    "Permissions" text[] NOT NULL,
                    "IsSystemTemplate" boolean NOT NULL,
                    "IsActive" boolean NOT NULL,
                    "Category" character varying(50),
                    "MinimumTier" character varying(50),
                    "Metadata" jsonb,
                    "Version" integer NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    "DeletedAt" timestamp with time zone,
                    "TenantId" uuid,
                    CONSTRAINT "PK_PermissionTemplates" PRIMARY KEY ("Id")
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_PermissionTemplates_Name"
                    ON "PermissionTemplates" ("Name");

                CREATE INDEX IF NOT EXISTS "IX_PermissionTemplates_IsSystemTemplate"
                    ON "PermissionTemplates" ("IsSystemTemplate");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This repair may target a table that predates EF migrations; rollback must preserve it.
        }
    }
}
