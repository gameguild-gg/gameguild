using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811102000_AllowBountyFifoReservationPurpose")]
public partial class AllowBountyFifoReservationPurpose : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.economy_fragment_reservations
                DROP CONSTRAINT IF EXISTS ck_economy_fragment_reservations_state;
            ALTER TABLE public.economy_fragment_reservations
                ADD CONSTRAINT ck_economy_fragment_reservations_state
                CHECK ("Purpose" BETWEEN 1 AND 6 AND "Status" BETWEEN 1 AND 3);
            """);

        InstallBountyFifoReservationWriter(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemoveBountyFifoReservationWriter(migrationBuilder);

        migrationBuilder.Sql(
            """
            ALTER TABLE public.economy_fragment_reservations
                DROP CONSTRAINT IF EXISTS ck_economy_fragment_reservations_state;
            ALTER TABLE public.economy_fragment_reservations
                ADD CONSTRAINT ck_economy_fragment_reservations_state
                CHECK ("Purpose" BETWEEN 1 AND 5 AND "Status" BETWEEN 1 AND 3);
            """);
    }
}
