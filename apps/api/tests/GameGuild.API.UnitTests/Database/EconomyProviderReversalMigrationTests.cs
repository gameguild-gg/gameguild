using FluentAssertions;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace GameGuild.API.UnitTests.Database;

public sealed class EconomyProviderReversalMigrationTests
{
    [Fact]
    public void MigrationInstallsAnAtomicProviderReversalWriter()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedMigration().BuildUp(builder);
        var sql = string.Join('\n', builder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));

        builder.Operations.OfType<CreateTableOperation>()
            .Should().Contain(operation => operation.Name == "economy_provider_reversal_operations");
        sql.Should().Contain("post_provider_reversal_v1");
        sql.Should().Contain("economy_root_reversal_states");
        sql.Should().Contain("FOR UPDATE");
        sql.Should().Contain("economy_entry_allocations");
        sql.Should().Contain("economy_fragment_root_ranges");
        sql.Should().Contain("economy_wallet_debts");
        sql.Should().Contain("economy_wallet_debt_events");
        sql.Should().Contain("rebuild_wallet_projection_v1");
        sql.Should().Contain("SECURITY DEFINER");
        sql.Should().Contain("gameguild_economy_writer");

        var hardenedBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedHardeningMigration().BuildUp(hardenedBuilder);
        var hardenedSql = string.Join("\n", hardenedBuilder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
        hardenedSql.Should().Contain("post_provider_reversal_v2");
        hardenedSql.Should().Contain("REVOKE EXECUTE ON FUNCTION economy_private.post_provider_reversal_v1");
        hardenedSql.Should().Contain("economy_risk_decision_consumptions");
        hardenedSql.Should().Contain("economy_provider_reversal_fragments");
    }

    private sealed class ExposedMigration : AddEconomyProviderReversalWriter
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);
    }
    private sealed class ExposedHardeningMigration : HardenEconomyProviderReversalWriter
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);
    }

}
