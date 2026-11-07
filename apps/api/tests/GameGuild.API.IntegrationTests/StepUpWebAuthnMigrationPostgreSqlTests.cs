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
public sealed class StepUpWebAuthnMigrationPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task UpgradeAndGuardedRollbackPreserveChallengeAndReceiptEvidence()
    {
        // Initialize the same real host model used by the collection, but exercise
        // upgrade/downgrade only in this test's independent disposable database.
        using var hostScope = fixture.Factory.Services.CreateScope();
        _ = hostScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Model;
        await using var database = await EconomyPostgreSqlTestDatabase.CreateEmptyAsync("stepup_nonce_migration");
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(database.ConnectionString).Options);
        Assert.False(context.Database.HasPendingModelChanges());
        var migrations = context.Database.GetMigrations().ToList();
        var migration = Assert.Single(migrations, value => value.EndsWith("_BindStepUpWebAuthnChallenge", StringComparison.Ordinal));
        var predecessor = migrations[migrations.IndexOf(migration) - 1];
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(predecessor);

        var web = await InsertLegacyAsync(context, MfaMethod.WebAuthn);
        await InsertLegacyAsync(context, MfaMethod.Totp);
        await InsertLegacyAsync(context, MfaMethod.BackupCode);
        var original = await ReadBaseRowsAsync(context);
        Assert.Equal(3, original.Length);

        await migrator.MigrateAsync(migration);
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Equal(original, await ReadBaseRowsAsync(context));
        Assert.All(await context.Set<StepUpChallenge>().AsNoTracking().ToListAsync(), row => Assert.Null(row.WebAuthnChallengeHash));
        var store = new PostgreSqlStepUpChallengeStore(context);
        // The upgrade preserves the historical row, but an old unbound receipt
        // cannot become an authorized WebAuthn operation after the security fix.
        Assert.False(await store.ConsumeAsync(
            new StepUpSubject(web.TenantId, web.ActorId, web.SessionId),
            new StepUpOperationBinding(web.OperationType, web.TargetReference, web.PayloadHash),
            web.ReceiptHash!, DateTimeOffset.UtcNow, CancellationToken.None));
        Assert.Equal(original, await ReadBaseRowsAsync(context));

        var bound = new StepUpChallenge
        {
            Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), ActorId = Guid.NewGuid(), SessionId = Guid.NewGuid(),
            OperationType = "identity.credentials.rotate", TargetReference = "credential/current", PayloadHash = Digest(),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
        };
        await store.AddAsync(bound, CancellationToken.None);
        var subject = new StepUpSubject(bound.TenantId, bound.ActorId, bound.SessionId);
        var nonce = Digest();
        Assert.True(await store.BindWebAuthnChallengeAsync(bound.Id, subject, nonce, DateTimeOffset.UtcNow, CancellationToken.None));
        Assert.True(await store.MarkWebAuthnVerifiedAsync(bound.Id, subject, nonce, Digest(), DateTimeOffset.UtcNow, CancellationToken.None));
        var upgraded = await ReadBaseRowsAsync(context);
        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();

        var rejected = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync(predecessor));
        Assert.Equal(PostgresErrorCodes.CheckViolation, rejected.SqlState);
        Assert.Equal("ck_step_up_challenges_webauthn_downgrade", rejected.ConstraintName);
        Assert.Equal(applied, (await context.Database.GetAppliedMigrationsAsync()).ToArray());
        Assert.Equal(upgraded, await ReadBaseRowsAsync(context));
        var retained = await context.Set<StepUpChallenge>().AsNoTracking().SingleAsync(row => row.Id == bound.Id);
        Assert.Equal(nonce, retained.WebAuthnChallengeHash);
        Assert.NotNull(retained.ReceiptHash);
        Assert.Equal(MfaMethod.WebAuthn, retained.VerificationMethod);

        // Remove only this test's new bound row from its own disposable database.
        // The three pre-upgrade rows remain untouched through safe downgrade/re-upgrade.
        Assert.Equal(1, await context.Set<StepUpChallenge>().Where(row => row.Id == bound.Id).ExecuteDeleteAsync());
        await migrator.MigrateAsync(predecessor);
        Assert.DoesNotContain(migration, await context.Database.GetAppliedMigrationsAsync());
        Assert.Equal(original, await ReadBaseRowsAsync(context));
        await migrator.MigrateAsync(migration);
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Equal(original, await ReadBaseRowsAsync(context));
        Assert.All(await context.Set<StepUpChallenge>().AsNoTracking().ToListAsync(), row => Assert.Null(row.WebAuthnChallengeHash));
    }

    private static async Task<StepUpChallenge> InsertLegacyAsync(ApplicationDbContext context, MfaMethod method)
    {
        var row = new StepUpChallenge
        {
            Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), ActorId = Guid.NewGuid(), SessionId = Guid.NewGuid(),
            OperationType = "identity.credentials.rotate", TargetReference = "credential/legacy", PayloadHash = Digest(),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            VerifiedAt = DateTimeOffset.UtcNow, VerificationMethod = method, ReceiptHash = Digest()
        };
        // The predecessor schema deliberately has no nonce column yet.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "gameguild.authentication"."step_up_challenges"
                (id, tenant_id, actor_id, session_id, operation_type, target_reference,
                 payload_hash, created_at, expires_at, verified_at, verification_method, receipt_hash)
            VALUES ({row.Id}, {row.TenantId}, {row.ActorId}, {row.SessionId}, {row.OperationType}, {row.TargetReference},
                    {row.PayloadHash}, {row.CreatedAt}, {row.ExpiresAt}, {row.VerifiedAt}, {method.ToString()}, {row.ReceiptHash})
            """);
        return row;
    }

    private static Task<string[]> ReadBaseRowsAsync(ApplicationDbContext context) => context.Database.SqlQueryRaw<string>("""
        SELECT row_to_json(original_row)::text AS "Value" FROM (
            SELECT id, tenant_id, actor_id, session_id, operation_type, target_reference,
                   payload_hash, created_at, expires_at, verified_at, verification_method, receipt_hash, consumed_at
            FROM "gameguild.authentication"."step_up_challenges" ORDER BY id
        ) original_row
        """).ToArrayAsync();

    private static string Digest() => Convert.ToHexString(SHA256.HashData(RandomNumberGenerator.GetBytes(32))).ToLowerInvariant();
}
