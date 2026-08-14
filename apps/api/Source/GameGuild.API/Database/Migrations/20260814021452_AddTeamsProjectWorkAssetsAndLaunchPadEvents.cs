using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamsProjectWorkAssetsAndLaunchPadEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Older environments were created from snapshots that declared these indexes
            // without a matching migration. Keep the rollout compatible with both shapes.
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_project_collaboration_teams_Name\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_launch_plans_ProjectId\";");

            migrationBuilder.EnsureSchema(
                name: "assets");

            // These legacy Team entities existed in the EF snapshot but were never emitted by
            // an executable migration. Bootstrap the old shape so both clean and existing
            // databases can be upgraded through the same migration.
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS project_collaboration_teams (
                    "Id" uuid NOT NULL,
                    "Name" character varying(200) NOT NULL,
                    "Description" character varying(2000),
                    "IsActive" boolean NOT NULL DEFAULT TRUE,
                    "Version" integer NOT NULL DEFAULT 0,
                    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    "DeletedAt" timestamp with time zone,
                    "TenantId" uuid,
                    CONSTRAINT "PK_project_collaboration_teams" PRIMARY KEY ("Id")
                );

                CREATE TABLE IF NOT EXISTS project_collaboration_team_members (
                    "Id" uuid NOT NULL,
                    "TeamId" uuid NOT NULL,
                    "UserId" uuid NOT NULL,
                    "Role" character varying(100) NOT NULL DEFAULT 'Member',
                    "JoinedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    "IsActive" boolean NOT NULL DEFAULT TRUE,
                    "Version" integer NOT NULL DEFAULT 0,
                    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    "DeletedAt" timestamp with time zone,
                    "TenantId" uuid,
                    CONSTRAINT "PK_project_collaboration_team_members" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_project_collaboration_team_members_Users_UserId"
                        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_project_collaboration_team_members_project_collaboration_teams_TeamId"
                        FOREIGN KEY ("TeamId") REFERENCES project_collaboration_teams ("Id") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS "IX_project_collaboration_team_members_UserId"
                    ON project_collaboration_team_members ("UserId");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_project_collaboration_team_members_TeamId_UserId"
                    ON project_collaboration_team_members ("TeamId", "UserId");

                -- Reuse the real legacy Teams/TeamMembers tables emitted by the 202606 closeout.
                -- Unknown role strings are intentionally retained until the typed-role conversion
                -- below moves them into ProfessionalTitle for auditability.
                INSERT INTO project_collaboration_teams
                    ("Id", "Name", "Description", "IsActive", "Version", "CreatedAt", "UpdatedAt", "DeletedAt", "TenantId")
                SELECT "Id", "Name", "Description", "IsActive", "Version", "CreatedAt", "UpdatedAt", "DeletedAt", "TenantId"
                FROM "Teams"
                ON CONFLICT ("Id") DO UPDATE
                SET "Name" = EXCLUDED."Name", "Description" = EXCLUDED."Description",
                    "IsActive" = EXCLUDED."IsActive", "UpdatedAt" = EXCLUDED."UpdatedAt",
                    "DeletedAt" = EXCLUDED."DeletedAt", "TenantId" = EXCLUDED."TenantId";

                INSERT INTO project_collaboration_team_members
                    ("Id", "TeamId", "UserId", "Role", "JoinedAt", "IsActive", "Version",
                     "CreatedAt", "UpdatedAt", "DeletedAt", "TenantId")
                SELECT tm."Id", tm."TeamId", tm."UserId", tm."Role", tm."JoinedAt", tm."IsActive", tm."Version",
                       tm."CreatedAt", tm."UpdatedAt", tm."DeletedAt", tm."TenantId"
                FROM "TeamMembers" tm
                JOIN project_collaboration_teams t ON t."Id" = tm."TeamId"
                ON CONFLICT ("TeamId", "UserId") DO UPDATE
                SET "Role" = EXCLUDED."Role", "JoinedAt" = EXCLUDED."JoinedAt",
                    "IsActive" = EXCLUDED."IsActive", "UpdatedAt" = EXCLUDED."UpdatedAt",
                    "DeletedAt" = EXCLUDED."DeletedAt", "TenantId" = EXCLUDED."TenantId";

                -- A former stub TeamId could be reused across tenants because there was no Team
                -- table enforcing its scope. Preserve the first tenant's ID and split only the
                -- conflicting tenant rows into deterministic IDs.
                WITH scoped AS (
                    SELECT "TeamId", "TenantId",
                           min("TenantId"::text) OVER (PARTITION BY "TeamId") AS first_tenant,
                           count(*) OVER (PARTITION BY "TeamId") AS tenant_count
                    FROM (SELECT DISTINCT "TeamId", "TenantId" FROM project_teams) pairs
                ), mappings AS (
                    SELECT "TeamId" AS old_team_id, "TenantId" AS tenant_id,
                           (substr(team_hash,1,8) || '-' || substr(team_hash,9,4) || '-' || substr(team_hash,13,4) || '-' ||
                            substr(team_hash,17,4) || '-' || substr(team_hash,21,12))::uuid AS new_team_id
                    FROM (
                        SELECT *, md5("TeamId"::text || ':' || COALESCE("TenantId"::text, 'global') || ':tenant-split') AS team_hash
                        FROM scoped
                        WHERE tenant_count > 1 AND COALESCE("TenantId"::text, '') <> COALESCE(first_tenant, '')
                    ) hashes
                )
                UPDATE project_teams pt
                SET "TeamId" = mappings.new_team_id
                FROM mappings
                WHERE pt."TeamId" = mappings.old_team_id
                  AND pt."TenantId" IS NOT DISTINCT FROM mappings.tenant_id;

                INSERT INTO project_collaboration_teams
                    ("Id", "Name", "Description", "IsActive", "Version", "CreatedAt", "UpdatedAt", "DeletedAt", "TenantId")
                SELECT DISTINCT ON (pt."TeamId") pt."TeamId", 'Legacy Team ' || left(replace(pt."TeamId"::text, '-', ''), 8),
                       'Team materialized from a legacy ProjectTeam relation.', TRUE, 0,
                       CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, NULL, pt."TenantId"
                FROM project_teams pt
                WHERE pt."TeamId" IS NOT NULL
                ORDER BY pt."TeamId", pt."AssignedAt", pt."Id"
                ON CONFLICT ("Id") DO NOTHING;

                -- The old executable migration pointed ProjectTeam at "Teams", while the
                -- snapshot pointed at project_collaboration_teams. Rebind the real FK so newly
                -- created Teams work and the resource cannot cross the normalized boundary.
                ALTER TABLE project_teams
                    DROP CONSTRAINT IF EXISTS "FK_project_teams_Teams_TeamId";
                ALTER TABLE project_teams
                    DROP CONSTRAINT IF EXISTS "FK_project_teams_project_collaboration_teams_TeamId";
                ALTER TABLE project_teams
                    ADD CONSTRAINT "FK_project_teams_project_collaboration_teams_TeamId"
                    FOREIGN KEY ("TeamId") REFERENCES project_collaboration_teams ("Id") ON DELETE RESTRICT;
                """);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedAssetReferenceIdsJson",
                table: "testing_project_applications",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "project_teams",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "ParticipationMode",
                table: "project_teams",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPersonal",
                table: "project_collaboration_teams",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "project_collaboration_teams",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "project_collaboration_teams",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "project_collaboration_teams",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Authority",
                table: "project_collaboration_team_members",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LeftAt",
                table: "project_collaboration_team_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfessionalTitle",
                table: "project_collaboration_team_members",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE project_collaboration_teams
                SET "Slug" = COALESCE(NULLIF("Slug", ''),
                        trim(both '-' from lower(regexp_replace("Name", '[^a-zA-Z0-9]+', '-', 'g'))) || '-' || left(replace("Id"::text, '-', ''), 8)),
                    "Status" = COALESCE(NULLIF("Status", ''), 'Active'),
                    "Visibility" = COALESCE(NULLIF("Visibility", ''), 'Private');

                UPDATE project_collaboration_team_members
                SET "Authority" = CASE lower(COALESCE("Role", ''))
                        WHEN 'owner' THEN 'Owner'
                        WHEN 'admin' THEN 'Manager'
                        WHEN 'manager' THEN 'Manager'
                        WHEN 'viewer' THEN 'Viewer'
                        ELSE 'Member'
                    END,
                    "ProfessionalTitle" = CASE
                        WHEN lower(COALESCE("Role", '')) IN ('owner', 'admin', 'manager', 'member', 'viewer') THEN "ProfessionalTitle"
                        ELSE COALESCE("ProfessionalTitle", NULLIF("Role", ''))
                    END;

                WITH ranked AS (
                    SELECT "Id", "ProjectId", "Role",
                           row_number() OVER (
                               PARTITION BY "ProjectId"
                               ORDER BY CASE WHEN lower("Role") IN ('owner', 'lead') THEN 0 ELSE 1 END,
                                        "AssignedAt", "Id") AS owner_rank
                    FROM project_teams
                    WHERE "DeletedAt" IS NULL AND "IsActive" = TRUE AND "EndedAt" IS NULL
                )
                UPDATE project_teams pt
                SET "Role" = CASE
                        WHEN ranked.owner_rank = 1 THEN 'Owner'
                        WHEN lower(ranked."Role") IN ('owner', 'coowner', 'co-owner') THEN 'CoOwner'
                        WHEN lower(ranked."Role") IN ('guest', 'viewer') THEN 'Guest'
                        ELSE 'Contributor'
                    END,
                    "ParticipationMode" = CASE WHEN ranked.owner_rank = 1 THEN 'AllMembers' ELSE 'SelectedMembers' END
                FROM ranked
                WHERE pt."Id" = ranked."Id";

                -- The legacy stub did not persist Team memberships. Give each Project creator
                -- ownership authority in the normalized Owner Team so the Project remains
                -- manageable after rollout.
                WITH owners AS (
                    SELECT DISTINCT ON (pt."TeamId", p."CreatedById")
                           pt."TeamId" AS team_id, p."CreatedById" AS user_id,
                           COALESCE(pt."TenantId", p."TenantId") AS tenant_id,
                           md5(pt."TeamId"::text || ':' || p."CreatedById"::text || ':owner-member') AS member_hash
                    FROM project_teams pt
                    JOIN projects p ON p."Id" = pt."ProjectId"
                    WHERE pt."Role" = 'Owner' AND pt."IsActive" = TRUE AND pt."EndedAt" IS NULL
                      AND pt."DeletedAt" IS NULL AND p."CreatedById" IS NOT NULL
                    ORDER BY pt."TeamId", p."CreatedById", pt."AssignedAt", pt."Id"
                )
                INSERT INTO project_collaboration_team_members
                    ("Id", "TeamId", "UserId", "Role", "Authority", "ProfessionalTitle", "JoinedAt", "LeftAt", "IsActive",
                     "TenantId", "CreatedAt", "UpdatedAt", "DeletedAt", "Version")
                SELECT (substr(member_hash,1,8) || '-' || substr(member_hash,9,4) || '-' || substr(member_hash,13,4) || '-' ||
                        substr(member_hash,17,4) || '-' || substr(member_hash,21,12))::uuid,
                       team_id, user_id, 'Owner', 'Owner', NULL, CURRENT_TIMESTAMP, NULL, TRUE,
                       tenant_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, NULL, 0
                FROM owners
                ON CONFLICT ("TeamId", "UserId") DO UPDATE
                SET "Authority" = 'Owner', "IsActive" = TRUE, "LeftAt" = NULL, "DeletedAt" = NULL;

                UPDATE project_teams
                SET "Role" = CASE
                        WHEN lower("Role") IN ('owner') THEN 'Owner'
                        WHEN lower("Role") IN ('coowner', 'co-owner') THEN 'CoOwner'
                        WHEN lower("Role") IN ('guest', 'viewer') THEN 'Guest'
                        ELSE 'Contributor'
                    END,
                    "ParticipationMode" = COALESCE(NULLIF("ParticipationMode", ''), 'SelectedMembers')
                WHERE "DeletedAt" IS NOT NULL OR "IsActive" = FALSE OR "EndedAt" IS NOT NULL;

                WITH missing AS (
                    SELECT p."Id" AS project_id, p."TenantId" AS tenant_id, p."CreatedById" AS creator_id,
                           p."Title" AS title,
                           md5(p."Id"::text || ':legacy-owner-team') AS team_hash
                    FROM projects p
                    WHERE p."DeletedAt" IS NULL
                      AND NOT EXISTS (
                          SELECT 1 FROM project_teams pt
                          WHERE pt."ProjectId" = p."Id" AND pt."DeletedAt" IS NULL AND pt."IsActive" = TRUE AND pt."EndedAt" IS NULL)
                ), materialized AS (
                    SELECT *,
                           (substr(team_hash,1,8) || '-' || substr(team_hash,9,4) || '-' || substr(team_hash,13,4) || '-' || substr(team_hash,17,4) || '-' || substr(team_hash,21,12))::uuid AS team_id
                    FROM missing
                )
                INSERT INTO project_collaboration_teams
                    ("Id", "Name", "Slug", "Description", "Visibility", "Status", "IsPersonal", "IsActive",
                     "TenantId", "CreatedAt", "UpdatedAt", "DeletedAt", "Version")
                SELECT team_id, COALESCE(NULLIF(title, ''), 'Legacy Project') || ' Team',
                       'legacy-project-' || replace(project_id::text, '-', ''),
                       'Team created automatically while migrating a Project that had no owner Team.',
                       'Private', 'Active', TRUE, TRUE, tenant_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, NULL, 0
                FROM materialized
                ON CONFLICT ("Id") DO NOTHING;

                WITH missing AS (
                    SELECT p."Id" AS project_id, p."TenantId" AS tenant_id,
                           md5(p."Id"::text || ':legacy-owner-team') AS team_hash
                    FROM projects p
                    WHERE p."DeletedAt" IS NULL
                      AND NOT EXISTS (
                          SELECT 1 FROM project_teams pt
                          WHERE pt."ProjectId" = p."Id" AND pt."DeletedAt" IS NULL AND pt."IsActive" = TRUE AND pt."EndedAt" IS NULL)
                ), materialized AS (
                    SELECT *,
                           (substr(team_hash,1,8) || '-' || substr(team_hash,9,4) || '-' || substr(team_hash,13,4) || '-' || substr(team_hash,17,4) || '-' || substr(team_hash,21,12))::uuid AS team_id,
                           md5(project_id::text || ':legacy-project-team') AS link_hash
                    FROM missing
                )
                INSERT INTO project_teams
                    ("Id", "ProjectId", "TeamId", "Role", "ParticipationMode", "AssignedAt", "EndedAt", "IsActive",
                     "Permissions", "Notes", "ContributionPercentage", "TenantId", "CreatedAt", "UpdatedAt", "DeletedAt", "Version")
                SELECT (substr(link_hash,1,8) || '-' || substr(link_hash,9,4) || '-' || substr(link_hash,13,4) || '-' || substr(link_hash,17,4) || '-' || substr(link_hash,21,12))::uuid,
                       project_id, team_id, 'Owner', 'AllMembers', CURRENT_TIMESTAMP, NULL, TRUE,
                       NULL, 'Created by Teams ownership migration.', 100, tenant_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, NULL, 0
                FROM materialized
                ON CONFLICT DO NOTHING;

                WITH candidates AS (
                    SELECT p."Id" AS project_id, p."TenantId" AS tenant_id, p."CreatedById" AS user_id,
                           md5(p."Id"::text || ':legacy-owner-team') AS team_hash
                    FROM projects p
                    WHERE p."DeletedAt" IS NULL AND p."CreatedById" IS NOT NULL
                ), materialized AS (
                    SELECT *,
                           (substr(team_hash,1,8) || '-' || substr(team_hash,9,4) || '-' || substr(team_hash,13,4) || '-' || substr(team_hash,17,4) || '-' || substr(team_hash,21,12))::uuid AS team_id,
                           md5(project_id::text || ':' || user_id::text || ':legacy-team-member') AS member_hash
                    FROM candidates
                )
                INSERT INTO project_collaboration_team_members
                    ("Id", "TeamId", "UserId", "Role", "Authority", "ProfessionalTitle", "JoinedAt", "LeftAt", "IsActive",
                     "TenantId", "CreatedAt", "UpdatedAt", "DeletedAt", "Version")
                SELECT (substr(member_hash,1,8) || '-' || substr(member_hash,9,4) || '-' || substr(member_hash,13,4) || '-' || substr(member_hash,17,4) || '-' || substr(member_hash,21,12))::uuid,
                       team_id, user_id, 'Owner', 'Owner', NULL, CURRENT_TIMESTAMP, NULL, TRUE,
                       tenant_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, NULL, 0
                FROM materialized
                WHERE EXISTS (SELECT 1 FROM project_collaboration_teams t WHERE t."Id" = materialized.team_id)
                ON CONFLICT ("TeamId", "UserId") DO NOTHING;
                """);

            migrationBuilder.DropColumn(
                name: "Role",
                table: "project_collaboration_team_members");

            migrationBuilder.AddColumn<Guid>(
                name: "LaunchPadApplicationId",
                table: "launch_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LaunchPadEventId",
                table: "launch_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectVersionId",
                table: "launch_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "asset_contents",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PerceptualHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BucketName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    VirusScanStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VirusScanCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModerationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModerationCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModerationReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModerationReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModerationReviewNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ModerationLabels = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsDeletable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ReferenceCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    MarkedForDeletionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_contents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "asset_folders",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ParentResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentFolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RestrictionMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AllowedTeamIdsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AllowedAuthoritiesJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_folders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "languages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "launch_pad_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApplicationsOpenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApplicationsCloseAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_launch_pad_events", x => x.Id);
                });

            // Preserve legacy launch plans by grouping them under one archived import event per tenant.
            // Application and version remain nullable only for these imported rows; all newly approved
            // applications create fully-linked launch plans through the Launch Pad event workflow.
            migrationBuilder.Sql("""
                WITH legacy AS (
                    SELECT lp."Id" AS plan_id,
                           COALESCE(lp."TenantId", p."TenantId") AS tenant_id,
                           COALESCE(lp."CreatedAt", CURRENT_TIMESTAMP) AS created_at,
                           COALESCE(lp."UpdatedAt", lp."CreatedAt", CURRENT_TIMESTAMP) AS updated_at
                    FROM launch_plans lp
                    INNER JOIN projects p ON p."Id" = lp."ProjectId"
                    WHERE lp."DeletedAt" IS NULL
                      AND lp."LaunchPadEventId" IS NULL
                      AND COALESCE(lp."TenantId", p."TenantId") IS NOT NULL
                ), tenant_events AS (
                    SELECT tenant_id,
                           (substr(event_hash,1,8) || '-' || substr(event_hash,9,4) || '-' || substr(event_hash,13,4) || '-' ||
                            substr(event_hash,17,4) || '-' || substr(event_hash,21,12))::uuid AS event_id,
                           MIN(created_at) AS starts_at,
                           GREATEST(MAX(updated_at), MIN(created_at) + INTERVAL '1 second') AS ends_at
                    FROM (
                        SELECT legacy.*, md5(tenant_id::text || ':imported-launch-plans-event') AS event_hash
                        FROM legacy
                    ) materialized
                    GROUP BY tenant_id, event_hash
                )
                INSERT INTO launch_pad_events
                    ("Id", "Name", "Description", "StartsAt", "EndsAt", "ApplicationsOpenAt", "ApplicationsCloseAt",
                     "Status", "Version", "CreatedAt", "UpdatedAt", "DeletedAt", "TenantId")
                SELECT event_id, 'Imported Launch Plans',
                       'Archived event created automatically for launch plans that predate Launch Pad events.',
                       starts_at, ends_at, NULL, NULL, 7, 0, starts_at, ends_at, NULL, tenant_id
                FROM tenant_events
                ON CONFLICT ("Id") DO NOTHING;

                WITH legacy AS (
                    SELECT lp."Id" AS plan_id,
                           COALESCE(lp."TenantId", p."TenantId") AS tenant_id,
                           md5(COALESCE(lp."TenantId", p."TenantId")::text || ':imported-launch-plans-event') AS event_hash
                    FROM launch_plans lp
                    INNER JOIN projects p ON p."Id" = lp."ProjectId"
                    WHERE lp."DeletedAt" IS NULL
                      AND lp."LaunchPadEventId" IS NULL
                      AND COALESCE(lp."TenantId", p."TenantId") IS NOT NULL
                )
                UPDATE launch_plans lp
                SET "LaunchPadEventId" = (substr(legacy.event_hash,1,8) || '-' || substr(legacy.event_hash,9,4) || '-' ||
                                           substr(legacy.event_hash,13,4) || '-' || substr(legacy.event_hash,17,4) || '-' ||
                                           substr(legacy.event_hash,21,12))::uuid,
                    "TenantId" = legacy.tenant_id,
                    "UpdatedAt" = CURRENT_TIMESTAMP
                FROM legacy
                WHERE lp."Id" = legacy.plan_id;
                """);

            migrationBuilder.CreateTable(
                name: "project_member_allocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Function = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CapacityPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_member_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_member_allocations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project_member_allocations_project_teams_ProjectTeamId",
                        column: x => x.ProjectTeamId,
                        principalTable: "project_teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_member_allocations_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_milestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_milestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_milestones_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_task_labels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_task_labels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_task_labels_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_team_agreements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposingTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivingTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Scope = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Deliverables = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_team_agreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_team_agreements_project_collaboration_teams_Proposi~",
                        column: x => x.ProposingTeamId,
                        principalTable: "project_collaboration_teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project_team_agreements_project_collaboration_teams_Receivi~",
                        column: x => x.ReceivingTeamId,
                        principalTable: "project_collaboration_teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project_team_agreements_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_work_boards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_work_boards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_work_boards_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_work_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChangesJson = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_work_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_work_history_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvitedEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Authority = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_invitations_project_collaboration_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "project_collaboration_teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transformed_assets",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransformationSpec = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BucketName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transformed_assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transformed_assets_asset_contents_SourceContentId",
                        column: x => x.SourceContentId,
                        principalSchema: "assets",
                        principalTable: "asset_contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_references",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OriginalFilename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AltText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AccessPolicy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ParentResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ParentResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    FolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentRevisionNumber = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AccessCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DownloadWindowExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GrantedByOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_references", x => x.Id);
                    table.ForeignKey(
                        name: "FK_asset_references_asset_contents_AssetContentId",
                        column: x => x.AssetContentId,
                        principalSchema: "assets",
                        principalTable: "asset_contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_references_asset_folders_FolderId",
                        column: x => x.FolderId,
                        principalSchema: "assets",
                        principalTable: "asset_folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "launch_pad_applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaunchPadEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Pitch = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SubmittedAssetReferenceIdsJson = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_launch_pad_applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_launch_pad_applications_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_launch_pad_applications_launch_pad_events_LaunchPadEventId",
                        column: x => x.LaunchPadEventId,
                        principalTable: "launch_pad_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_launch_pad_applications_project_versions_ProjectVersionId",
                        column: x => x.ProjectVersionId,
                        principalTable: "project_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_launch_pad_applications_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "launch_pad_participant_slots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaunchPadEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    ReservedCount = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_launch_pad_participant_slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_launch_pad_participant_slots_launch_pad_events_LaunchPadEve~",
                        column: x => x.LaunchPadEventId,
                        principalTable: "launch_pad_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_work_columns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BoardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    WorkInProgressLimit = table.Column<int>(type: "integer", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_work_columns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_work_columns_project_work_boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "project_work_boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_reference_revisions",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_reference_revisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_asset_reference_revisions_asset_contents_AssetContentId",
                        column: x => x.AssetContentId,
                        principalSchema: "assets",
                        principalTable: "asset_contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_reference_revisions_asset_references_AssetReferenceId",
                        column: x => x.AssetReferenceId,
                        principalSchema: "assets",
                        principalTable: "asset_references",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_reports",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_asset_reports_asset_references_AssetReferenceId",
                        column: x => x.AssetReferenceId,
                        principalSchema: "assets",
                        principalTable: "asset_references",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_scoped_access_grants",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_scoped_access_grants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_asset_scoped_access_grants_asset_references_AssetReferenceId",
                        column: x => x.AssetReferenceId,
                        principalSchema: "assets",
                        principalTable: "asset_references",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource_localizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssetReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_localizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_resource_localizations_asset_references_AssetReferenceId",
                        column: x => x.AssetReferenceId,
                        principalSchema: "assets",
                        principalTable: "asset_references",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_resource_localizations_languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "launch_pad_participant_registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaunchPadParticipantSlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_launch_pad_participant_registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_launch_pad_participant_registrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_launch_pad_participant_registrations_launch_pad_participant~",
                        column: x => x.LaunchPadParticipantSlotId,
                        principalTable: "launch_pad_participant_slots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_work_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoardId = table.Column<Guid>(type: "uuid", nullable: false),
                    ColumnId = table.Column<Guid>(type: "uuid", nullable: false),
                    MilestoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AssigneeUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    DueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_work_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_work_tasks_Users_AssigneeUserId",
                        column: x => x.AssigneeUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_project_work_tasks_project_milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "project_milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_project_work_tasks_project_work_columns_ColumnId",
                        column: x => x.ColumnId,
                        principalTable: "project_work_columns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project_work_tasks_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_task_checklist_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_task_checklist_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_task_checklist_items_project_work_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "project_work_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_task_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_task_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_task_comments_project_work_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "project_work_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_task_dependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    DependsOnTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_task_dependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_task_dependencies_project_work_tasks_DependsOnTaskId",
                        column: x => x.DependsOnTaskId,
                        principalTable: "project_work_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project_task_dependencies_project_work_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "project_work_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_task_label_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_task_label_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_task_label_assignments_project_task_labels_LabelId",
                        column: x => x.LabelId,
                        principalTable: "project_task_labels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_task_label_assignments_project_work_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "project_work_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project_collaboration_teams_TenantId_Slug",
                table: "project_collaboration_teams",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_launch_plans_LaunchPadApplicationId",
                table: "launch_plans",
                column: "LaunchPadApplicationId",
                unique: true,
                filter: "\"LaunchPadApplicationId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_launch_plans_LaunchPadEventId",
                table: "launch_plans",
                column: "LaunchPadEventId");

            migrationBuilder.CreateIndex(
                name: "IX_launch_plans_ProjectId",
                table: "launch_plans",
                column: "ProjectId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"LaunchPadEventId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_launch_plans_ProjectVersionId",
                table: "launch_plans",
                column: "ProjectVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetContents_ContentHash",
                schema: "assets",
                table: "asset_contents",
                column: "ContentHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetContents_GC",
                schema: "assets",
                table: "asset_contents",
                columns: new[] { "ReferenceCount", "MarkedForDeletionAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetContents_ModerationStatus",
                schema: "assets",
                table: "asset_contents",
                column: "ModerationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AssetContents_VirusScanStatus",
                schema: "assets",
                table: "asset_contents",
                column: "VirusScanStatus");

            migrationBuilder.CreateIndex(
                name: "IX_asset_folders_ParentResourceType_ParentResourceId_ParentFol~",
                schema: "assets",
                table: "asset_folders",
                columns: new[] { "ParentResourceType", "ParentResourceId", "ParentFolderId", "Name" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_asset_reference_revisions_AssetContentId",
                schema: "assets",
                table: "asset_reference_revisions",
                column: "AssetContentId");

            migrationBuilder.CreateIndex(
                name: "IX_asset_reference_revisions_AssetReferenceId_RevisionNumber",
                schema: "assets",
                table: "asset_reference_revisions",
                columns: new[] { "AssetReferenceId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_asset_references_FolderId",
                schema: "assets",
                table: "asset_references",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReferences_AccessPolicy",
                schema: "assets",
                table: "asset_references",
                column: "AccessPolicy");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReferences_ContentId",
                schema: "assets",
                table: "asset_references",
                column: "AssetContentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReferences_Parent",
                schema: "assets",
                table: "asset_references",
                columns: new[] { "ParentResourceType", "ParentResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetReferences_UserId",
                schema: "assets",
                table: "asset_references",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReports_ReferenceId",
                schema: "assets",
                table: "asset_reports",
                column: "AssetReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReports_ReporterId",
                schema: "assets",
                table: "asset_reports",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReports_Status",
                schema: "assets",
                table: "asset_reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReports_Unique_UserReport",
                schema: "assets",
                table: "asset_reports",
                columns: new[] { "AssetReferenceId", "ReportedByUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_asset_scoped_access_grants_AssetReferenceId_UserId_ScopeTyp~",
                schema: "assets",
                table: "asset_scoped_access_grants",
                columns: new[] { "AssetReferenceId", "UserId", "ScopeType", "ScopeId" });

            migrationBuilder.CreateIndex(
                name: "IX_asset_scoped_access_grants_ExpiresAt",
                schema: "assets",
                table: "asset_scoped_access_grants",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_languages_Code",
                table: "languages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_languages_Name",
                table: "languages",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_launch_pad_applications_LaunchPadEventId_ProjectId",
                table: "launch_pad_applications",
                columns: new[] { "LaunchPadEventId", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_launch_pad_applications_ProjectId",
                table: "launch_pad_applications",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_launch_pad_applications_ProjectVersionId",
                table: "launch_pad_applications",
                column: "ProjectVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_launch_pad_applications_SubmittedByUserId",
                table: "launch_pad_applications",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_launch_pad_events_TenantId_Status_StartsAt",
                table: "launch_pad_events",
                columns: new[] { "TenantId", "Status", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_launch_pad_participant_registrations_LaunchPadParticipantSl~",
                table: "launch_pad_participant_registrations",
                columns: new[] { "LaunchPadParticipantSlotId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_launch_pad_participant_registrations_UserId",
                table: "launch_pad_participant_registrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_launch_pad_participant_slots_LaunchPadEventId_Role",
                table: "launch_pad_participant_slots",
                columns: new[] { "LaunchPadEventId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_project_member_allocations_ProjectId_UserId_ProjectTeamId",
                table: "project_member_allocations",
                columns: new[] { "ProjectId", "UserId", "ProjectTeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_member_allocations_ProjectTeamId",
                table: "project_member_allocations",
                column: "ProjectTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_project_member_allocations_UserId",
                table: "project_member_allocations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_project_milestones_ProjectId",
                table: "project_milestones",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_project_task_checklist_items_TaskId",
                table: "project_task_checklist_items",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_project_task_comments_TaskId",
                table: "project_task_comments",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_project_task_dependencies_DependsOnTaskId",
                table: "project_task_dependencies",
                column: "DependsOnTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_project_task_dependencies_TaskId_DependsOnTaskId",
                table: "project_task_dependencies",
                columns: new[] { "TaskId", "DependsOnTaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_task_label_assignments_LabelId",
                table: "project_task_label_assignments",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "IX_project_task_label_assignments_TaskId_LabelId",
                table: "project_task_label_assignments",
                columns: new[] { "TaskId", "LabelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_task_labels_ProjectId_Name",
                table: "project_task_labels",
                columns: new[] { "ProjectId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_team_agreements_ProjectId",
                table: "project_team_agreements",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_project_team_agreements_ProposingTeamId",
                table: "project_team_agreements",
                column: "ProposingTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_project_team_agreements_ReceivingTeamId",
                table: "project_team_agreements",
                column: "ReceivingTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_project_work_boards_ProjectId",
                table: "project_work_boards",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_work_columns_BoardId_Position",
                table: "project_work_columns",
                columns: new[] { "BoardId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_work_history_ProjectId_CreatedAt",
                table: "project_work_history",
                columns: new[] { "ProjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_project_work_tasks_AssigneeUserId",
                table: "project_work_tasks",
                column: "AssigneeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_project_work_tasks_ColumnId",
                table: "project_work_tasks",
                column: "ColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_project_work_tasks_MilestoneId",
                table: "project_work_tasks",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_project_work_tasks_ProjectId_ColumnId_Position",
                table: "project_work_tasks",
                columns: new[] { "ProjectId", "ColumnId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_resource_localizations_AssetReferenceId",
                table: "resource_localizations",
                column: "AssetReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_resource_localizations_FieldName",
                table: "resource_localizations",
                column: "FieldName");

            migrationBuilder.CreateIndex(
                name: "IX_resource_localizations_LanguageId",
                table: "resource_localizations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_resource_localizations_ResourceId",
                table: "resource_localizations",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_resource_localizations_ResourceId_FieldName_LanguageId",
                table: "resource_localizations",
                columns: new[] { "ResourceId", "FieldName", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_invitations_TeamId_InvitedEmail",
                table: "team_invitations",
                columns: new[] { "TeamId", "InvitedEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_team_invitations_TokenHash",
                table: "team_invitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transformed_assets_SourceContentId",
                schema: "assets",
                table: "transformed_assets",
                column: "SourceContentId");

            migrationBuilder.CreateIndex(
                name: "IX_TransformedAssets_LastAccessed",
                schema: "assets",
                table: "transformed_assets",
                column: "LastAccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TransformedAssets_Source_Transform",
                schema: "assets",
                table: "transformed_assets",
                columns: new[] { "SourceContentId", "TransformationSpec" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_launch_plans_launch_pad_applications_LaunchPadApplicationId",
                table: "launch_plans",
                column: "LaunchPadApplicationId",
                principalTable: "launch_pad_applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_launch_plans_launch_pad_events_LaunchPadEventId",
                table: "launch_plans",
                column: "LaunchPadEventId",
                principalTable: "launch_pad_events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_launch_plans_project_versions_ProjectVersionId",
                table: "launch_plans",
                column: "ProjectVersionId",
                principalTable: "project_versions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_launch_plans_launch_pad_applications_LaunchPadApplicationId",
                table: "launch_plans");

            migrationBuilder.DropForeignKey(
                name: "FK_launch_plans_launch_pad_events_LaunchPadEventId",
                table: "launch_plans");

            migrationBuilder.DropForeignKey(
                name: "FK_launch_plans_project_versions_ProjectVersionId",
                table: "launch_plans");

            migrationBuilder.DropTable(
                name: "asset_reference_revisions",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "asset_reports",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "asset_scoped_access_grants",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "launch_pad_applications");

            migrationBuilder.DropTable(
                name: "launch_pad_participant_registrations");

            migrationBuilder.DropTable(
                name: "project_member_allocations");

            migrationBuilder.DropTable(
                name: "project_task_checklist_items");

            migrationBuilder.DropTable(
                name: "project_task_comments");

            migrationBuilder.DropTable(
                name: "project_task_dependencies");

            migrationBuilder.DropTable(
                name: "project_task_label_assignments");

            migrationBuilder.DropTable(
                name: "project_team_agreements");

            migrationBuilder.DropTable(
                name: "project_work_history");

            migrationBuilder.DropTable(
                name: "resource_localizations");

            migrationBuilder.DropTable(
                name: "team_invitations");

            migrationBuilder.DropTable(
                name: "transformed_assets",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "launch_pad_participant_slots");

            migrationBuilder.DropTable(
                name: "project_task_labels");

            migrationBuilder.DropTable(
                name: "project_work_tasks");

            migrationBuilder.DropTable(
                name: "asset_references",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "languages");

            migrationBuilder.DropTable(
                name: "launch_pad_events");

            migrationBuilder.DropTable(
                name: "project_milestones");

            migrationBuilder.DropTable(
                name: "project_work_columns");

            migrationBuilder.DropTable(
                name: "asset_contents",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "asset_folders",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "project_work_boards");

            migrationBuilder.DropIndex(
                name: "IX_project_collaboration_teams_TenantId_Slug",
                table: "project_collaboration_teams");

            migrationBuilder.DropIndex(
                name: "IX_launch_plans_LaunchPadApplicationId",
                table: "launch_plans");

            migrationBuilder.DropIndex(
                name: "IX_launch_plans_LaunchPadEventId",
                table: "launch_plans");

            migrationBuilder.DropIndex(
                name: "IX_launch_plans_ProjectId",
                table: "launch_plans");

            migrationBuilder.DropIndex(
                name: "IX_launch_plans_ProjectVersionId",
                table: "launch_plans");

            migrationBuilder.DropColumn(
                name: "SubmittedAssetReferenceIdsJson",
                table: "testing_project_applications");

            migrationBuilder.DropColumn(
                name: "ParticipationMode",
                table: "project_teams");

            migrationBuilder.DropColumn(
                name: "IsPersonal",
                table: "project_collaboration_teams");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "project_collaboration_teams");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "project_collaboration_teams");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "project_collaboration_teams");

            migrationBuilder.DropColumn(
                name: "Authority",
                table: "project_collaboration_team_members");

            migrationBuilder.DropColumn(
                name: "LeftAt",
                table: "project_collaboration_team_members");

            migrationBuilder.DropColumn(
                name: "ProfessionalTitle",
                table: "project_collaboration_team_members");

            migrationBuilder.DropColumn(
                name: "LaunchPadApplicationId",
                table: "launch_plans");

            migrationBuilder.DropColumn(
                name: "LaunchPadEventId",
                table: "launch_plans");

            migrationBuilder.DropColumn(
                name: "ProjectVersionId",
                table: "launch_plans");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "project_teams",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "project_collaboration_team_members",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_project_collaboration_teams_Name",
                table: "project_collaboration_teams",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_launch_plans_ProjectId",
                table: "launch_plans",
                column: "ProjectId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }
    }
}
