using System.Text.RegularExpressions;
using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Testcontainers.PostgreSql;

namespace GameGuild.API.UnitTests.Database;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class EconomyPayoutRequestPostgreSqlMigrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly Regex SqlFormatItem = new(@"\{(?<index>\d+)(?:,[^}:]+)?(?::[^}]*)?\}", RegexOptions.CultureInvariant);

    [DockerFact]
    public async Task WriterPersistsAnOwnedRequestAndOnlyAllowsThePayeeToCancelIt()
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("economy_payout_requests")
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
        var payeeId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        await SeedWalletAsync(connection, payeeId, walletId);

        var directInsert = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsWriterAsync(
            connection,
            $"""INSERT INTO public.economy_payout_requests ("Id") VALUES ({requestId});"""));
        directInsert.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);

        await ExecuteAsWriterAsync(connection, $"""
            SELECT economy_private.create_payout_request_v1(
                {requestId}, {"request-1"}, {new string('a', 64)}, {payeeId}, {walletId}, {250}, {1}, {1},
                {Now}, {Now});
            """);

        (await ScalarAsync<long>(connection, $"""
            SELECT "AmountUnits"
            FROM economy_private.read_payout_request_by_id_for_payee_v1({requestId}, {payeeId});
            """)).Should().Be(250);

        await ExecuteAsWriterAsync(connection, $"""
            SELECT economy_private.transition_payout_request_v1(
                {requestId}, {payeeId}, {1}, {2}, {Now.AddMinutes(1)});
            """);

        (await ScalarAsync<int>(connection, $"""
            SELECT "State"
            FROM economy_private.read_payout_request_by_id_for_payee_v1({requestId}, {payeeId});
            """)).Should().Be(2);
        (await ScalarAsync<long>(connection, $"""
            SELECT "Version"
            FROM economy_private.read_payout_request_by_id_for_payee_v1({requestId}, {payeeId});
            """)).Should().Be(2);

        var foreignCancellation = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsWriterAsync(
            connection,
            $"""SELECT economy_private.transition_payout_request_v1({requestId}, {Guid.NewGuid()}, {2}, {2}, {Now.AddMinutes(2)});"""));
        foreignCancellation.SqlState.Should().Be("P0002");
    }

    [DockerFact]
    public async Task WriterRejectsARequestForAnotherPayeesWallet()
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("economy_payout_request_ownership")
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
        var ownerId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        await SeedWalletAsync(connection, ownerId, walletId);

        var foreignRequest = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsWriterAsync(
            connection,
            $"""
            SELECT economy_private.create_payout_request_v1(
                {Guid.NewGuid()}, {"request-foreign"}, {new string('b', 64)}, {Guid.NewGuid()}, {walletId}, {250}, {1}, {1},
                {Now}, {Now});
            """));

        foreignRequest.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
        foreignRequest.MessageText.Should().Contain("does not belong");
    }

    [DockerFact]
    public async Task WriterScopesIdempotencyKeysToThePayee()
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("economy_payout_request_idempotency")
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
        var firstPayee = Guid.NewGuid();
        var firstWallet = Guid.NewGuid();
        var secondPayee = Guid.NewGuid();
        var secondWallet = Guid.NewGuid();
        await SeedWalletAsync(connection, firstPayee, firstWallet);
        await SeedWalletAsync(connection, secondPayee, secondWallet);

        await ExecuteAsWriterAsync(connection, $"""
            SELECT economy_private.create_payout_request_v1(
                {Guid.NewGuid()}, {"same-key"}, {new string('a', 64)}, {firstPayee}, {firstWallet}, {250}, {1}, {1},
                {Now}, {Now});
            SELECT economy_private.create_payout_request_v1(
                {Guid.NewGuid()}, {"same-key"}, {new string('b', 64)}, {secondPayee}, {secondWallet}, {250}, {1}, {1},
                {Now}, {Now});
            """);

        (await ScalarAsync<long>(connection, $"""
            SELECT COUNT(*)
            FROM economy_private.read_payout_request_by_idempotency_v1({firstPayee}, {"same-key"});
            """)).Should().Be(1);
        (await ScalarAsync<long>(connection, $"""
            SELECT COUNT(*)
            FROM economy_private.read_payout_request_by_idempotency_v1({secondPayee}, {"same-key"});
            """)).Should().Be(1);
    }

    private static Task SeedWalletAsync(NpgsqlConnection connection, Guid ownerId, Guid walletId) => ExecuteAsync(connection, $"""
        INSERT INTO public.economy_wallets ("Id", "OwnerId", "TenantId", "State", "CreatedAt")
        VALUES ({walletId}, {ownerId}, {Guid.NewGuid()}, {1}, {Now});
        """);

    private static async Task ExecuteAsWriterAsync(NpgsqlConnection connection, FormattableString sql)
    {
        await ExecuteLiteralAsync(connection, "SET ROLE gameguild_economy_writer;");
        try
        {
            await ExecuteAsync(connection, sql);
        }
        finally
        {
            await ExecuteLiteralAsync(connection, "RESET ROLE;");
        }
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, FormattableString sql)
    {
        await using var command = CreateCommand(connection, sql);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, FormattableString sql)
    {
        await using var command = CreateCommand(connection, sql);
        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? default! : (T)value;
    }

    private static async Task ExecuteLiteralAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static NpgsqlCommand CreateCommand(NpgsqlConnection connection, FormattableString sql)
    {
        var command = new NpgsqlCommand
        {
            Connection = connection,
            CommandText = SqlFormatItem.Replace(sql.Format, static match => $"@p{match.Groups["index"].Value}")
        };
        var arguments = sql.GetArguments();
        for (var index = 0; index < arguments.Length; index++)
        {
            command.Parameters.AddWithValue($"p{index}", arguments[index] ?? DBNull.Value);
        }

        return command;
    }

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS"), "1", StringComparison.Ordinal))
            {
                Skip = "Docker tests disabled by SKIP_DOCKER_TESTS=1.";
            }
        }
    }
}