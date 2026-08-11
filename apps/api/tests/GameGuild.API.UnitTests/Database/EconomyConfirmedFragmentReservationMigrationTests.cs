using FluentAssertions;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace GameGuild.API.UnitTests.Database;

public sealed class EconomyConfirmedFragmentReservationMigrationTests
{
    [Fact]
    public void MigrationRejectsFragmentReservationsFromUnconfirmedSources()
    {
        var up = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var down = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var migration = new ExposedMigration();

        migration.BuildUp(up);
        migration.BuildDown(down);

        var upSql = string.Join('\n', up.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
        upSql.Should().Contain("enforce_confirmed_fragment_reservation_source_v1");
        upSql.Should().Contain("SECURITY DEFINER");
        upSql.Should().Contain("source_stamp.\"State\" = 2");
        upSql.Should().Contain("source_stamp.\"ConfirmedAt\" IS NOT NULL");
        upSql.Should().Contain("source_stamp.\"ConfirmedAt\" <= NEW.\"ReservedAt\"");
        upSql.Should().Contain("tr_economy_fragment_reservations_require_confirmed_source");
        upSql.Should().Contain("BEFORE INSERT OR UPDATE OF \"RootSourceStampId\", \"ReservedAt\"");
        upSql.Should().Contain("REVOKE ALL ON FUNCTION");

        var downSql = string.Join('\n', down.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
        downSql.Should().Contain("DROP TRIGGER IF EXISTS tr_economy_fragment_reservations_require_confirmed_source");
        downSql.Should().Contain("DROP FUNCTION IF EXISTS economy_private.enforce_confirmed_fragment_reservation_source_v1");
    }

    private sealed class ExposedMigration : RequireConfirmedFragmentReservationSources
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);

        public void BuildDown(MigrationBuilder builder) => Down(builder);
    }
}
