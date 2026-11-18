using System.Security.Cryptography;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Identity.Authentication;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class StepUpVerificationBudgetPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task TwentyIndependentConnectionsCanReserveExactlyFiveSlots()
    {
        var row = await SeedAsync();
        var subject = Subject(row);
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var database = CreateContext();
            return await new PostgreSqlStepUpChallengeStore(database)
                .TryReserveVerificationAttemptAsync(row.Id, subject, DateTimeOffset.UtcNow, CancellationToken.None);
        }));

        Assert.Equal(5, results.Count(success => success));
        Assert.Equal(5, (await ReadAsync(row.Id)).VerificationAttempts);
        await using var read = CreateContext();
        var store = new PostgreSqlStepUpChallengeStore(read);
        Assert.Null(await store.FindActiveAsync(row.Id, subject, DateTimeOffset.UtcNow, CancellationToken.None));
        Assert.False(await store.BindWebAuthnChallengeAsync(row.Id, subject, Digest(), DateTimeOffset.UtcNow, CancellationToken.None));
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("actor")]
    [InlineData("session")]
    public async Task ReservationCannotSpendAnotherSubjectsBudget(string dimension)
    {
        var row = await SeedAsync();
        var subject = Subject(row);
        var foreign = dimension switch
        {
            "tenant" => subject with { TenantId = Guid.NewGuid() },
            "actor" => subject with { ActorId = Guid.NewGuid() },
            _ => subject with { SessionId = Guid.NewGuid() }
        };
        await using var database = CreateContext();
        Assert.False(await new PostgreSqlStepUpChallengeStore(database)
            .TryReserveVerificationAttemptAsync(row.Id, foreign, DateTimeOffset.UtcNow, CancellationToken.None));
        Assert.Equal(0, (await ReadAsync(row.Id)).VerificationAttempts);
    }

    [Fact]
    public async Task ReissuedNonceKeepsSpentSlotsAndTheFifthProofCanVerifyAndConsumeOnce()
    {
        var row = await SeedAsync();
        var subject = Subject(row);
        await using var database = CreateContext();
        var store = new PostgreSqlStepUpChallengeStore(database);
        for (var attempt = 0; attempt < 4; attempt++)
        {
            Assert.True(await store.TryReserveVerificationAttemptAsync(row.Id, subject, DateTimeOffset.UtcNow, CancellationToken.None));
        }

        var nonce = Digest();
        Assert.True(await store.BindWebAuthnChallengeAsync(row.Id, subject, nonce, DateTimeOffset.UtcNow, CancellationToken.None));
        Assert.Equal(4, (await ReadAsync(row.Id)).VerificationAttempts);
        Assert.True(await store.TryReserveVerificationAttemptAsync(row.Id, subject, DateTimeOffset.UtcNow, CancellationToken.None));
        var receipt = Digest();
        Assert.True(await store.MarkWebAuthnVerifiedAsync(row.Id, subject, nonce, receipt, DateTimeOffset.UtcNow, CancellationToken.None));
        var binding = new StepUpOperationBinding(row.OperationType, row.TargetReference, row.PayloadHash);
        Assert.True(await store.ConsumeAsync(subject, binding, receipt, DateTimeOffset.UtcNow, CancellationToken.None));
        Assert.False(await store.ConsumeAsync(subject, binding, receipt, DateTimeOffset.UtcNow, CancellationToken.None));
        Assert.Equal(5, (await ReadAsync(row.Id)).VerificationAttempts);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("verified")]
    [InlineData("consumed")]
    public async Task InactiveChallengesCannotReserveSlots(string boundary)
    {
        var row = Challenge();
        var now = DateTimeOffset.UtcNow;
        if (boundary == "expired")
        {
            row.CreatedAt = now.AddMinutes(-10);
            row.ExpiresAt = now.AddMinutes(-1);
        }
        else
        {
            row.VerifiedAt = now;
            row.VerificationMethod = MfaMethod.Totp;
            row.ReceiptHash = Digest();
            if (boundary == "consumed")
            {
                row.ConsumedAt = now;
            }
        }

        await using var database = CreateContext();
        var store = new PostgreSqlStepUpChallengeStore(database);
        await store.AddAsync(row, CancellationToken.None);
        Assert.False(await store.TryReserveVerificationAttemptAsync(row.Id, Subject(row), now, CancellationToken.None));
        Assert.Equal(0, (await ReadAsync(row.Id)).VerificationAttempts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CounterParticipatesInTheOwningSqlTransaction(bool commit)
    {
        var row = await SeedAsync();
        await using (var database = CreateContext())
        {
            await using var transaction = await database.BeginTransactionAsync();
            Assert.True(await new PostgreSqlStepUpChallengeStore(database)
                .TryReserveVerificationAttemptAsync(row.Id, Subject(row), DateTimeOffset.UtcNow, CancellationToken.None));
            if (commit)
            {
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }
        }

        Assert.Equal(commit ? 1 : 0, (await ReadAsync(row.Id)).VerificationAttempts);
    }

    [Fact]
    public async Task CancelledReservationLeavesNoDurableIncrement()
    {
        var row = await SeedAsync();
        await using var database = CreateContext();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new PostgreSqlStepUpChallengeStore(database)
            .TryReserveVerificationAttemptAsync(row.Id, Subject(row), DateTimeOffset.UtcNow, cancellation.Token));
        Assert.Equal(0, (await ReadAsync(row.Id)).VerificationAttempts);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public async Task DatabaseRejectsAnOutOfRangeCounter(int counter)
    {
        var row = Challenge();
        row.VerificationAttempts = counter;
        await using var database = CreateContext();
        database.Set<StepUpChallenge>().Add(row);
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
        var postgres = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.CheckViolation, postgres.SqlState);
        Assert.Equal("ck_step_up_challenges_verification_attempts", postgres.ConstraintName);
    }

    [Fact]
    public async Task PruningIsOldestFirstBoundedAndKeepsRecentExpiredEvidence()
    {
        // The sweep is deliberately global, so it owns a migrated disposable database.
        await using var isolated = await EconomyPostgreSqlTestDatabase.CreateEmptyAsync("stepup_budget_cleanup");
        await using var database = CreateContext(isolated.ConnectionString);
        Assert.False(database.Database.HasPendingModelChanges());
        await database.Database.MigrateAsync();
        var now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var old = new List<StepUpChallenge>();
        for (var index = 0; index < 5; index++)
        {
            var row = Challenge();
            row.CreatedAt = now.AddDays(-4);
            row.ExpiresAt = now.AddDays(-3).AddMinutes(index);
            if (index >= 3)
            {
                row.VerifiedAt = row.CreatedAt.AddMinutes(1);
                row.VerificationMethod = MfaMethod.Totp;
                row.ReceiptHash = Digest();
            }

            if (index == 4)
            {
                row.ConsumedAt = row.CreatedAt.AddMinutes(2);
            }

            old.Add(row);
        }

        var recent = Challenge();
        recent.CreatedAt = now.AddDays(-1);
        recent.ExpiresAt = now.AddHours(-12);
        var live = Challenge();
        var exactCutoff = Challenge();
        exactCutoff.CreatedAt = now.AddDays(-2);
        exactCutoff.ExpiresAt = now.AddDays(-1);
        var justAfterCutoff = Challenge();
        justAfterCutoff.CreatedAt = now.AddDays(-2);
        justAfterCutoff.ExpiresAt = now.AddDays(-1).AddTicks(10);
        database.AddRange(old);
        database.AddRange(recent, live, exactCutoff, justAfterCutoff);
        await database.SaveChangesAsync();
        var store = new PostgreSqlStepUpChallengeStore(database);
        Assert.Equal(2, await store.PruneExpiredAsync(now, 2, CancellationToken.None));
        Assert.False(await database.Set<StepUpChallenge>().AsNoTracking().AnyAsync(row => row.Id == old[0].Id || row.Id == old[1].Id));
        Assert.True(await database.Set<StepUpChallenge>().AsNoTracking().AnyAsync(row => row.Id == old[2].Id));
        Assert.Equal(4, await store.PruneExpiredAsync(now, 1000, CancellationToken.None));
        Assert.Equal(0, await store.PruneExpiredAsync(now, 1000, CancellationToken.None));
        Assert.True(await database.Set<StepUpChallenge>().AsNoTracking().AnyAsync(row => row.Id == recent.Id));
        Assert.True(await database.Set<StepUpChallenge>().AsNoTracking().AnyAsync(row => row.Id == live.Id));
        Assert.False(await database.Set<StepUpChallenge>().AsNoTracking().AnyAsync(row => row.Id == exactCutoff.Id));
        Assert.True(await database.Set<StepUpChallenge>().AsNoTracking().AnyAsync(row => row.Id == justAfterCutoff.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public async Task InvalidPruneBatchIsRejected(int size)
    {
        await using var database = CreateContext();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new PostgreSqlStepUpChallengeStore(database)
            .PruneExpiredAsync(DateTimeOffset.UtcNow, size, CancellationToken.None));
    }

    private ApplicationDbContext CreateContext(string? connectionString = null) => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(connectionString ?? fixture.ConnectionString).Options);

    private async Task<StepUpChallenge> SeedAsync()
    {
        var row = Challenge();
        await using var database = CreateContext();
        await new PostgreSqlStepUpChallengeStore(database).AddAsync(row, CancellationToken.None);
        return row;
    }

    private async Task<StepUpChallenge> ReadAsync(Guid id)
    {
        await using var database = CreateContext();
        return await database.Set<StepUpChallenge>().AsNoTracking().SingleAsync(row => row.Id == id);
    }

    private static StepUpSubject Subject(StepUpChallenge row) => new(row.TenantId, row.ActorId, row.SessionId);
    private static string Digest() => Convert.ToHexString(SHA256.HashData(RandomNumberGenerator.GetBytes(32))).ToLowerInvariant();
    private static StepUpChallenge Challenge() => new()
    {
        Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), ActorId = Guid.NewGuid(), SessionId = Guid.NewGuid(),
        OperationType = "identity.operation.confirm", TargetReference = "resource/current", PayloadHash = Digest(),
        CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
    };
}
