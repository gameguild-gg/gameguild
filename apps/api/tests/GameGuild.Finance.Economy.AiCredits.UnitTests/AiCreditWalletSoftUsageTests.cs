using FluentAssertions;
using GameGuild;
using GameGuild.AI;
using GameGuild.Finance.Economy.Contracts;
using GameGuild.Finance.Economy.Integrations.AI;
using GameGuild.Finance.Economy.Persistence;
using GameGuild.Finance.Economy.Queries;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameGuild.Finance.Economy.AiCredits.UnitTests;

public sealed class AiCreditWalletSoftUsageTests
{
    private static readonly DateTimeOffset RecordedAt = DateTimeOffset.Parse("2026-09-11T12:00:00Z");

    [Fact]
    public async Task Contributor_SumsReservedAndSettledReservationsForTheWalletOnly()
    {
        var walletId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        await using var context = CreateContext(walletId, ownerId, tenantId, includeSettledAndOtherWallets: true);
        var contributor = new AiCreditWalletSoftUsageContributor(context);

        var usage = await contributor.GetUsageAsync(walletId);

        usage.Should().Be(new EconomyWalletSoftUsage(20, 7));
    }

    [Fact]
    public async Task Contributor_ReturnsZeroUsageForAWalletWithoutReservations()
    {
        var walletId = Guid.NewGuid();
        await using var context = CreateContext(walletId, Guid.NewGuid(), Guid.NewGuid(), includeSettledAndOtherWallets: false);
        var contributor = new AiCreditWalletSoftUsageContributor(context);

        var usage = await contributor.GetUsageAsync(Guid.NewGuid());

        usage.Should().Be(new EconomyWalletSoftUsage(0, 0));
    }

    [Fact]
    public async Task WalletView_MergesContributedAiCreditUsageIntoTheSharedSoftBalance()
    {
        var walletId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        await using var context = CreateContext(walletId, ownerId, tenantId, includeSettledAndOtherWallets: false, runId: runId);

        var reservedView = await new GetMyEconomyWalletQueryHandler(
                context,
                CreateActor(ownerId, tenantId),
                [new AiCreditWalletSoftUsageContributor(context)])
            .Handle(new GetMyEconomyWalletQuery(), CancellationToken.None);

        reservedView!.HeldSoft.Should().Be(20);
        reservedView.AvailableSoftToSpend.Should().Be(80);

        var reservation = await context.Set<AiCreditReservation>().SingleAsync(item => item.RunId == runId);
        reservation.Settle(5, 2, 7, "provider-usage", "settle-shared-balance", RecordedAt.AddMinutes(1));
        await context.SaveChangesAsync();

        var settledView = await new GetMyEconomyWalletQueryHandler(
                context,
                CreateActor(ownerId, tenantId),
                [new AiCreditWalletSoftUsageContributor(context)])
            .Handle(new GetMyEconomyWalletQuery(), CancellationToken.None);

        settledView!.HeldSoft.Should().Be(0);
        settledView.AvailableSoftToSpend.Should().Be(93);
    }

    [Fact]
    public async Task WalletView_IgnoresContributedUsageWhenNoContributorIsRegistered()
    {
        var walletId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        await using var context = CreateContext(walletId, ownerId, tenantId, includeSettledAndOtherWallets: false);

        var view = await new GetMyEconomyWalletQueryHandler(
                context,
                CreateActor(ownerId, tenantId))
            .Handle(new GetMyEconomyWalletQuery(), CancellationToken.None);

        view!.HeldSoft.Should().Be(0);
        view.AvailableSoftToSpend.Should().Be(100);
    }

    [Fact]
    public async Task ExecutionRecorder_AttributesTerminalExecutionsToTheAiCreditDomain()
    {
        var recorder = new AiCreditExecutionBillingRecorder(NullLogger<AiCreditExecutionBillingRecorder>.Instance);

        var act = () => recorder.RecordExecutionAsync(new AiExecutionBillingRecord(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "OpenAi",
            "gpt-test",
            3,
            2,
            5,
            "Completed",
            null,
            null,
            RecordedAt)).AsTask();

        await act.Should().NotThrowAsync();
    }

    private static AiCreditWalletViewTestContext CreateContext(
        Guid walletId,
        Guid ownerId,
        Guid tenantId,
        bool includeSettledAndOtherWallets,
        Guid? runId = null)
    {
        var context = new AiCreditWalletViewTestContext(
            new DbContextOptionsBuilder<AiCreditWalletViewTestContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        context.Set<EconomyWalletRow>().Add(new EconomyWalletRow
        {
            Id = walletId,
            OwnerId = ownerId,
            TenantId = tenantId,
            State = WalletLifecycleState.Active,
            CreatedAt = RecordedAt,
        });
        context.Set<EconomyWalletBalanceProjectionRow>().Add(new EconomyWalletBalanceProjectionRow
        {
            WalletId = walletId,
            Soft = 100,
            AvailableSoftToSpend = 100,
            ProjectionHash = "test",
            RebuiltAt = RecordedAt,
        });
        context.Set<AiCreditReservation>().Add(AiCreditReservation.Create(
            runId ?? Guid.NewGuid(),
            tenantId,
            ownerId,
            walletId,
            "lesson-authoring",
            "OpenAi",
            "test-model",
            20,
            "reserve-shared-balance",
            RecordedAt));
        if (includeSettledAndOtherWallets)
        {
            var settled = AiCreditReservation.Create(
                Guid.NewGuid(),
                tenantId,
                ownerId,
                walletId,
                "lesson-authoring",
                "OpenAi",
                "test-model",
                7,
                "reserve-settled",
                RecordedAt);
            settled.Settle(3, 2, 7, "provider-usage-settled", "settle-early", RecordedAt.AddMinutes(1));
            context.Set<AiCreditReservation>().Add(settled);
            context.Set<AiCreditReservation>().Add(AiCreditReservation.Create(
                Guid.NewGuid(),
                tenantId,
                ownerId,
                Guid.NewGuid(),
                "lesson-authoring",
                "OpenAi",
                "test-model",
                50,
                "reserve-other-wallet",
                RecordedAt));
        }
        context.SaveChanges();
        return context;
    }

    private static ActorContextAccessor CreateActor(Guid userId, Guid tenantId)
    {
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = userId.ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            IsAuthenticated = true
        });
        return accessor;
    }

    private sealed class AiCreditWalletViewTestContext(DbContextOptions<AiCreditWalletViewTestContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // The wallet view handler reads the shared economy wallet model
            // (wallet row, balance projection, and debt projection), so the
            // test context applies the real economy model configuration
            // alongside the AI-credit model configuration it extends.
            new EconomyModelConfiguration().Configure(modelBuilder);
            new AiCreditsModelConfiguration().Configure(modelBuilder);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
