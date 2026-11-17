using System.Security.Cryptography;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Identity.Authentication;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class StepUpBudgetMigrationPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task UpgradeAndGuardedRollbackPreserveExistingRowsAndSpentBudgetEvidence()
    {
        using var hostScope = fixture.Factory.Services.CreateScope();
        _ = hostScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Model;
        await using var isolated = await EconomyPostgreSqlTestDatabase.CreateEmptyAsync("stepup_attempt_migration");
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(isolated.ConnectionString).Options);
        Assert.False(context.Database.HasPendingModelChanges());
        var migrations = context.Database.GetMigrations().ToList();
        var migration = Assert.Single(migrations, value => value.EndsWith("_LimitStepUpVerificationAttempts", StringComparison.Ordinal));
        var predecessor = migrations[migrations.IndexOf(migration) - 1];
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(predecessor);

        var active = await InsertLegacyAsync(context, null);
        await InsertLegacyAsync(context, MfaMethod.Totp);
        await InsertLegacyAsync(context, MfaMethod.WebAuthn);
        var original = await ReadBaseRowsAsync(context);
        Assert.Equal(3, original.Length);
        await migrator.MigrateAsync(migration);
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Equal(original, await ReadBaseRowsAsync(context));
        Assert.All(await context.Set<StepUpChallenge>().AsNoTracking().ToListAsync(), row => Assert.Equal(0, row.VerificationAttempts));
        await AssertCounterAndIndexAsync(context);

        var store = new PostgreSqlStepUpChallengeStore(context);
        Assert.True(await store.TryReserveVerificationAttemptAsync(active.Id,
            new StepUpSubject(active.TenantId, active.ActorId, active.SessionId), DateTimeOffset.UtcNow, CancellationToken.None));
        Assert.Equal(1, (await context.Set<StepUpChallenge>().AsNoTracking().SingleAsync(row => row.Id == active.Id)).VerificationAttempts);
        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        var rejected = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync(predecessor));
        Assert.Equal(PostgresErrorCodes.CheckViolation, rejected.SqlState);
        Assert.Equal("ck_step_up_challenges_attempts_downgrade", rejected.ConstraintName);
        Assert.Equal(applied, (await context.Database.GetAppliedMigrationsAsync()).ToArray());
        Assert.Equal(original, await ReadBaseRowsAsync(context));
        Assert.Equal(1, (await context.Set<StepUpChallenge>().AsNoTracking().SingleAsync(row => row.Id == active.Id)).VerificationAttempts);
        await AssertCounterAndIndexAsync(context);

        // Only this test's spent row is removed from its own disposable database.
        // A safe downgrade never resets a retained challenge's attempt counter.
        Assert.Equal(1, await context.Set<StepUpChallenge>().Where(row => row.Id == active.Id).ExecuteDeleteAsync());
        var retained = await ReadBaseRowsAsync(context);
        Assert.Equal(2, retained.Length);
        await migrator.MigrateAsync(predecessor);
        Assert.DoesNotContain(migration, await context.Database.GetAppliedMigrationsAsync());
        Assert.Equal(retained, await ReadBaseRowsAsync(context));
        Assert.Equal(0, await context.Database.SqlQueryRaw<int>("""
            SELECT COUNT(*)::integer AS "Value" FROM information_schema.columns
            WHERE table_schema = 'gameguild.authentication' AND table_name = 'step_up_challenges'
                AND column_name = 'verification_attempts'
            """).SingleAsync());
        await migrator.MigrateAsync(migration);
        Assert.Equal(retained, await ReadBaseRowsAsync(context));
        Assert.All(await context.Set<StepUpChallenge>().AsNoTracking().ToListAsync(), row => Assert.Equal(0, row.VerificationAttempts));
        Assert.False(context.Database.HasPendingModelChanges());
        await AssertCounterAndIndexAsync(context);
    }

    private static async Task AssertCounterAndIndexAsync(ApplicationDbContext context)
    {
        Assert.Equal("NO:0", await context.Database.SqlQueryRaw<string>("""
            SELECT (is_nullable || ':' || column_default)::text AS "Value"
            FROM information_schema.columns
            WHERE table_schema = 'gameguild.authentication' AND table_name = 'step_up_challenges'
                AND column_name = 'verification_attempts' AND data_type = 'integer'
            """).SingleAsync());
        var index = await context.Database.SqlQueryRaw<string>("""
            SELECT indexdef::text AS "Value" FROM pg_indexes
            WHERE schemaname = 'gameguild.authentication' AND tablename = 'step_up_challenges'
                AND indexname = 'ix_step_up_challenges_expiry_cleanup'
            """).SingleAsync();
        Assert.Contains("expires_at", index);
    }

    private static async Task<StepUpChallenge> InsertLegacyAsync(ApplicationDbContext context, MfaMethod? method)
    {
        var row = new StepUpChallenge
        {
            Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), ActorId = Guid.NewGuid(), SessionId = Guid.NewGuid(),
            OperationType = "identity.operation.confirm", TargetReference = "resource/legacy", PayloadHash = Digest(),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            VerifiedAt = method.HasValue ? DateTimeOffset.UtcNow : null, VerificationMethod = method,
            ReceiptHash = method.HasValue ? Digest() : null,
            WebAuthnChallengeHash = method == MfaMethod.WebAuthn ? Digest() : null
        };
        // The predecessor already has nonce binding but deliberately has no counter.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "gameguild.authentication"."step_up_challenges"
                (id, tenant_id, actor_id, session_id, operation_type, target_reference,
                 payload_hash, created_at, expires_at, webauthn_challenge_hash,
                 verified_at, verification_method, receipt_hash)
            VALUES ({row.Id}, {row.TenantId}, {row.ActorId}, {row.SessionId}, {row.OperationType}, {row.TargetReference},
                    {row.PayloadHash}, {row.CreatedAt}, {row.ExpiresAt}, {row.WebAuthnChallengeHash},
                    {row.VerifiedAt}, {method?.ToString()}, {row.ReceiptHash})
            """);
        return row;
    }

    private static Task<string[]> ReadBaseRowsAsync(ApplicationDbContext context) => context.Database.SqlQueryRaw<string>("""
        SELECT row_to_json(original_row)::text AS "Value" FROM (
            SELECT id, tenant_id, actor_id, session_id, operation_type, target_reference,
                   payload_hash, created_at, expires_at, webauthn_challenge_hash,
                   verified_at, verification_method, receipt_hash, consumed_at
            FROM "gameguild.authentication"."step_up_challenges" ORDER BY id
        ) original_row
        """).ToArrayAsync();

    private static string Digest() => Convert.ToHexString(SHA256.HashData(RandomNumberGenerator.GetBytes(32))).ToLowerInvariant();
}
