using FluentAssertions;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace GameGuild.API.UnitTests.Database;

public sealed class EconomyBountyFifoReservationMigrationTests
{
    [Fact]
    public void MigrationAddsDedicatedBountyReservationPurposeAndWriter()
    {
        var up = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var down = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var migration = new ExposedMigration();

        migration.BuildUp(up);
        migration.BuildDown(down);

        var upSql = string.Join('\n', up.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
        upSql.Should().Contain("\"Purpose\" BETWEEN 1 AND 6");
        upSql.Should().Contain("reserve_bounty_fifo_fragments_v1");
        upSql.Should().Contain("p_purpose <> 6");
        upSql.Should().Contain("SECURITY DEFINER");
        upSql.Should().Contain("gameguild_economy_writer");

        var downSql = string.Join('\n', down.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
        downSql.Should().Contain("DROP FUNCTION IF EXISTS economy_private.reserve_bounty_fifo_fragments_v1");
        downSql.Should().Contain("\"Purpose\" BETWEEN 1 AND 5");
    }

    private sealed class ExposedMigration : AllowBountyFifoReservationPurpose
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);
        public void BuildDown(MigrationBuilder builder) => Down(builder);
    }
}
