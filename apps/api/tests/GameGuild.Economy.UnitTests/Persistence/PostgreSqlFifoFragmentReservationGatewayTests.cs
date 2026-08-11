using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.Database.Migrations;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace GameGuild.Economy.UnitTests.Persistence;

public sealed class PostgreSqlFifoFragmentReservationGatewayTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 9, 22, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MigrationReplacesGlobalLineageOverlapWithPerLotFifoReservations()
    {
        var up = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var down = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var migration = new ExposedMigration();

        migration.BuildUp(up);
        migration.BuildDown(down);

        up.Operations.OfType<DropIndexOperation>()
            .Should().Contain(operation => operation.Name == "ux_economy_credit_lots_root_source");
        up.Operations.OfType<CreateIndexOperation>()
            .Should().Contain(operation => operation.Name == "ix_economy_credit_lots_root_source" && !operation.IsUnique);

        var sql = string.Join('\n', up.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
        sql.Should().Contain("DROP CONSTRAINT IF EXISTS ex_economy_fragment_root_ranges_no_overlap");
        sql.Should().Contain("economy_fragment_reservations");
        sql.Should().Contain("reserve_fifo_fragments_v1");
        sql.Should().Contain("transition_fifo_fragment_reservations_v1");
        sql.Should().Contain("SECURITY DEFINER");
        sql.Should().Contain("FOR UPDATE");
        sql.Should().Contain("range_agg");
        sql.Should().Contain("gameguild_economy_writer");

        var downSql = string.Join('\n', down.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
        downSql.Should().Contain("DROP FUNCTION IF EXISTS economy_private.reserve_fifo_fragments_v1");
        downSql.Should().Contain("ex_economy_fragment_root_ranges_no_overlap");
    }

    [DockerFact]
    public async Task WriterReservesConfirmedLotsInFifoOrderAndPreventsASecondReservation()
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("economy_fifo_reservations")
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
        var walletId = Guid.NewGuid();
        var firstRoot = Guid.NewGuid();
        var secondRoot = Guid.NewGuid();
        var firstLot = Guid.NewGuid();
        var secondLot = Guid.NewGuid();
        await SeedLotAsync(connection, walletId, firstRoot, firstLot, 10, Now.AddMinutes(-2), 1);
        await SeedLotAsync(connection, walletId, secondRoot, secondLot, 10, Now.AddMinutes(-1), 2);

        var operationId = Guid.NewGuid();
        var gateway = new PostgreSqlFifoFragmentReservationGateway(context);
        var request = new FifoFragmentReservationRequest(
            operationId,
            new WalletId(walletId),
            CurrencyCode.HardCoin,
            ProvenanceKind.PurchasedHard,
            new CoinAmount(CurrencyCode.HardCoin, 15),
            PersistedFragmentReservationPurpose.HardToSoftConversion,
            Now);
        var rows = gateway.Reserve(request);
        rows.Should().HaveCount(2);
        rows[0].ParentLotId.Value.Should().Be(firstLot);
        rows[0].Amount.Units.Should().Be(10);
        rows[1].ParentLotId.Value.Should().Be(secondLot);
        rows[1].Amount.Units.Should().Be(5);

        var duplicate = gateway.Reserve(request);
        duplicate.Select(row => row.Id).Should().BeEquivalentTo(rows.Select(row => row.Id));

        var competing = () => gateway.Reserve(request with
        {
            OperationId = Guid.NewGuid(),
            Amount = new CoinAmount(CurrencyCode.HardCoin, 6)
        });
        competing.Should().Throw<RegisteredPostingRejectedException>();

        var released = gateway.Transition(
            operationId,
            PersistedFragmentReservationStatus.Reserved,
            PersistedFragmentReservationStatus.Released,
            Now);
        released.Should().Be(2);

        var next = gateway.Reserve(request with
        {
            OperationId = Guid.NewGuid(),
            Amount = new CoinAmount(CurrencyCode.HardCoin, 20)
        });
        next.Sum(row => row.Amount.Units).Should().Be(20);
    }

    [DockerFact]
    public async Task WriterRejectsPayoutReservationsUnlessEarnedHardIsMatureAndTheWalletIsClear()
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("economy_payout_fifo_eligibility")
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
        var gateway = new PostgreSqlFifoFragmentReservationGateway(context);

        var immatureWallet = Guid.NewGuid();
        await SeedLotAsync(
            connection,
            immatureWallet,
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            Now.AddDays(-119),
            1,
            provenance: 2,
            cashOutEligible: true,
            maturesAt: Now.AddDays(1));
        Action reserveImmature = () => gateway.Reserve(CreatePayoutRequest(immatureWallet, 10));
        reserveImmature.Should().Throw<RegisteredPostingRejectedException>();

        var heldWallet = Guid.NewGuid();
        await SeedLotAsync(
            connection,
            heldWallet,
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            Now.AddDays(-121),
            1,
            provenance: 2,
            cashOutEligible: true,
            maturesAt: Now.AddDays(-1));
        await ExecuteAsync(connection, $"""
            INSERT INTO public.economy_holds (
                "Id", "WalletId", "Currency", "AmountUnits", "Reason", "Status", "EffectiveAt", "ReleasedAt")
            VALUES ('{Guid.NewGuid()}', '{heldWallet}', 1, 1, 1, 1, '{Now.AddMinutes(-1):O}', NULL);
            """);
        Action reserveHeld = () => gateway.Reserve(CreatePayoutRequest(heldWallet, 10));
        reserveHeld.Should().Throw<RegisteredPostingRejectedException>();

        var indebtedWallet = Guid.NewGuid();
        await SeedLotAsync(
            connection,
            indebtedWallet,
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            Now.AddDays(-121),
            1,
            provenance: 2,
            cashOutEligible: true,
            maturesAt: Now.AddDays(-1));
        await ExecuteAsync(connection, $"""
            INSERT INTO public.economy_wallet_debts (
                "WalletId", "OutstandingHardUnits", "UpdatedAt", "Version")
            VALUES ('{indebtedWallet}', 1, '{Now:O}', 1);
            """);
        Action reserveIndebted = () => gateway.Reserve(CreatePayoutRequest(indebtedWallet, 10));
        reserveIndebted.Should().Throw<RegisteredPostingRejectedException>();

        var eligibleWallet = Guid.NewGuid();
        var eligibleLot = Guid.NewGuid();
        await SeedLotAsync(
            connection,
            eligibleWallet,
            Guid.NewGuid(),
            eligibleLot,
            10,
            Now.AddDays(-121),
            1,
            provenance: 2,
            cashOutEligible: true,
            maturesAt: Now.AddDays(-1));
        var reservations = gateway.Reserve(CreatePayoutRequest(eligibleWallet, 10));
        reservations.Should().ContainSingle();
        reservations.Single().ParentLotId.Value.Should().Be(eligibleLot);
    }

    private static FifoFragmentReservationRequest CreatePayoutRequest(Guid walletId, long amountUnits)
        => new(
            Guid.NewGuid(),
            new WalletId(walletId),
            CurrencyCode.HardCoin,
            ProvenanceKind.EarnedHard,
            new CoinAmount(CurrencyCode.HardCoin, amountUnits),
            PersistedFragmentReservationPurpose.Payout,
            Now);
    private static async Task SeedLotAsync(
        NpgsqlConnection connection,
        Guid walletId,
        Guid rootId,
        Guid lotId,
        long amountUnits,
        DateTimeOffset confirmedAt,
        long sequence,
        int provenance = 1,
        bool cashOutEligible = false,
        DateTimeOffset? maturesAt = null)
    {
        if (sequence == 1)
        {
            await ExecuteAsync(connection, $"""
                INSERT INTO public.economy_wallets ("Id", "OwnerId", "TenantId", "State", "CreatedAt")
                VALUES ('{walletId}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 1, '{confirmedAt:O}');
                """);
        }

        await ExecuteAsync(connection, $"""
            INSERT INTO public.economy_source_stamps (
                "Id", "SourceKind", "InternalSourceId", "SourceLegId", "Provider", "ProviderReference",
                "EvidenceHash", "Provenance", "State", "ActorId", "TenantId", "PostingReferenceId",
                "PolicyVersion", "AuthoritativeUnits", "ObservedAt", "ConfirmedAt")
            VALUES (
                '{rootId}', 'test', '{rootId:N}', 'leg', NULL, NULL, 'evidence-{rootId:N}', {provenance}, 2,
                '{Guid.NewGuid()}', '{Guid.NewGuid()}', NULL, 1, {amountUnits}, '{confirmedAt:O}', '{confirmedAt:O}');

            INSERT INTO public.economy_credit_lots (
                "Id", "WalletId", "RootSourceStampId", "Currency", "AmountUnits", "Provenance", "CreditedAt",
                "ConfirmedAt", "OriginalMaturesAt", "CashOutEligible", "JournalSequence", "State", "ReversalEpoch")
            VALUES (
                '{lotId}', '{walletId}', '{rootId}', 1, {amountUnits}, {provenance}, '{confirmedAt:O}', '{confirmedAt:O}',
                '{(maturesAt ?? confirmedAt):O}', {cashOutEligible.ToString().ToLowerInvariant()}, {sequence}, 1, 0);

            INSERT INTO public.economy_root_reversal_states (
                "RootSourceStampId", "Epoch", "CumulativeProviderUnits", "ReversedUnits", "State", "TargetedRanges", "UpdatedAt")
            VALUES ('{rootId}', 0, 0, 0, 'active', '[]'::jsonb, '{confirmedAt:O}');

            INSERT INTO public.economy_fragment_root_ranges (
                "Id", "RootSourceStampId", "CreditLotId", "EntryAllocationId", "StartInclusive", "EndExclusive", "ReversalEpoch")
            VALUES ('{Guid.NewGuid()}', '{rootId}', '{lotId}', NULL, 0, {amountUnits * 1000}, 0);
            """);
    }

    private static async Task<IReadOnlyList<ReservationRow>> ReserveAsync(
        NpgsqlConnection connection,
        Guid operationId,
        Guid walletId,
        long amount)
    {
        await using var command = new NpgsqlCommand($"""
            SELECT *
            FROM economy_private.reserve_fifo_fragments_v1(
                '{operationId}', '{walletId}', 1, 1, {amount}, 3, '{Now:O}');
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<ReservationRow>();
        while (await reader.ReadAsync())
        {
            rows.Add(new ReservationRow(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetInt64(6)));
        }
        return rows;
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

    private sealed record ReservationRow(Guid ReservationId, Guid ParentLotId, long AmountUnits);

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS"), "1", StringComparison.Ordinal))
                Skip = "Docker tests disabled by SKIP_DOCKER_TESTS=1.";
        }
    }

    private sealed class ExposedMigration : AddEconomyFifoReservationWriter
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);

        public void BuildDown(MigrationBuilder builder) => Down(builder);
    }
}
