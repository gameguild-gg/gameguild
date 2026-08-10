using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Testcontainers.PostgreSql;

namespace GameGuild.API.UnitTests.Database;

public sealed class EconomyProviderReversalPostgreSqlMigrationTests
{
    [Fact]
    public async Task MigrationInstallsRestrictedProviderReversalWriterOnPostgreSql()
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("economy_provider_reversal")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
        await container.StartAsync();

        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.GetConnectionString())
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT count(*)
            FROM pg_proc procedure
            JOIN pg_namespace schema ON schema.oid = procedure.pronamespace
            WHERE schema.nspname = 'economy_private'
              AND procedure.proname = 'post_provider_reversal_v2'
              AND has_function_privilege('gameguild_economy_writer', procedure.oid, 'EXECUTE');
            """, connection);

        Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(1);
        command.CommandText = """
            SELECT count(*)
            FROM pg_proc procedure
            JOIN pg_namespace schema ON schema.oid = procedure.pronamespace
            WHERE schema.nspname = 'economy_private'
              AND procedure.proname = 'post_provider_reversal_v1'
              AND has_function_privilege('gameguild_economy_writer', procedure.oid, 'EXECUTE');
            """;
        Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(0);
    }
}
