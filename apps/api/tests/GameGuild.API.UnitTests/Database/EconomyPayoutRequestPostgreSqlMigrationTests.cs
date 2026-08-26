using System.Text.RegularExpressions;
using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class EconomyPayoutRequestPostgreSqlMigrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly Regex SqlFormatItem = new(@"\{(?<index>\d+)(?:,[^}:]+)?(?::[^}]*)?\}", RegexOptions.CultureInvariant);

    [DockerFact]
    public async Task WriterPersistsAnOwnedRequestAndOnlyAllowsThePayeeToCancelIt()
    {
        await using var container = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_payout_requests");

        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.ConnectionString)
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(container.ConnectionString);
        await connection.OpenAsync();
        var payeeId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await SeedWalletAsync(connection, payeeId, walletId, tenantId);

        var directInsert = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsWriterAsync(
            connection,
            $"""INSERT INTO public.economy_payout_requests ("Id") VALUES ({requestId});"""));
        directInsert.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);

        await ExecuteAsWriterAsync(connection, $"""
            SELECT economy_private.create_payout_request_v3(
                {requestId}, {tenantId}, {"request-1"}, {new string('a', 64)}, {payeeId}, {walletId}, {250}, {1}, {1},
                {Now}, {Now});
            """);

        (await ScalarAsync<long>(connection, $"""
            SELECT "AmountUnits"
            FROM economy_private.read_payout_request_by_id_for_payee_v3({tenantId}, {requestId}, {payeeId});
            """)).Should().Be(250);

        await ExecuteAsWriterAsync(connection, $"""
            SELECT economy_private.transition_payout_request_v3(
                {tenantId}, {requestId}, {payeeId}, {1}, {2}, {Now.AddMinutes(1)});
            """);

        (await ScalarAsync<int>(connection, $"""
            SELECT "State"
            FROM economy_private.read_payout_request_by_id_for_payee_v3({tenantId}, {requestId}, {payeeId});
            """)).Should().Be(2);
        (await ScalarAsync<long>(connection, $"""
            SELECT "Version"
            FROM economy_private.read_payout_request_by_id_for_payee_v3({tenantId}, {requestId}, {payeeId});
            """)).Should().Be(2);

        var foreignCancellation = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsWriterAsync(
            connection,
            $"""SELECT economy_private.transition_payout_request_v3({tenantId}, {requestId}, {Guid.NewGuid()}, {2}, {2}, {Now.AddMinutes(2)});"""));
        foreignCancellation.SqlState.Should().Be("P0002");
    }

    [DockerFact]
    public async Task WriterRejectsARequestForAnotherPayeesWallet()
    {
        await using var container = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_payout_request_ownership");

        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.ConnectionString)
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(container.ConnectionString);
        await connection.OpenAsync();
        var ownerId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await SeedWalletAsync(connection, ownerId, walletId, tenantId);

        var foreignRequest = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsWriterAsync(
            connection,
            $"""
            SELECT economy_private.create_payout_request_v3(
                {Guid.NewGuid()}, {tenantId}, {"request-foreign"}, {new string('b', 64)}, {Guid.NewGuid()}, {walletId}, {250}, {1}, {1},
                {Now}, {Now});
            """));

        foreignRequest.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
        foreignRequest.MessageText.Should().Contain("does not belong");
    }

    [DockerFact]
    public async Task WriterScopesIdempotencyKeysToThePayee()
    {
        await using var container = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_payout_request_idempotency");

        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.ConnectionString)
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(container.ConnectionString);
        await connection.OpenAsync();
        var firstPayee = Guid.NewGuid();
        var firstWallet = Guid.NewGuid();
        var secondPayee = Guid.NewGuid();
        var secondWallet = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await SeedWalletAsync(connection, firstPayee, firstWallet, tenantId);
        await SeedWalletAsync(connection, secondPayee, secondWallet, tenantId);

        await ExecuteAsWriterAsync(connection, $"""
            SELECT economy_private.create_payout_request_v3(
                {Guid.NewGuid()}, {tenantId}, {"same-key"}, {new string('a', 64)}, {firstPayee}, {firstWallet}, {250}, {1}, {1},
                {Now}, {Now});
            SELECT economy_private.create_payout_request_v3(
                {Guid.NewGuid()}, {tenantId}, {"same-key"}, {new string('b', 64)}, {secondPayee}, {secondWallet}, {250}, {1}, {1},
                {Now}, {Now});
            """);

        (await ScalarAsync<long>(connection, $"""
            SELECT COUNT(*)
            FROM economy_private.read_payout_request_by_idempotency_v3({tenantId}, {firstPayee}, {"same-key"});
            """)).Should().Be(1);
        (await ScalarAsync<long>(connection, $"""
            SELECT COUNT(*)
            FROM economy_private.read_payout_request_by_idempotency_v3({tenantId}, {secondPayee}, {"same-key"});
            """)).Should().Be(1);
    }

    [DockerFact]
    public async Task ReviewIsTenantScopedAppendOnlyAndRequiresTwoDifferentAdministrators()
    {
        await using var container = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_payout_request_review");

        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.ConnectionString)
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(container.ConnectionString);
        await connection.OpenAsync();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var payeeId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var firstAdministratorId = Guid.NewGuid();
        var secondAdministratorId = Guid.NewGuid();
        await SeedWalletAsync(connection, payeeId, walletId, tenantId);

        await ExecuteAsWriterAsync(connection, $"""
            SELECT economy_private.create_payout_request_v3(
                {requestId}, {tenantId}, {"review-request"}, {new string('c', 64)}, {payeeId}, {walletId}, {250}, {1}, {1},
                {Now}, {Now});
            """);

        (await ScalarAsync<long>(connection, $"""
            SELECT COUNT(*)
            FROM economy_private.read_payout_request_for_review_v2({otherTenantId}, {requestId});
            """)).Should().Be(0);

        await ExecuteAsWriterAsync(connection, $"""
            SELECT economy_private.review_payout_request_v2(
                {tenantId}, {requestId}, {1}, {firstAdministratorId}, {3}, {"approved after review"}, {Now.AddMinutes(1)});
            """);

        (await ScalarAsync<int>(connection, $"""
            SELECT "State"
            FROM economy_private.read_payout_request_for_review_v2({tenantId}, {requestId});
            """)).Should().Be(5);

        var duplicateApprover = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsWriterAsync(connection, $"""
            SELECT economy_private.review_payout_request_v2(
                {tenantId}, {requestId}, {2}, {firstAdministratorId}, {3}, {"duplicate approval"}, {Now.AddMinutes(2)});
            """));
        duplicateApprover.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);

        await ExecuteAsWriterAsync(connection, $"""
            SELECT economy_private.review_payout_request_v2(
                {tenantId}, {requestId}, {2}, {secondAdministratorId}, {3}, {"second independent approval"}, {Now.AddMinutes(2)});
            """);

        (await ScalarAsync<int>(connection, $"""
            SELECT "State"
            FROM economy_private.read_payout_request_for_review_v2({tenantId}, {requestId});
            """)).Should().Be(3);
        (await ScalarAsync<long>(connection, $"""
            SELECT COUNT(*)
            FROM economy_private.read_payout_request_review_audit_v2({tenantId}, {requestId});
            """)).Should().Be(2);

        var directAuditUpdate = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsWriterAsync(
            connection,
            $"""UPDATE public.economy_payout_request_review_audit_events SET "Reason" = {"mutated"};"""));
        directAuditUpdate.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
    }

    private static Task SeedWalletAsync(NpgsqlConnection connection, Guid ownerId, Guid walletId, Guid? tenantId = null) => ExecuteAsync(connection, $"""
        INSERT INTO public.economy_wallets ("Id", "OwnerId", "TenantId", "State", "CreatedAt")
        VALUES ({walletId}, {ownerId}, {tenantId ?? Guid.NewGuid()}, {1}, {Now});
        """);

    private static async Task ExecuteAsWriterAsync(NpgsqlConnection connection, FormattableString sql)
    {
        await SetWriterRoleAsync(connection);
        try
        {
            await ExecuteAsync(connection, sql);
        }
        finally
        {
            await ResetRoleAsync(connection);
        }
    }

    private static async Task SetWriterRoleAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("SET ROLE gameguild_economy_writer;", connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ResetRoleAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("RESET ROLE;", connection);
        await command.ExecuteNonQueryAsync();
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
