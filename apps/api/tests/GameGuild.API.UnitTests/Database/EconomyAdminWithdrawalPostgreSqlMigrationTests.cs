using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace GameGuild.API.UnitTests.Database;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class EconomyAdminWithdrawalPostgreSqlMigrationTests
{
    private static readonly Guid WalletId = Guid.Parse("95000000-0000-0000-0000-000000000001");
    private static readonly Guid RequestedBy = Guid.Parse("95000000-0000-0000-0000-000000000002");
    private static readonly Guid ApprovedBy = Guid.Parse("95000000-0000-0000-0000-000000000003");
    private static readonly Guid RunId = Guid.Parse("95000000-0000-0000-0000-000000000004");
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 9, 12, 30, 0, TimeSpan.Zero);

    [DockerFact]
    public async Task MigrationPersistsWithdrawalThroughProceduresAndRejectsDirectWriterMutation()
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("economy_admin_withdrawal")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
        await container.StartAsync();

        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.GetConnectionString())
                .Options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        await SeedWalletAsync(connection);

        (await ScalarAsync<string?>(connection,
            "SELECT to_regprocedure('economy_private.create_admin_withdrawal_run_v1(uuid,text,text,date,uuid,uuid,bigint,text,text,integer,bigint,bigint,bigint,bigint,bigint,bigint,text,text,timestamptz,timestamptz)')::text;"))
            .Should().NotBeNull();
        (await ScalarAsync<string?>(connection,
            "SELECT to_regprocedure('economy_private.append_admin_withdrawal_audit_event_v1(uuid,text,uuid,text,timestamptz)')::text;"))
            .Should().NotBeNull();

        var directInsert = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsRoleAsync(
            connection,
            "gameguild_economy_writer",
            $"INSERT INTO public.economy_admin_withdrawal_runs (\"Id\") VALUES ('{RunId}');"));
        directInsert.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);

        await ExecuteAsRoleAsync(connection, "gameguild_economy_writer", $"""
            SELECT economy_private.create_admin_withdrawal_run_v1(
                '{RunId}', 'withdrawal:2026-08', 'request-hash', '2026-08-01',
                '{RequestedBy}', '{WalletId}', 750, 'reserve:hard:primary', 'destination-hash',
                1, 1, 1, 1, 1, 1, 1, NULL, NULL, '{CreatedAt:O}', '{CreatedAt:O}');
            """);

        var created = await ScalarAsync<int>(connection, $"""
            SELECT "State" FROM economy_private.read_admin_withdrawal_run_by_id_v1('{RunId}');
            """);
        created.Should().Be(1);

        await ExecuteAsRoleAsync(connection, "gameguild_economy_writer", $"""
            SELECT economy_private.transition_admin_withdrawal_run_v1(
                '{RunId}', 1, 2, '{ApprovedBy}', NULL, NULL, '{CreatedAt.AddMinutes(1):O}');
            """);
        await ExecuteAsRoleAsync(connection, "gameguild_economy_writer", $"""
            SELECT economy_private.transition_admin_withdrawal_run_v1(
                '{RunId}', 2, 3, '{ApprovedBy}', 'dispatch-hash', NULL, '{CreatedAt.AddMinutes(2):O}');
            """);
        await ExecuteAsRoleAsync(connection, "gameguild_economy_writer", $"""
            SELECT * FROM economy_private.append_admin_withdrawal_audit_event_v1(
                '{RunId}', 'dispatching', '{ApprovedBy}', 'dispatch-hash', '{CreatedAt.AddMinutes(2):O}');
            """);

        var auditRows = await ScalarAsync<long>(connection, $"""
            SELECT count(*) FROM economy_private.read_admin_withdrawal_audit_events_v1('{RunId}');
            """);
        auditRows.Should().Be(1);

        var directAuditUpdate = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsRoleAsync(
            connection,
            "gameguild_economy_writer",
            $"UPDATE public.economy_admin_withdrawal_audit_events SET \"Evidence\" = 'rewritten' WHERE \"RunId\" = '{RunId}';"));
        directAuditUpdate.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);

        await ExecuteAsRoleAsync(connection, "gameguild_economy_writer", $"""
            SELECT economy_private.complete_admin_withdrawal_provider_event_v1(
                'provider-event-1', 'provider-hash-1', '{RunId}', 3, 5, 'provider-transfer-1',
                '{CreatedAt.AddMinutes(3):O}');
            """);

        (await ScalarAsync<int>(connection, $"""
            SELECT "State" FROM economy_private.read_admin_withdrawal_run_by_id_v1('{RunId}');
            """)).Should().Be(5);
        (await ScalarAsync<string>(connection, $"""
            SELECT "EventHash" FROM economy_private.read_admin_withdrawal_provider_event_v1('provider-event-1');
            """)).Should().Be("provider-hash-1");
    }


    [DockerFact]
    public async Task MigrationRollsBackWithdrawalFunctionsAndTables()
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("economy_admin_withdrawal_rollback")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
        await container.StartAsync();

        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.GetConnectionString())
                .Options);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync();

        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        (await ScalarAsync<string?>(connection,
            "SELECT to_regclass('public.economy_admin_withdrawal_runs')::text;"))
            .Should().Be("economy_admin_withdrawal_runs");

        await migrator.MigrateAsync("20260803050213_HardenEconomyPostingWriter");

        (await ScalarAsync<string?>(connection,
            "SELECT to_regclass('public.economy_admin_withdrawal_runs')::text;"))
            .Should().BeNull();
        (await ScalarAsync<string?>(connection,
            "SELECT to_regprocedure('economy_private.create_admin_withdrawal_run_v1(uuid,text,text,date,uuid,uuid,bigint,text,text,integer,bigint,bigint,bigint,bigint,bigint,bigint,text,text,timestamptz,timestamptz)')::text;"))
            .Should().BeNull();
    }

    private static Task SeedWalletAsync(NpgsqlConnection connection) => ExecuteAsync(connection, $"""
        INSERT INTO public.economy_wallets ("Id", "OwnerId", "TenantId", "State", "CreatedAt")
        VALUES ('{WalletId}', '{RequestedBy}', '95000000-0000-0000-0000-000000000005', 1, '{CreatedAt:O}');
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
