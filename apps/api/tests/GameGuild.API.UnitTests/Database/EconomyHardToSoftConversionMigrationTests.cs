using FluentAssertions;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace GameGuild.API.UnitTests.Database;

public sealed class EconomyHardToSoftConversionMigrationTests
{
    [Fact]
    public void MigrationInstallsAtomicConversionWriter()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedMigration().BuildUp(builder);
        var sql = string.Join('\n', builder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));

        builder.Operations.OfType<CreateTableOperation>()
            .Should().Contain(operation => operation.Name == "economy_hard_to_soft_conversion_operations");
        sql.Should().Contain("post_hard_to_soft_conversion_v1");
        sql.Should().Contain("reserve_fifo_fragments_v1");
        sql.Should().Contain("economy_lot_lineage_edges");
        sql.Should().Contain("economy_fragment_root_ranges");
        sql.Should().Contain("transition_fifo_fragment_reservations_v1");
        sql.Should().Contain("rebuild_wallet_projection_v1");
        sql.Should().Contain("SECURITY DEFINER");
    }

    private sealed class ExposedMigration : AddEconomyHardToSoftConversionWriter
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);
    }
}
