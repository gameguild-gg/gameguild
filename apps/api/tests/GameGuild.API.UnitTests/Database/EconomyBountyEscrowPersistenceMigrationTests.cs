using FluentAssertions;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace GameGuild.API.UnitTests.Database;

public sealed class EconomyBountyEscrowPersistenceMigrationTests
{
    [Fact]
    public void MigrationSecuresBountyWritesAndBindsThemToFifoReservations()
    {
        var up = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var down = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var migration = new ExposedMigration();

        migration.BuildUp(up);
        migration.BuildDown(down);

        var upSql = string.Join('\n', up.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
        upSql.Should().Contain("\"RequestHash\"");
        upSql.Should().Contain("create_bounty_escrow_v1");
        upSql.Should().Contain("read_bounty_escrow_by_id_v1");
        upSql.Should().Contain("public.economy_fragment_reservations", "the durable bounty must consume persisted FIFO reservations");
        upSql.Should().Contain("\"Purpose\" = 6");
        upSql.Should().Contain("SECURITY DEFINER");
        upSql.Should().Contain("REVOKE ALL ON TABLE public.economy_bounties");
        upSql.Should().Contain("gameguild_economy_writer");

        var downSql = string.Join('\n', down.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
        downSql.Should().Contain("DROP FUNCTION IF EXISTS economy_private.create_bounty_escrow_v1");
        downSql.Should().Contain("DROP COLUMN IF EXISTS \"RequestHash\"");
    }

    private sealed class ExposedMigration : SecureBountyEscrowPersistence
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);
        public void BuildDown(MigrationBuilder builder) => Down(builder);
    }
}
