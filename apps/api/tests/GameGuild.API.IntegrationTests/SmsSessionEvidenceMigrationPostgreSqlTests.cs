using System.Security.Cryptography;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class SmsSessionEvidenceMigrationPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task UpgradeAndGuardedRollbackPreserveVerifiedSessionEvidence()
    {
        using var hostScope = fixture.Factory.Services.CreateScope();
        _ = hostScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Model;
        await using var database = await EconomyPostgreSqlTestDatabase.CreateEmptyAsync("sms_evidence_migration");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(database.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        Assert.False(db.Database.HasPendingModelChanges());
        var migrations = db.Database.GetMigrations().ToList();
        var migration = Assert.Single(migrations, value => value.EndsWith("_EnableSmsSessionMfaEvidence", StringComparison.Ordinal));
        var predecessor = migrations[migrations.IndexOf(migration) - 1];
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(predecessor);

        foreach (var method in new[] { MfaMethod.Totp, MfaMethod.BackupCode, MfaMethod.WebAuthn })
        {
            await InsertEvidenceAsync(db, method);
        }
        var original = await ReadEvidenceAsync(db);
        Assert.Equal(new[] { MfaMethod.Totp, MfaMethod.BackupCode, MfaMethod.WebAuthn }.Order(),
            original.Select(row => row.Method).Order());
        await AssertMethodRejectedAsync(db, MfaMethod.Sms);
        Assert.Equal(original, await ReadEvidenceAsync(db));

        await migrator.MigrateAsync(migration);
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(original, await ReadEvidenceAsync(db));
        var smsSession = await InsertEvidenceAsync(db, MfaMethod.Sms);
        var upgraded = await ReadEvidenceAsync(db);
        Assert.Equal(new[] { MfaMethod.Totp, MfaMethod.BackupCode, MfaMethod.WebAuthn, MfaMethod.Sms }.Order(),
            upgraded.Select(row => row.Method).Order());
        Assert.Contains(upgraded, row => row.SessionId == smsSession && row.Method == MfaMethod.Sms);

        foreach (var method in new[] { MfaMethod.Email, MfaMethod.Push, (MfaMethod)int.MaxValue })
        {
            await AssertMethodRejectedAsync(db, method);
            Assert.Equal(upgraded, await ReadEvidenceAsync(db));
        }

        // PostgreSQL validates the restored older check inside the migration transaction.
        // A downgrade must refuse existing SMS evidence rather than deleting or relabeling it.
        var rejectedRollback = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync(predecessor));
        Assert.Equal(PostgresErrorCodes.CheckViolation, rejectedRollback.SqlState);
        Assert.Equal("ck_session_mfa_method", rejectedRollback.ConstraintName);
        Assert.Contains(migration, await db.Database.GetAppliedMigrationsAsync());
        Assert.Equal(upgraded, await ReadEvidenceAsync(db));
        Assert.Contains("'Sms'", await ReadConstraintAsync(db), StringComparison.Ordinal);
        Assert.Equal(4, await db.Set<UserSession>().CountAsync());

        // Remove only this test's SMS rows in its isolated database, then exercise a safe downgrade.
        Assert.Equal(1, await db.Set<SessionMfaEvidence>().Where(row => row.SessionId == smsSession).ExecuteDeleteAsync());
        Assert.Equal(1, await db.Set<UserSession>().Where(row => row.Id == smsSession).ExecuteDeleteAsync());
        await migrator.MigrateAsync(predecessor);
        Assert.DoesNotContain(migration, await db.Database.GetAppliedMigrationsAsync());
        Assert.DoesNotContain("'Sms'", await ReadConstraintAsync(db), StringComparison.Ordinal);
        Assert.Equal(original, await ReadEvidenceAsync(db));
        await AssertMethodRejectedAsync(db, MfaMethod.Sms);
        Assert.Equal(original, await ReadEvidenceAsync(db));

        await migrator.MigrateAsync(migration);
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(original, await ReadEvidenceAsync(db));
        await InsertEvidenceAsync(db, MfaMethod.Sms);
        Assert.Equal(4, await db.Set<SessionMfaEvidence>().CountAsync());
        Assert.Equal(4, await db.Set<UserSession>().CountAsync());
    }

    private static async Task<Guid> InsertEvidenceAsync(ApplicationDbContext db, MfaMethod method)
    {
        var sessionId = Guid.NewGuid();
        var marker = Guid.NewGuid().ToString("N");
        var subject = User.Create($"mfa-evidence-{marker}@example.test", $"MFA evidence {marker}");
        var subjectId = subject.Id;
        // Evidence metadata is synthetic; its session must still satisfy the current principal FK.
        db.Set<User>().Add(subject);
        var now = DateTimeOffset.UtcNow;
        db.Set<UserSession>().Add(new UserSession
        {
            Id = sessionId,
            UserId = subjectId,
            RefreshToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
            IpAddress = "127.0.0.1",
            UserAgent = "Native migration validation",
            ExpiresAt = now.UtcDateTime.AddHours(1),
            LastUsedAt = now.UtcDateTime,
            CreatedAt = now.UtcDateTime,
            UpdatedAt = now.UtcDateTime,
            IsActive = true
        });
        db.Set<SessionMfaEvidence>().Add(new SessionMfaEvidence
        {
            SessionId = sessionId,
            ChallengeId = Guid.NewGuid(),
            SubjectId = subjectId,
            TenantId = Guid.NewGuid(),
            TokenVersion = 1,
            PolicyFingerprint = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
            FirstFactor = SignInFirstFactor.Password,
            FirstFactorVerifiedAt = now.AddMinutes(-1),
            VerifiedAt = now,
            Method = method
        });
        await db.SaveChangesAsync();
        return sessionId;
    }

    private static async Task AssertMethodRejectedAsync(ApplicationDbContext db, MfaMethod method)
    {
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => InsertEvidenceAsync(db, method));
        var postgres = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.CheckViolation, postgres.SqlState);
        Assert.Equal("ck_session_mfa_method", postgres.ConstraintName);
        db.ChangeTracker.Clear();
    }

    private static Task<string> ReadConstraintAsync(ApplicationDbContext db) => db.Database.SqlQueryRaw<string>("""
        SELECT pg_get_constraintdef(oid) AS "Value" FROM pg_constraint
        WHERE conname = 'ck_session_mfa_method'
          AND conrelid = '"gameguild.authentication".session_mfa_evidence'::regclass
        """).SingleAsync();

    private static Task<List<EvidenceRow>> ReadEvidenceAsync(ApplicationDbContext db) => db.Set<SessionMfaEvidence>()
        .AsNoTracking().OrderBy(row => row.SessionId)
        .Select(row => new EvidenceRow(row.SessionId, row.ChallengeId, row.SubjectId, row.TenantId,
            row.TokenVersion, row.PolicyFingerprint, row.FirstFactor, row.FirstFactorVerifiedAt, row.VerifiedAt, row.Method))
        .ToListAsync();

    private sealed record EvidenceRow(Guid SessionId, Guid ChallengeId, Guid SubjectId, Guid TenantId,
        int TokenVersion, string PolicyFingerprint, SignInFirstFactor FirstFactor,
        DateTimeOffset FirstFactorVerifiedAt, DateTimeOffset VerifiedAt, MfaMethod Method);
}
