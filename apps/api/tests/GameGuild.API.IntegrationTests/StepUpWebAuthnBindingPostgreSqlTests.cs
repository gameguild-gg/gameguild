using System.Security.Cryptography;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Identity.Authentication;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace GameGuild.API.IntegrationTests;

// These cases execute the actual store against migrated PostgreSQL. Cryptographic
// assertion verification is covered separately by the receipt-service boundary tests.
[Collection(ApiPostgreSqlCollection.Name)]
public sealed class StepUpWebAuthnBindingPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task IssuedNonceDigestIsStoredWithoutChangingOperationScope()
    {
        await using var database = CreateContext();
        var subject = Subject();
        var row = Challenge(subject);
        var store = new PostgreSqlStepUpChallengeStore(database);
        await store.AddAsync(row, CancellationToken.None);
        var digest = Digest();

        Assert.True(await store.BindWebAuthnChallengeAsync(row.Id, subject, digest, DateTimeOffset.UtcNow, CancellationToken.None));

        var stored = await database.Set<StepUpChallenge>().AsNoTracking().SingleAsync(value => value.Id == row.Id);
        Assert.Equal(digest, stored.WebAuthnChallengeHash);
        Assert.Equal(row.PayloadHash, stored.PayloadHash);
        Assert.Equal(row.OperationType, stored.OperationType);
        Assert.Equal(row.TargetReference, stored.TargetReference);
        Assert.Equal(subject.TenantId, stored.TenantId);
        Assert.Equal(subject.ActorId, stored.ActorId);
        Assert.Equal(subject.SessionId, stored.SessionId);
        Assert.Null(stored.ReceiptHash);
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("actor")]
    [InlineData("session")]
    public async Task BindingVerificationAndConsumptionRequireTheSameSubject(string changed)
    {
        await using var database = CreateContext();
        var subject = Subject();
        var foreign = changed switch
        {
            "tenant" => subject with { TenantId = Guid.NewGuid() },
            "actor" => subject with { ActorId = Guid.NewGuid() },
            _ => subject with { SessionId = Guid.NewGuid() }
        };
        var row = Challenge(subject);
        var store = new PostgreSqlStepUpChallengeStore(database);
        await store.AddAsync(row, CancellationToken.None);
        var nonce = Digest();
        var receipt = Digest();
        var now = DateTimeOffset.UtcNow;

        Assert.False(await store.BindWebAuthnChallengeAsync(row.Id, foreign, nonce, now, CancellationToken.None));
        Assert.True(await store.BindWebAuthnChallengeAsync(row.Id, subject, nonce, now, CancellationToken.None));
        Assert.False(await store.MarkWebAuthnVerifiedAsync(row.Id, foreign, nonce, receipt, now, CancellationToken.None));
        Assert.True(await store.MarkWebAuthnVerifiedAsync(row.Id, subject, nonce, receipt, now, CancellationToken.None));
        Assert.False(await store.ConsumeAsync(foreign, Binding(row), receipt, now, CancellationToken.None));
        Assert.True(await store.ConsumeAsync(subject, Binding(row), receipt, now, CancellationToken.None));
        Assert.False(await store.ConsumeAsync(subject, Binding(row), receipt, now, CancellationToken.None));
    }

    [Theory]
    [InlineData("operation")]
    [InlineData("target")]
    [InlineData("payload")]
    public async Task BoundReceiptCannotBeUsedForChangedOperationData(string changed)
    {
        await using var database = CreateContext();
        var subject = Subject();
        var row = Challenge(subject);
        var store = new PostgreSqlStepUpChallengeStore(database);
        await store.AddAsync(row, CancellationToken.None);
        var nonce = Digest();
        var receipt = Digest();
        var now = DateTimeOffset.UtcNow;
        Assert.True(await store.BindWebAuthnChallengeAsync(row.Id, subject, nonce, now, CancellationToken.None));
        Assert.True(await store.MarkWebAuthnVerifiedAsync(row.Id, subject, nonce, receipt, now, CancellationToken.None));
        var other = new StepUpOperationBinding(
            changed == "operation" ? "identity.credentials.delete" : row.OperationType,
            changed == "target" ? "credential/other" : row.TargetReference,
            changed == "payload" ? Digest() : row.PayloadHash);

        Assert.False(await store.ConsumeAsync(subject, other, receipt, now, CancellationToken.None));
        Assert.True(await store.ConsumeAsync(subject, Binding(row), receipt, now, CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpiredChallengesCannotBindVerifyOrConsume(bool alreadyVerified)
    {
        await using var database = CreateContext();
        var subject = Subject();
        var row = Challenge(subject);
        var now = DateTimeOffset.UtcNow;
        row.CreatedAt = now.AddMinutes(-10);
        row.ExpiresAt = now.AddMinutes(-1);
        row.WebAuthnChallengeHash = Digest();
        var receipt = Digest();
        if (alreadyVerified)
        {
            row.VerifiedAt = now.AddMinutes(-2);
            row.VerificationMethod = MfaMethod.WebAuthn;
            row.ReceiptHash = receipt;
        }
        var store = new PostgreSqlStepUpChallengeStore(database);
        await store.AddAsync(row, CancellationToken.None);

        Assert.False(await store.BindWebAuthnChallengeAsync(row.Id, subject, Digest(), now, CancellationToken.None));
        Assert.False(await store.MarkWebAuthnVerifiedAsync(row.Id, subject, row.WebAuthnChallengeHash!, receipt, now, CancellationToken.None));
        Assert.False(await store.ConsumeAsync(subject, Binding(row), receipt, now, CancellationToken.None));
    }

    [Fact]
    public async Task GenericVerificationCannotBypassTheWebAuthnNonceCondition()
    {
        await using var database = CreateContext();
        var subject = Subject();
        var row = Challenge(subject);
        var store = new PostgreSqlStepUpChallengeStore(database);
        await store.AddAsync(row, CancellationToken.None);
        var now = DateTimeOffset.UtcNow;
        var nonce = Digest();
        var receipt = Digest();
        Assert.True(await store.BindWebAuthnChallengeAsync(row.Id, subject, nonce, now, CancellationToken.None));

        Assert.False(await store.MarkVerifiedAsync(row.Id, subject, receipt, MfaMethod.WebAuthn, now, CancellationToken.None));
        Assert.False(await store.MarkWebAuthnVerifiedAsync(row.Id, subject, Digest(), receipt, now, CancellationToken.None));
        Assert.True(await store.MarkWebAuthnVerifiedAsync(row.Id, subject, nonce, receipt, now, CancellationToken.None));
    }

    [Fact]
    public async Task AnotherContextReissuingOptionsInvalidatesAnEarlierVerification()
    {
        await using var first = CreateContext();
        await using var second = CreateContext();
        var subject = Subject();
        var row = Challenge(subject);
        var store = new PostgreSqlStepUpChallengeStore(first);
        var other = new PostgreSqlStepUpChallengeStore(second);
        await store.AddAsync(row, CancellationToken.None);
        var oldNonce = Digest();
        var newNonce = Digest();
        var now = DateTimeOffset.UtcNow;
        Assert.True(await store.BindWebAuthnChallengeAsync(row.Id, subject, oldNonce, now, CancellationToken.None));
        var captured = await store.FindActiveAsync(row.Id, subject, now, CancellationToken.None);
        Assert.Equal(oldNonce, captured!.WebAuthnChallengeHash);
        Assert.True(await other.BindWebAuthnChallengeAsync(row.Id, subject, newNonce, now, CancellationToken.None));

        Assert.False(await store.MarkWebAuthnVerifiedAsync(row.Id, subject, captured.WebAuthnChallengeHash!, Digest(), now, CancellationToken.None));
        Assert.True(await other.MarkWebAuthnVerifiedAsync(row.Id, subject, newNonce, Digest(), now, CancellationToken.None));
        Assert.False(await store.BindWebAuthnChallengeAsync(row.Id, subject, Digest(), now, CancellationToken.None));
    }

    [Fact]
    public async Task ConcurrentVerificationAndConsumptionEachHaveOneWinner()
    {
        var subject = Subject();
        var row = Challenge(subject);
        var nonce = Digest();
        await using (var seed = CreateContext())
        {
            var store = new PostgreSqlStepUpChallengeStore(seed);
            await store.AddAsync(row, CancellationToken.None);
            Assert.True(await store.BindWebAuthnChallengeAsync(row.Id, subject, nonce, DateTimeOffset.UtcNow, CancellationToken.None));
        }

        var verified = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
        {
            await using var context = CreateContext();
            var receipt = Digest();
            var won = await new PostgreSqlStepUpChallengeStore(context).MarkWebAuthnVerifiedAsync(
                row.Id, subject, nonce, receipt, DateTimeOffset.UtcNow, CancellationToken.None);
            return (Won: won, Receipt: receipt);
        }));
        var winner = Assert.Single(verified, result => result.Won);
        var consumed = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
        {
            await using var context = CreateContext();
            return await new PostgreSqlStepUpChallengeStore(context).ConsumeAsync(
                subject, Binding(row), winner.Receipt, DateTimeOffset.UtcNow, CancellationToken.None);
        }));
        Assert.Single(consumed, won => won);
        await using var final = CreateContext();
        var stored = await final.Set<StepUpChallenge>().SingleAsync(value => value.Id == row.Id);
        Assert.Equal(winner.Receipt, stored.ReceiptHash);
        Assert.NotNull(stored.ConsumedAt);
    }

    [Theory]
    [InlineData(MfaMethod.WebAuthn, false)]
    [InlineData(MfaMethod.Totp, true)]
    [InlineData(MfaMethod.BackupCode, true)]
    public async Task LegacyUnboundWebAuthnIsDeniedWithoutBreakingOtherReceiptMethods(MfaMethod method, bool allowed)
    {
        await using var database = CreateContext();
        var subject = Subject();
        var row = Challenge(subject);
        row.VerifiedAt = DateTimeOffset.UtcNow;
        row.VerificationMethod = method;
        row.ReceiptHash = Digest();
        var store = new PostgreSqlStepUpChallengeStore(database);
        await store.AddAsync(row, CancellationToken.None);

        Assert.Equal(allowed, await store.ConsumeAsync(subject, Binding(row), row.ReceiptHash!, DateTimeOffset.UtcNow, CancellationToken.None));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("upper")]
    [InlineData("nonhex")]
    public async Task DatabaseConstraintRejectsMalformedNonceDigests(string fault)
    {
        await using var database = CreateContext();
        var row = Challenge(Subject());
        var digest = Digest();
        row.WebAuthnChallengeHash = fault switch { "short" => "a", "upper" => "A" + digest[1..], _ => "g" + digest[1..] };
        database.Set<StepUpChallenge>().Add(row);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
        var postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.CheckViolation, postgres.SqlState);
        Assert.Equal("ck_step_up_challenges_webauthn_hash", postgres.ConstraintName);
    }

    private ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(fixture.ConnectionString).Options);

    private static StepUpSubject Subject() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    private static string Digest() => Convert.ToHexString(SHA256.HashData(RandomNumberGenerator.GetBytes(32))).ToLowerInvariant();

    private static StepUpOperationBinding Binding(StepUpChallenge row) => new(row.OperationType, row.TargetReference, row.PayloadHash);

    private static StepUpChallenge Challenge(StepUpSubject subject) => new()
    {
        Id = Guid.NewGuid(), TenantId = subject.TenantId, ActorId = subject.ActorId, SessionId = subject.SessionId,
        OperationType = "identity.credentials.rotate", TargetReference = "credential/current", PayloadHash = Digest(),
        CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
    };
}
