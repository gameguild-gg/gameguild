using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811103500_SecureBountyEscrowPersistence")]
public partial class SecureBountyEscrowPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.economy_bounties
                ADD COLUMN IF NOT EXISTS "RequestHash" character varying(128) NULL;
            """);

        InstallBountyEscrowPersistenceSecurity(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemoveBountyEscrowPersistenceSecurity(migrationBuilder);

        migrationBuilder.Sql(
            """
            ALTER TABLE public.economy_bounties
                DROP COLUMN IF EXISTS "RequestHash";
            """);
    }
}
