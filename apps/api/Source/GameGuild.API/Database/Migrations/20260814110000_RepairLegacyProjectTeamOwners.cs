using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260814110000_RepairLegacyProjectTeamOwners")]
public partial class RepairLegacyProjectTeamOwners : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            WITH owner_teams AS (
                SELECT DISTINCT ON (pt."TeamId")
                       pt."TeamId" AS team_id,
                       pt."ProjectId" AS project_id,
                       COALESCE(pt."TenantId", p."TenantId") AS tenant_id,
                       p."CreatedById" AS project_creator_id
                FROM project_teams pt
                JOIN projects p ON p."Id" = pt."ProjectId"
                WHERE pt."Role" = 'Owner'
                  AND pt."IsActive" = TRUE
                  AND pt."EndedAt" IS NULL
                  AND pt."DeletedAt" IS NULL
                  AND p."DeletedAt" IS NULL
                ORDER BY pt."TeamId", pt."AssignedAt", pt."Id"
            ), candidate AS (
                SELECT owner_teams.*,
                       COALESCE(
                           (
                               SELECT owner_teams.project_creator_id
                               FROM "TenantMembers" tenant_member
                               WHERE owner_teams.project_creator_id IS NOT NULL
                                 AND tenant_member."UserId" = owner_teams.project_creator_id
                                 AND tenant_member."TenantId" = owner_teams.tenant_id
                                 AND tenant_member."IsActive" = TRUE
                                 AND tenant_member."LeftAt" IS NULL
                                 AND tenant_member."DeletedAt" IS NULL
                               LIMIT 1
                           ),
                           (
                               SELECT pv."CreatedById"
                               FROM project_versions pv
                               JOIN "TenantMembers" tenant_member
                                 ON tenant_member."UserId" = pv."CreatedById"
                                AND tenant_member."TenantId" = owner_teams.tenant_id
                                AND tenant_member."IsActive" = TRUE
                                AND tenant_member."LeftAt" IS NULL
                                AND tenant_member."DeletedAt" IS NULL
                               WHERE pv."ProjectId" = owner_teams.project_id
                                 AND pv."DeletedAt" IS NULL
                                 AND pv."TenantId" IS NOT DISTINCT FROM owner_teams.tenant_id
                               ORDER BY pv."CreatedAt", pv."Id"
                               LIMIT 1
                           ),
                           (
                               SELECT pc."UserId"
                               FROM "ProjectCollaborators" pc
                               JOIN "TenantMembers" tenant_member
                                 ON tenant_member."UserId" = pc."UserId"
                                AND tenant_member."TenantId" = owner_teams.tenant_id
                                AND tenant_member."IsActive" = TRUE
                                AND tenant_member."LeftAt" IS NULL
                                AND tenant_member."DeletedAt" IS NULL
                               WHERE pc."ProjectId" = owner_teams.project_id
                                 AND pc."IsActive" = TRUE
                                 AND pc."LeftAt" IS NULL
                                 AND pc."DeletedAt" IS NULL
                                 AND pc."TenantId" IS NOT DISTINCT FROM owner_teams.tenant_id
                               ORDER BY CASE WHEN lower(pc."Role") IN ('owner', 'lead') THEN 0 ELSE 1 END,
                                        pc."JoinedAt", pc."Id"
                               LIMIT 1
                           )
                       ) AS user_id
                FROM owner_teams
            ), repair AS (
                SELECT candidate.*,
                       md5(candidate.team_id::text || ':' || candidate.user_id::text || ':repair-owner-member') AS member_hash
                FROM candidate
                WHERE candidate.user_id IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM project_collaboration_team_members tm
                      WHERE tm."TeamId" = candidate.team_id
                        AND tm."Authority" = 'Owner'
                        AND tm."IsActive" = TRUE
                        AND tm."LeftAt" IS NULL
                        AND tm."DeletedAt" IS NULL
                        AND tm."TenantId" IS NOT DISTINCT FROM candidate.tenant_id
                  )
            )
            INSERT INTO project_collaboration_team_members
                ("Id", "TeamId", "UserId", "Authority", "ProfessionalTitle", "JoinedAt", "LeftAt", "IsActive",
                 "TenantId", "CreatedAt", "UpdatedAt", "DeletedAt", "Version")
            SELECT (substr(member_hash,1,8) || '-' || substr(member_hash,9,4) || '-' || substr(member_hash,13,4) || '-' ||
                    substr(member_hash,17,4) || '-' || substr(member_hash,21,12))::uuid,
                   team_id, user_id, 'Owner', NULL, CURRENT_TIMESTAMP, NULL, TRUE,
                   tenant_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, NULL, 0
            FROM repair
            ON CONFLICT ("TeamId", "UserId") DO UPDATE
            SET "Authority" = 'Owner',
                "IsActive" = TRUE,
                "LeftAt" = NULL,
                "DeletedAt" = NULL,
                "TenantId" = EXCLUDED."TenantId",
                "UpdatedAt" = CURRENT_TIMESTAMP;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data repair is intentionally retained on rollback.
    }
}
