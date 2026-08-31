using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class EconomyPayoutPostgreSqlMigrationTests
{
    private static readonly Guid WalletId = Guid.Parse("96000000-0000-0000-0000-000000000001");
    private static readonly Guid ActorId = Guid.Parse("96000000-0000-0000-0000-000000000002");
    private static readonly Guid PayeeId = Guid.Parse("96000000-0000-0000-0000-000000000003");
    private static readonly Guid OperationId = Guid.Parse("96000000-0000-0000-0000-000000000004");
    private static readonly Guid RiskDecisionId = Guid.Parse("96000000-0000-0000-0000-000000000005");
    private static readonly Guid TenantId = Guid.Parse("96000000-0000-0000-0000-000000000006");
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 9, 18, 0, 0, TimeSpan.Zero);

    [DockerFact]
    public async Task MigrationPersistsPayoutThroughProceduresAndRejectsDirectWriterMutation()
    {
        await using var container = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_payout");

        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.ConnectionString)
                .Options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(container.ConnectionString);
        await connection.OpenAsync();
        await SeedWalletAsync(connection);

        (await ScalarAsync<string?>(connection,
            "SELECT to_regprocedure('economy_private.create_payout_operation_v2(uuid,uuid,text,text,uuid,uuid,uuid,bigint,text,text,text,text,text,text,integer,bigint,bigint,bigint,bigint,bigint,bigint,uuid,timestamptz,timestamptz)')::text;"))
            .Should().NotBeNull();

        var directInsert = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsRoleAsync(
            connection,
            "gameguild_economy_writer",
            $"INSERT INTO public.economy_payout_operations (\"Id\") VALUES ('{OperationId}');"));
        directInsert.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);

        await ExecuteAsRoleAsync(connection, "gameguild_economy_writer", $"""
            SELECT economy_private.create_payout_operation_v2(
                '{OperationId}', '{TenantId}', 'payout:1', 'request-hash', '{ActorId}', '{PayeeId}', '{WalletId}', 750,
                'acct_1', 'destination-hash', 'binding-hash', 'eligibility-hash', NULL, NULL, 1, 1, 1, 1,
                1, 1, 1, '{RiskDecisionId}', '{CreatedAt:O}', '{CreatedAt:O}');
            """);

        await ExecuteAsRoleAsync(connection, "gameguild_economy_writer", $"""
            SELECT economy_private.transition_payout_operation_v1(
                '{OperationId}', 1, 2, 'dispatch-hash', NULL, '{CreatedAt.AddMinutes(1):O}');
            """);

        await ExecuteAsRoleAsync(connection, "gameguild_economy_writer", $"""
            SELECT economy_private.complete_payout_provider_event_v1(
                'provider-event-1', 'provider-hash-1', '{OperationId}', 2, 4, 'provider-payout-1',
                '{CreatedAt.AddMinutes(2):O}');
            """);

        (await ScalarAsync<int>(connection, $"""
            SELECT "State" FROM economy_private.read_payout_operation_for_tenant_v2('{TenantId}', '{OperationId}');
            """)).Should().Be(4);
        (await ScalarAsync<string>(connection, $"""
            SELECT "EventHash" FROM economy_private.read_payout_provider_event_v1('provider-event-1');
            """)).Should().Be("provider-hash-1");

        var directEventUpdate = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsRoleAsync(
            connection,
            "gameguild_economy_writer",
            "UPDATE public.economy_payout_provider_events SET \"EventHash\" = 'rewritten';"));
        directEventUpdate.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
    }

    [DockerFact]
    public async Task MigrationRollsBackPayoutFunctionsAndTables()
    {
        await using var container = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_payout_rollback");

        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.ConnectionString)
                .Options);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync();

        await using var connection = new NpgsqlConnection(container.ConnectionString);
        await connection.OpenAsync();
        (await ScalarAsync<string?>(connection,
            "SELECT to_regclass('public.economy_payout_operations')::text;"))
            .Should().Be("economy_payout_operations");

        await migrator.MigrateAsync("20260809160000_AddEconomyAdminWithdrawalPersistence");

        (await ScalarAsync<string?>(connection,
            "SELECT to_regclass('public.economy_payout_operations')::text;"))
            .Should().BeNull();
        (await ScalarAsync<string?>(connection,
            "SELECT to_regprocedure('economy_private.create_payout_operation_v1(uuid,text,text,uuid,uuid,uuid,bigint,text,text,text,text,text,text,integer,bigint,bigint,bigint,bigint,bigint,bigint,uuid,timestamptz,timestamptz)')::text;"))
            .Should().BeNull();
    }

    private static Task SeedWalletAsync(NpgsqlConnection connection) => ExecuteAsync(connection, $"""
        INSERT INTO public.economy_wallets ("Id", "OwnerId", "TenantId", "State", "CreatedAt")
        VALUES ('{WalletId}', '{PayeeId}', '{TenantId}', 1, '{CreatedAt:O}');
        """);

    private static async Task ExecuteAsRoleAsync(NpgsqlConnection connection, string role, string sql)
    {
        await ExecuteAsync(connection, $"SET ROLE {role};");
        try
        {
            await ExecuteAsync(connection, sql);
        }
        finally
        {
            await ExecuteAsync(connection, "RESET ROLE;");
        }
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? default! : (T)value;
    }

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS"), "1", StringComparison.Ordinal))
                Skip = "Docker tests disabled by SKIP_DOCKER_TESTS=1.";
        }
    }
}
