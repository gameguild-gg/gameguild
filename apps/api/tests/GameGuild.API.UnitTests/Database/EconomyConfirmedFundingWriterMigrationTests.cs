using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace GameGuild.API.UnitTests.Database;

public sealed class EconomyConfirmedFundingWriterMigrationTests
{
    [Fact]
    public void MigrationInstallsAtomicObservedFundingAndConfirmationProcedures()
    {
        var up = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var down = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var migration = new ExposedMigration();

        migration.BuildUp(up);
        migration.BuildDown(down);

        var sql = string.Join('\n', up.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
        sql.Should().Contain("observe_hard_coin_top_up_v1");
        sql.Should().Contain("confirm_observed_hard_coin_top_up_v1");
        sql.Should().Contain("post_registered_posting_v1");
        sql.Should().Contain("rebuild_wallet_projection_v1");
        sql.Should().Contain("economy_provider_fact_allocations");
        sql.Should().Contain("economy_outbox_messages");
        sql.Should().Contain("SECURITY DEFINER");
        sql.Should().Contain("SET search_path = pg_catalog, economy_private");
        sql.Should().NotContain("GRANT ALL ON TABLE public.economy_funding_claims TO gameguild_economy_writer");

        var downSql = string.Join('\n', down.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
        downSql.Should().Contain("DROP FUNCTION IF EXISTS economy_private.confirm_observed_hard_coin_top_up_v1");
        downSql.Should().Contain("DROP FUNCTION IF EXISTS economy_private.observe_hard_coin_top_up_v1");
    }

    [DockerFact]
    public async Task Migration_InstallsWriterProceduresWithRestrictedExecutionOnPostgreSql()
    {
        await using var container = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_confirmed_funding");

        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.ConnectionString)
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(container.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT count(*)
            FROM pg_proc procedure
            JOIN pg_namespace schema ON schema.oid = procedure.pronamespace
            WHERE schema.nspname = 'economy_private'
              AND procedure.proname IN ('observe_hard_coin_top_up_v1', 'confirm_observed_hard_coin_top_up_v1')
              AND has_function_privilege('gameguild_economy_writer', procedure.oid, 'EXECUTE');
            """, connection);

        (Convert.ToInt64(await command.ExecuteScalarAsync())).Should().Be(2);
    }

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS"), "1", StringComparison.Ordinal))
                Skip = "Docker tests disabled by SKIP_DOCKER_TESTS=1.";
        }
    }

    private sealed class ExposedMigration : AddEconomyConfirmedFundingWriter
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);

        public void BuildDown(MigrationBuilder builder) => Down(builder);
    }
}
