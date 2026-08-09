using FluentAssertions;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace GameGuild.API.UnitTests.Database;

public sealed class EconomyHardToSoftConversionAuthorizationGateTests
{
    [Fact]
    public void MigrationBindsConversionReservationsToAuthorizedRoots()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedMigration().BuildUp(builder);
        var sql = string.Join('\n', builder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));

        sql.Should().Contain("post_authorized_hard_to_soft_conversion_v1");
        sql.Should().Contain("p_authorized_root_ids uuid[]");
        sql.Should().Contain("reserve_fifo_fragments_v1");
        sql.Should().Contain("conversion source roots do not match the authorization");
        sql.Should().Contain("SECURITY DEFINER");
        sql.Should().Contain("gameguild_economy_writer");
    }

    private sealed class ExposedMigration : AddEconomyHardToSoftConversionAuthorizationGate
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);
    }
}
