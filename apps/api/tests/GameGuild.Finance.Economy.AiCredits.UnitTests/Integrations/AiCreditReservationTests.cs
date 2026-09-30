using FluentAssertions;
using GameGuild.Finance.Economy.Integrations.AI;
using GameGuild.Finance.Economy.Contracts;
using GameGuild.Finance.Economy.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Finance.Economy.AiCredits.UnitTests.Integrations;

public sealed class AiCreditReservationTests
{
    [Fact]
    public void Settle_ChargesActualUsageAndReleasesUnusedMaximum()
    {
        var reservation = AiCreditReservation.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "lesson-authoring", "OpenAi", "gpt-test", 100, "reserve-1", DateTimeOffset.UtcNow);

        reservation.Settle(120, 30, 35, "provider-usage-1", "settle-1", DateTimeOffset.UtcNow);

        reservation.Status.Should().Be(AiCreditReservationStatus.Settled);
        reservation.SettledSoftUnits.Should().Be(35);
        reservation.ReleasedSoftUnits.Should().Be(65);
        reservation.InputTokens.Should().Be(120);
        reservation.OutputTokens.Should().Be(30);
    }

    [Fact]
    public void Settle_RetryWithSameIdempotencyKey_DoesNotChargeTwice()
    {
        var reservation = AiCreditReservation.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "lesson-authoring", "OpenAi", "gpt-test", 80, "reserve-2", DateTimeOffset.UtcNow);
        reservation.Settle(10, 5, 20, "usage-2", "settle-2", DateTimeOffset.UtcNow);

        var act = () => reservation.Settle(10, 5, 20, "usage-2", "settle-2", DateTimeOffset.UtcNow);

        act.Should().NotThrow();
        reservation.SettledSoftUnits.Should().Be(20);
    }

    [Fact]
    public void Release_BeforeProviderConsumption_ReturnsEntireReservation()
    {
        var reservation = AiCreditReservation.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "lesson-authoring", "OpenAi", "gpt-test", 55, "reserve-3", DateTimeOffset.UtcNow);

        reservation.Release("provider failed", "release-3", DateTimeOffset.UtcNow);

        reservation.Status.Should().Be(AiCreditReservationStatus.Released);
        reservation.SettledSoftUnits.Should().Be(0);
        reservation.ReleasedSoftUnits.Should().Be(55);
    }

    [Fact]
    public void Create_RequiresExplicitActorAndTenant()
    {
        var act = () => AiCreditReservation.Create(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid(),
            "lesson-authoring", "OpenAi", "gpt-test", 55, "reserve-4", DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Settlement_ChargesOnlyTheAuthenticatedActorWallet()
    {
        await using var context = new AiCreditTestContext(
            new DbContextOptionsBuilder<AiCreditTestContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var tenantId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        SeedWallet(context, tenantId, teacherId, 100);
        SeedWallet(context, tenantId, studentId, 100);
        await context.SaveChangesAsync();
        var service = new AiCreditWalletService(
            context,
            Options.Create(new AiCreditPricingOptions
            {
                DefaultInputSoftUnitsPerMillion = 1_000_000,
                DefaultOutputSoftUnitsPerMillion = 1_000_000,
            }),
            TimeProvider.System);
        var quote = await service.QuoteAsync("lesson-authoring", "OpenAi", "test", 5, 5);
        var runId = Guid.NewGuid();

        await service.ReserveAsync(runId, tenantId, teacherId, "lesson-authoring", quote, "teacher-run");
        await service.SettleAsync(runId, 3, 2, "provider-usage", "teacher-settlement");

        var teacher = await service.GetBalanceAsync(tenantId, teacherId);
        var student = await service.GetBalanceAsync(tenantId, studentId);
        teacher.AvailableSoftUnits.Should().Be(95);
        student.AvailableSoftUnits.Should().Be(100);
    }

    [Fact]
    public async Task Quote_ReusesThePersistedRateCardForTheSameProviderModelAndService()
    {
        await using var context = new AiCreditTestContext(
            new DbContextOptionsBuilder<AiCreditTestContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var service = new AiCreditWalletService(
            context,
            Options.Create(new AiCreditPricingOptions()),
            TimeProvider.System);

        var first = await service.QuoteAsync("lesson-authoring", "OpenAi", "test", 5, 5);
        var second = await service.QuoteAsync("lesson-authoring", "OpenAi", "test", 10, 10);

        first.RateCardVersion.Should().Be(second.RateCardVersion);
        (await context.Set<AiCreditRateCard>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Reserve_IdempotencyKeyCannotBeReusedWithAnotherPriceSnapshot()
    {
        await using var context = new AiCreditTestContext(
            new DbContextOptionsBuilder<AiCreditTestContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        SeedWallet(context, tenantId, actorId, 100);
        await context.SaveChangesAsync();
        var service = new AiCreditWalletService(
            context,
            Options.Create(new AiCreditPricingOptions()),
            TimeProvider.System);
        var runId = Guid.NewGuid();
        var quote = new AiCreditQuote("rate-v1", "OpenAi", "model-a", 10, 10, 25, 100, 200);
        await service.ReserveAsync(runId, tenantId, actorId, "lesson-authoring", quote, "same-key");

        var replay = () => service.ReserveAsync(
            runId,
            tenantId,
            actorId,
            "lesson-authoring",
            quote with { MaximumSoftUnits = 26 },
            "same-key");

        await replay.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*another reservation request*");
        (await context.Set<AiCreditReservation>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public void Settle_CannotChargeMoreThanTheReservation()
    {
        var reservation = CreateReservation();

        var act = () => reservation.Settle(10, 10, 101, "usage", "settle", DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>().WithMessage("*maximum reservation*");
        reservation.Status.Should().Be(AiCreditReservationStatus.Reserved);
        reservation.SettledSoftUnits.Should().Be(0);
        reservation.Version.Should().Be(1);
    }

    [Fact]
    public void Settle_CannotReplaceAnExistingSettlement()
    {
        var reservation = CreateReservation();
        var settledAt = DateTimeOffset.UtcNow;
        reservation.Settle(10, 5, 20, "usage", "settle", settledAt);

        var act = () => reservation.Settle(12, 8, 30, "other-usage", "other-key", settledAt.AddMinutes(1));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Only a reserved*");
        reservation.SettledSoftUnits.Should().Be(20);
        reservation.ProviderUsageId.Should().Be("usage");
        reservation.SettledAt.Should().Be(settledAt);
        reservation.Version.Should().Be(2);
    }

    [Fact]
    public void Settle_CannotChargeAReleasedReservation()
    {
        var reservation = CreateReservation();
        reservation.Release("cancelled", "release", DateTimeOffset.UtcNow);

        var act = () => reservation.Settle(10, 5, 20, "usage", "settle", DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Only a reserved*");
        reservation.Status.Should().Be(AiCreditReservationStatus.Released);
        reservation.SettledSoftUnits.Should().Be(0);
        reservation.ReleasedSoftUnits.Should().Be(100);
    }

    [Fact]
    public void Release_RetryWithTheSameTrimmedKeyDoesNotChangeTheDecision()
    {
        var reservation = CreateReservation();
        var releasedAt = DateTimeOffset.UtcNow;
        reservation.Release(" provider failed ", " release ", releasedAt);

        reservation.Release("retry", " release ", releasedAt.AddMinutes(1));

        reservation.ReleaseReason.Should().Be("provider failed");
        reservation.SettlementIdempotencyKey.Should().Be("release");
        reservation.ReleasedAt.Should().Be(releasedAt);
        reservation.ReleasedSoftUnits.Should().Be(100);
        reservation.Version.Should().Be(2);
    }

    [Fact]
    public void Release_CannotReplaceAnExistingRelease()
    {
        var reservation = CreateReservation();
        reservation.Release("cancelled", "release", DateTimeOffset.UtcNow);

        var act = () => reservation.Release("other reason", "other-key", DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Only a reserved*");
        reservation.ReleaseReason.Should().Be("cancelled");
        reservation.Version.Should().Be(2);
    }

    [Fact]
    public void Release_CannotRefundAnAlreadySettledCharge()
    {
        var reservation = CreateReservation();
        reservation.Settle(10, 5, 20, "usage", "settle", DateTimeOffset.UtcNow);

        var act = () => reservation.Release("cancelled", "release", DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Only a reserved*");
        reservation.SettledSoftUnits.Should().Be(20);
        reservation.ReleasedSoftUnits.Should().Be(80);
        reservation.Status.Should().Be(AiCreditReservationStatus.Settled);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 1)]
    [InlineData(0, 1, 0, 0, 1)]
    [InlineData(0, 0, 1_000_000, 1_000_000, 0)]
    [InlineData(3, 2, 1_000_000, 1_000_000, 5)]
    [InlineData(1, 1, 1, 1, 2)]
    [InlineData(3, 2, 500_000, 250_000, 3)]
    public void RateCard_RoundsEachTokenRateUpAndAppliesTheMinimumCharge(
        int inputTokens, int outputTokens, long inputRate, long outputRate, long expected)
    {
        var card = AiCreditRateCard.Create("v1", "authoring", "OpenAi", "test", inputRate, outputRate, DateTimeOffset.UtcNow);

        card.Price(inputTokens, outputTokens).Should().Be(expected);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Balance_RequiresBothAuthenticatedTenantAndActor(bool missingTenant, bool missingActor)
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var act = () => service.GetBalanceAsync(
            missingTenant ? Guid.Empty : Guid.NewGuid(), missingActor ? Guid.Empty : Guid.NewGuid());

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*authenticated tenant actor*");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Balance_FailsClosedWithoutAPersistedWalletAndProjection(bool hasWallet)
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        if (hasWallet)
        {
            SeedWallet(context, tenantId, actorId, 100);
            context.Set<EconomyWalletBalanceProjectionRow>().RemoveRange(context.Set<EconomyWalletBalanceProjectionRow>().Local);
            await context.SaveChangesAsync();
        }
        var service = CreateService(context);

        var act = () => service.GetBalanceAsync(tenantId, actorId);

        var exception = (await act.Should().ThrowAsync<InsufficientAiCreditsException>()).Which;
        exception.Available.Should().Be(0);
        exception.Required.Should().Be(1);
    }

    [Fact]
    public async Task Reserve_ReportsTheShortfallWithoutPersistingACharge()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        SeedWallet(context, tenantId, actorId, 10);
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var quote = new AiCreditQuote("v1", "OpenAi", "test", 10, 10, 20, 1_000_000, 1_000_000);

        var act = () => service.ReserveAsync(Guid.NewGuid(), tenantId, actorId, "authoring", quote, "reserve");

        var exception = (await act.Should().ThrowAsync<InsufficientAiCreditsException>()).Which;
        exception.Available.Should().Be(10);
        exception.Required.Should().Be(20);
        exception.Message.Should().Contain("Available: 10; required: 20");
        (await context.Set<AiCreditReservation>().CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 1)]
    [InlineData(0, 1, 0, 0, 1)]
    [InlineData(0, 0, 1_000_000, 1_000_000, 0)]
    [InlineData(3, 2, 1_000_000, 1_000_000, 5)]
    [InlineData(1, 1, 1, 1, 2)]
    [InlineData(3, 2, 500_000, 250_000, 3)]
    public async Task Settlement_UsesTheStoredPriceSnapshot(
        int inputTokens, int outputTokens, long inputRate, long outputRate, long expected)
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        SeedWallet(context, tenantId, actorId, 100);
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var runId = Guid.NewGuid();
        var quote = new AiCreditQuote("quoted-v1", "OpenAi", "test", 10, 10, 20, inputRate, outputRate);
        var reserved = await service.ReserveAsync(runId, tenantId, actorId, "authoring", quote, "reserve");

        var replay = await service.ReserveAsync(runId, tenantId, actorId, "authoring", quote, "reserve");
        replay.Id.Should().Be(reserved.Id);
        var settled = await service.SettleAsync(runId, inputTokens, outputTokens, "usage", "settle");

        settled.SettledSoftUnits.Should().Be(expected);
        settled.ReleasedSoftUnits.Should().Be(20 - expected);
        settled.RateCardVersion.Should().Be("quoted-v1");
        var balance = await service.GetBalanceAsync(tenantId, actorId);
        balance.Should().Be(new AiCreditBalance(100 - expected, 0, expected));
        (await context.Set<AiCreditReservation>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Release_RestoresTheAvailableWalletBalance()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        SeedWallet(context, tenantId, actorId, 100);
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var runId = Guid.NewGuid();
        var quote = new AiCreditQuote("v1", "OpenAi", "test", 10, 10, 20, 1, 1);
        await service.ReserveAsync(runId, tenantId, actorId, "authoring", quote, "reserve");
        (await service.GetBalanceAsync(tenantId, actorId)).Should().Be(new AiCreditBalance(80, 20, 0));

        var released = await service.ReleaseAsync(runId, "provider failed", "release");

        released.Status.Should().Be(AiCreditReservationStatus.Released);
        released.ReleasedSoftUnits.Should().Be(20);
        (await service.GetBalanceAsync(tenantId, actorId)).Should().Be(new AiCreditBalance(100, 0, 0));
    }

    private static AiCreditReservation CreateReservation() => AiCreditReservation.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        "authoring", "OpenAi", "test", 100, "reserve", DateTimeOffset.UtcNow);

    private static AiCreditTestContext CreateContext() => new(
        new DbContextOptionsBuilder<AiCreditTestContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static AiCreditWalletService CreateService(AiCreditTestContext context) => new(
        context, Options.Create(new AiCreditPricingOptions()), TimeProvider.System);

    private static void SeedWallet(AiCreditTestContext context, Guid tenantId, Guid actorId, long softBalance)
    {
        var walletId = Guid.NewGuid();
        context.Set<EconomyWalletRow>().Add(new EconomyWalletRow
        {
            Id = walletId,
            TenantId = tenantId,
            OwnerId = actorId,
            State = WalletLifecycleState.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        context.Set<EconomyWalletBalanceProjectionRow>().Add(new EconomyWalletBalanceProjectionRow
        {
            WalletId = walletId,
            Soft = softBalance,
            AvailableSoftToSpend = softBalance,
            ProjectionHash = "test",
            RebuiltAt = DateTimeOffset.UtcNow,
        });
    }

    private sealed class AiCreditTestContext(DbContextOptions<AiCreditTestContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EconomyWalletRow>().HasKey(item => item.Id);
            modelBuilder.Entity<EconomyWalletBalanceProjectionRow>().HasKey(item => item.WalletId);
            modelBuilder.Entity<AiCreditReservation>().HasKey(item => item.Id);
            modelBuilder.Entity<AiCreditRateCard>().HasKey(item => item.Id);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
