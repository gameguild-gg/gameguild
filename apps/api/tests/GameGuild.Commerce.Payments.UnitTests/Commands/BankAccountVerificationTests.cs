using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.BankAccountVerification;

/// <summary>
///     Micro-deposit bank-account verification (issue #414): challenge generation bounds and
///     hashing, the 3-business-day deadline, the 3-attempt cap, expiry, owner-only access,
///     idempotent initiation, and the charge-candidate gate (unverified bank accounts never
///     resolve as charge candidates).
/// </summary>
public class BankAccountVerificationTests
{
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();
    private readonly Mock<IPaymentMethodRepository> _paymentMethods = new();
    private readonly BankAccountVerificationTestDbContext _db = NewDatabase();
    private readonly BankAccountVerificationRepository _verifications;
    private readonly ActorContext _actor;
    private readonly PaymentMethod _bankAccount;

    public BankAccountVerificationTests()
    {
        _actor = AuthenticatedActor();
        _actorAccessor.SetupGet(accessor => accessor.ActorContext).Returns(_actor);
        _verifications = new BankAccountVerificationRepository(_db, NullLogger<BankAccountVerificationRepository>.Instance);

        _bankAccount = NewBankAccount(tenantId: _actor.TenantId!.Value, userId: _actor.UserId());
        _paymentMethods
            .Setup(repository => repository.GetByIdAsync(_bankAccount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_bankAccount);
    }

    // ---------- Rules ----------

    [Fact]
    public void GenerateChallenge_ProducesTwoDistinctBoundedAmounts()
    {
        for (var attempt = 0; attempt < 25; attempt++)
        {
            var (first, second, salt) = BankAccountVerificationRules.GenerateChallenge();
            first.Should().BeInRange(BankAccountVerificationRules.MinimumAmount, BankAccountVerificationRules.MaximumAmount);
            second.Should().BeInRange(BankAccountVerificationRules.MinimumAmount, BankAccountVerificationRules.MaximumAmount);
            first.Should().NotBe(second);
            salt.Should().HaveLength(32); // 16 bytes hex
        }
    }

    [Theory]
    [InlineData("2026-10-26", "2026-10-29")] // Monday initiation: Mon->Tue->Wed->Thu
    [InlineData("2026-10-27", "2026-10-30")] // Tuesday
    [InlineData("2026-10-28", "2026-11-02")] // Wednesday skips the weekend
    [InlineData("2026-10-30", "2026-11-04")] // Friday skips the weekend
    [InlineData("2026-10-31", "2026-11-04")] // Saturday skips the weekend
    public void DeadlineFromInitiation_SkipsWeekends(string startIso, string expectedIso)
    {
        var start = DateTime.Parse(startIso, System.Globalization.CultureInfo.InvariantCulture);
        BankAccountVerificationRules.DeadlineFromInitiation(start).Should().Be(DateTime.Parse(expectedIso, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void HashDeposit_IsSaltedAndDeterministic()
    {
        var hashA1 = BankAccountVerificationRules.HashDeposit("aabb", 0.15m);
        var hashA2 = BankAccountVerificationRules.HashDeposit("aabb", 0.15m);
        var hashB = BankAccountVerificationRules.HashDeposit("ccdd", 0.15m);
        var hashC = BankAccountVerificationRules.HashDeposit("aabb", 0.16m);

        hashA1.Should().Be(hashA2);      // deterministic
        hashA1.Should().NotBe(hashB);    // salt matters
        hashA1.Should().NotBe(hashC);    // amount matters
        hashA1.Should().HaveLength(64);  // SHA-256 hex
        BankAccountVerificationRules.HashDeposit("aabb", 0.15004m).Should().Be(hashA1); // two-decimal normalization
    }

    // ---------- Initiate ----------

    [Fact]
    public async Task Initiate_GeneratesChallenge_AndStoresOnlyHashes()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new InitiateBankAccountVerificationCommand(_bankAccount.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Deposit1Amount.Should().BeInRange(0.01m, 0.99m);
        result.Value.Deposit2Amount.Should().BeInRange(0.01m, 0.99m);
        result.Value.Status.Status.Should().Be("pending");
        var row = await _db.Set<GameGuild.Commerce.Payments.BankAccountVerification>().SingleAsync();
        row.Deposit1Hash.Should().NotContain(result.Value.Deposit1Amount.ToString());
        row.Deposit1Hash.Should().Be(BankAccountVerificationRules.HashDeposit(row.Salt, result.Value.Deposit1Amount));
        row.IsVerified.Should().BeFalse();
    }

    [Fact]
    public async Task Initiate_WhileChallengeOpen_IsIdempotentConflict()
    {
        var handler = CreateHandler();
        await handler.Handle(new InitiateBankAccountVerificationCommand(_bankAccount.Id), CancellationToken.None);

        var second = await handler.Handle(new InitiateBankAccountVerificationCommand(_bankAccount.Id), CancellationToken.None);

        second.IsSuccess.Should().BeFalse();
        second.Error.Code.Should().Be("PaymentMethods.BankVerificationPending");
        (await _db.Set<GameGuild.Commerce.Payments.BankAccountVerification>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Initiate_NonBankAccountMethod_IsRejected()
    {
        var card = NewCard(tenantId: _actor.TenantId!.Value, userId: _actor.UserId());
        _paymentMethods.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        var handler = CreateHandler();

        var result = await handler.Handle(new InitiateBankAccountVerificationCommand(card.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.NotABankAccount");
    }

    [Fact]
    public async Task Initiate_ForeignOwner_IsForbidden()
    {
        var foreign = NewBankAccount(tenantId: Guid.NewGuid(), userId: Guid.NewGuid());
        _paymentMethods.Setup(r => r.GetByIdAsync(foreign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(foreign);
        var handler = CreateHandler();

        var result = await handler.Handle(new InitiateBankAccountVerificationCommand(foreign.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.NotOwner");
    }

    // ---------- Verify ----------

    [Fact]
    public async Task Verify_CorrectAmounts_MarksVerifiedAndChargeable()
    {
        var handler = CreateHandler();
        var initiation = await handler.Handle(new InitiateBankAccountVerificationCommand(_bankAccount.Id), CancellationToken.None);

        var result = await handler.Handle(
            new VerifyBankAccountDepositsCommand(_bankAccount.Id, initiation.Value.Deposit1Amount, initiation.Value.Deposit2Amount),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("verified");
        (await _verifications.GetLatestAsync(_bankAccount.Id)).IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task Verify_SwappedAmounts_FailAndConsumeAnAttempt()
    {
        var handler = CreateHandler();
        var initiation = await handler.Handle(new InitiateBankAccountVerificationCommand(_bankAccount.Id), CancellationToken.None);

        var result = await handler.Handle(
            new VerifyBankAccountDepositsCommand(_bankAccount.Id, initiation.Value.Deposit2Amount, initiation.Value.Deposit1Amount),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.BankVerificationAmountsMismatched");
        var row = await _verifications.GetLatestAsync(_bankAccount.Id);
        row.FailedAttempts.Should().Be(1);
        row.IsVerified.Should().BeFalse();
    }

    [Fact]
    public async Task Verify_ThreeFailures_LockTheChallenge()
    {
        var handler = CreateHandler();
        await handler.Handle(new InitiateBankAccountVerificationCommand(_bankAccount.Id), CancellationToken.None);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var result = await handler.Handle(new VerifyBankAccountDepositsCommand(_bankAccount.Id, 0.01m, 0.02m), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
        }

        var row = await _verifications.GetLatestAsync(_bankAccount.Id);
        row.FailedAttempts.Should().Be(3);
        row.IsLocked.Should().BeTrue();

        var status = await new GetBankAccountVerificationQueryHandler(_paymentMethods.Object, _verifications, _actorAccessor.Object)
            .Handle(new GetBankAccountVerificationQuery(_bankAccount.Id), CancellationToken.None);
        status.Value.Status.Should().Be("locked");

        // A locked challenge refuses further echoes and demands re-initiation.
        var fourth = await handler.Handle(new VerifyBankAccountDepositsCommand(_bankAccount.Id, 0.01m, 0.02m), CancellationToken.None);
        fourth.Error.Code.Should().Be("PaymentMethods.BankVerificationLocked");

        // Re-initiation after lock is allowed (fresh challenge).
        var reinitiated = await handler.Handle(new InitiateBankAccountVerificationCommand(_bankAccount.Id), CancellationToken.None);
        reinitiated.IsSuccess.Should().BeTrue();
        reinitiated.Value.Status.Status.Should().Be("pending");
    }

    [Fact]
    public async Task Verify_AfterDeadline_IsExpiredAndDemandsReinitiation()
    {
        var frozen = new DateTime(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc); // Monday
        var handler = CreateHandler(utcNow: () => frozen);
        var initiation = await handler.Handle(new InitiateBankAccountVerificationCommand(_bankAccount.Id), CancellationToken.None);

        var late = CreateHandler(utcNow: () => frozen.AddDays(5));
        var result = await late.Handle(
            new VerifyBankAccountDepositsCommand(_bankAccount.Id, initiation.Value.Deposit1Amount, initiation.Value.Deposit2Amount),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentMethods.BankVerificationExpired");

        var reinitiated = await late.Handle(new InitiateBankAccountVerificationCommand(_bankAccount.Id), CancellationToken.None);
        reinitiated.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Verify_CorrectAmountsAfterTwoFailures_StillVerifies()
    {
        var handler = CreateHandler();
        var initiation = await handler.Handle(new InitiateBankAccountVerificationCommand(_bankAccount.Id), CancellationToken.None);

        await handler.Handle(new VerifyBankAccountDepositsCommand(_bankAccount.Id, 0.01m, 0.02m), CancellationToken.None);
        await handler.Handle(new VerifyBankAccountDepositsCommand(_bankAccount.Id, 0.03m, 0.04m), CancellationToken.None);
        var result = await handler.Handle(
            new VerifyBankAccountDepositsCommand(_bankAccount.Id, initiation.Value.Deposit1Amount, initiation.Value.Deposit2Amount),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("verified");
    }

    // ---------- Charge gate ----------

    [Fact]
    public async Task ChargeResolver_ExcludesUnverifiedBankAccounts_AndIncludesVerifiedOnes()
    {
        var repository = new Mock<IPaymentMethodRepository>();
        repository
            .Setup(r => r.ListAsync(_actor.TenantId!.Value, _actor.UserId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_bankAccount]);

        // Legacy construction (no verification store): bank accounts fail closed to excluded.
        var legacyResolver = new PaymentMethodChargeResolver(repository.Object);
        (await legacyResolver.ResolveChargeCandidatesAsync(_actor.TenantId!.Value, _actor.UserId(), null)).Should().BeEmpty();

        // With the store wired: still excluded while unverified...
        var resolver = new PaymentMethodChargeResolver(repository.Object, bankAccountVerifications: _verifications);
        (await resolver.ResolveChargeCandidatesAsync(_actor.TenantId!.Value, _actor.UserId(), null)).Should().BeEmpty();

        // ...and included after a successful verification round-trip.
        var handler = CreateHandler();
        var initiation = await handler.Handle(new InitiateBankAccountVerificationCommand(_bankAccount.Id), CancellationToken.None);
        await handler.Handle(
            new VerifyBankAccountDepositsCommand(_bankAccount.Id, initiation.Value.Deposit1Amount, initiation.Value.Deposit2Amount),
            CancellationToken.None);
        var candidates = await resolver.ResolveChargeCandidatesAsync(_actor.TenantId!.Value, _actor.UserId(), null);
        candidates.Should().ContainSingle();
    }

    // ---------- Helpers ----------

    private BankAccountVerificationCommandHandler CreateHandler(Func<DateTime>? utcNow = null) =>
        new(_paymentMethods.Object, _verifications, _actorAccessor.Object)
        {
            UtcNow = utcNow ?? (static () => SystemClock.UtcNow),
        };

    private static PaymentMethod NewBankAccount(Guid tenantId, Guid userId) => new(
        tenantId,
        userId,
        "stripe",
        "enc1:test",
        PaymentMethodType.BankAccount,
        false);

    private static PaymentMethod NewCard(Guid tenantId, Guid userId) => new(
        tenantId,
        userId,
        "stripe",
        "enc1:test-card",
        PaymentMethodType.Card,
        false);

    private static BankAccountVerificationTestDbContext NewDatabase()
    {
        var options = new DbContextOptionsBuilder<BankAccountVerificationTestDbContext>()
            .UseInMemoryDatabase($"bank-verifications-{Guid.NewGuid():N}")
            .Options;
        return new BankAccountVerificationTestDbContext(options);
    }

    private ActorContext AuthenticatedActor() => new()
    {
        ActorKind = ActorKind.User,
        SubjectId = Guid.NewGuid().ToString(),
        TenantId = Guid.NewGuid(),
        Roles = new HashSet<string>(),
        Permissions = new HashSet<string>(),
        TypedAttributes = ActorAttributes.Empty,
        AuthScheme = "Test",
        IsAuthenticated = true
    };
}

/// <summary>
///     In-memory context for the bank-account verification tests.
/// </summary>
internal sealed class BankAccountVerificationTestDbContext(DbContextOptions<BankAccountVerificationTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IDbContextTransaction>(BankVerificationNoOpTransaction.Instance);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BankAccountVerificationConfiguration());
        base.OnModelCreating(modelBuilder);
    }

    private sealed class BankVerificationNoOpTransaction : IDbContextTransaction
    {
        public static readonly BankVerificationNoOpTransaction Instance = new();

        public Guid TransactionId { get => Guid.Empty; }

        public void Dispose() { }

        public ValueTask DisposeAsync() { return ValueTask.CompletedTask; }

        public void Commit() { }

        public void Rollback() { }

        public Task CommitAsync(CancellationToken cancellationToken = default) { return Task.CompletedTask; }

        public Task RollbackAsync(CancellationToken cancellationToken = default) { return Task.CompletedTask; }
    }
}

file static class ActorExtensions
{
    public static Guid UserId(this ActorContext actor) => Guid.Parse(actor.SubjectId!);
}
