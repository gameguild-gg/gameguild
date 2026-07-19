using FluentAssertions;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Funding;
using GameGuild.Economy.Ledger;

namespace GameGuild.Economy.UnitTests.Funding;

public sealed class ProviderReversalServiceTests
{
    private static readonly DateTimeOffset Time = DateTimeOffset.Parse("2026-07-18T12:00:00Z");

    [Fact]
    public void ReverseTopUp_TraversesConvertedSoftAndHardDescendantsAtExactRootParity()
    {
        var fixture = Setup(10);
        fixture.Service.ConvertHardToSoft(Convert(fixture, 4));

        var reversal = fixture.Service.ReverseTopUp(Reverse(
            fixture.SourceId,
            cumulativeHardUnits: 5,
            ProviderReversalDisposition.ResponsibleDebt));

        reversal.State.CumulativeProviderHardUnits.Should().Be(5);
        reversal.State.RecoveredConvertedSoftUnits.Should().Be(4_000);
        reversal.State.RecoveredHardUnits.Should().Be(1);
        reversal.State.ResponsibleDebtHardUnits.Should().Be(0);
        reversal.State.PlatformLossHardUnits.Should().Be(0);
        reversal.State.ReversedRanges.Sum(range => range.Length).Should().Be(5_000);
        fixture.Store.GetAvailableLots(fixture.WalletId, CurrencyCode.SoftCoin).Should().BeEmpty();
        fixture.Store.GetAvailableLots(fixture.WalletId, CurrencyCode.HardCoin)
            .Should().ContainSingle().Which.Amount.Units.Should().Be(5);
        fixture.Store.FundingClaims.Single().State.Should().Be(SourceConfirmationState.Disputed);
    }

    [Fact]
    public void ReverseTopUp_ExtendsCumulativeRefundWithoutReversingAnyRangeTwice()
    {
        var fixture = Setup(10);
        fixture.Service.ReverseTopUp(Reverse(fixture.SourceId, 3, ProviderReversalDisposition.ResponsibleDebt));

        var extended = fixture.Service.ReverseTopUp(Reverse(
            fixture.SourceId, 8, ProviderReversalDisposition.ResponsibleDebt));
        var full = fixture.Service.ReverseTopUp(Reverse(
            fixture.SourceId, 10, ProviderReversalDisposition.ResponsibleDebt));

        extended.State.CumulativeProviderHardUnits.Should().Be(8);
        full.State.CumulativeProviderHardUnits.Should().Be(10);
        full.State.ReversedRanges.Should().HaveCount(3);
        full.State.ReversedRanges.Sum(range => range.Length).Should().Be(10_000);
        full.State.ReversedRanges.Should().OnlyHaveUniqueItems();
        fixture.Store.FundingClaims.Single().State.Should().Be(SourceConfirmationState.Reversed);
        fixture.Store.CreditLots.Should().ContainSingle();
    }

    [Theory]
    [InlineData(ProviderReversalDisposition.ResponsibleDebt, 1, 0)]
    [InlineData(ProviderReversalDisposition.PlatformLoss, 0, 1)]
    public void ReverseTopUp_ExactlyPartitionsIrrecoverableFeeConsumption(
        ProviderReversalDisposition disposition,
        long expectedDebt,
        long expectedLoss)
    {
        var fixture = Setup(10);
        fixture.Service.ConvertHardToSoft(Convert(fixture, 4, fee: 1));

        var reversal = fixture.Service.ReverseTopUp(Reverse(fixture.SourceId, 10, disposition));

        reversal.State.RecoveredHardUnits.Should().Be(5);
        reversal.State.RecoveredConvertedSoftUnits.Should().Be(4_000);
        reversal.State.ResponsibleDebtHardUnits.Should().Be(expectedDebt);
        reversal.State.PlatformLossHardUnits.Should().Be(expectedLoss);
        reversal.State.PartitionedHardEquivalentUnits.Should().Be(10);
        reversal.Postings.SelectMany(posting => posting.Lines)
            .Should().Contain(line => line.JournalLineId != Guid.Empty);
    }

    [Fact]
    public void ReverseTopUp_DuplicateWebhookIsIdempotentAndCannotExceedConfirmedProviderTotal()
    {
        var fixture = Setup(10);
        var command = Reverse(fixture.SourceId, 5, ProviderReversalDisposition.ResponsibleDebt);

        var first = fixture.Service.ReverseTopUp(command);
        var duplicate = fixture.Service.ReverseTopUp(command);

        duplicate.Should().BeEquivalentTo(first);
        fixture.Store.ProviderReversalStates.Should().ContainSingle();
        FluentActions.Invoking(() => fixture.Service.ReverseTopUp(
                Reverse(fixture.SourceId, 11, ProviderReversalDisposition.ResponsibleDebt)))
            .Should().Throw<ProviderMonetaryTotalExceededException>();
        fixture.Store.ProviderReversalStates.Single().CumulativeProviderHardUnits.Should().Be(5);
    }

    [Fact]
    public void ReverseTopUp_DoesNotTouchUnrelatedRoot()
    {
        var first = Setup(5);
        var unrelated = first.Service.ObserveTopUp(Observe(first.WalletId, 7));
        first.Service.ConfirmObservedTopUp(Confirm(unrelated));

        first.Service.ReverseTopUp(Reverse(
            first.SourceId, 5, ProviderReversalDisposition.ResponsibleDebt));

        first.Store.GetAvailableLots(first.WalletId, CurrencyCode.HardCoin)
            .Should().ContainSingle().Which.Ranges.Should().OnlyContain(range => range.Root == unrelated.SourceId);
    }

    [Fact]
    public void Planner_RejectsForeignHistoryRegressionAndParityFractions()
    {
        var root = SourceStampId.New();
        var foreign = SourceStampId.New();

        FluentActions.Invoking(() => ProviderReversalPlanner.Plan(
                root, 1_000, [new RootTraceRange(foreign, 0, 1_000, 0)], []))
            .Should().Throw<LineageConservationException>();
        FluentActions.Invoking(() => ProviderReversalPlanner.Plan(
                root, 999, [new RootTraceRange(root, 0, 1_000, 0)], []))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => ProviderReversalPlanner.Plan(root, 1, [], []))
            .Should().Throw<UnrecoverableParityFractionException>();
    }

    [Fact]
    public void Planner_SubtractsPreviouslyReversedInteriorAndCoveringRanges()
    {
        var root = SourceStampId.New();
        var tail = Lot(root, new RootTraceRange(root, 0, 2_000, 0));
        var tailPlan = ProviderReversalPlanner.Plan(
            root, 2_000, [new RootTraceRange(root, 0, 1_000, 0)], [tail]);

        tailPlan.Fragments.Should().ContainSingle();
        tailPlan.Fragments[0].Ranges.Should().Equal(new RootTraceRange(root, 1_000, 1_000, 0));

        var prefix = SoftLot(root, new RootTraceRange(root, 0, 1_000, 0));
        var prefixPlan = ProviderReversalPlanner.Plan(
            root, 1_500, [new RootTraceRange(root, 500, 1_000, 0)], [prefix]);

        prefixPlan.Fragments.Should().ContainSingle();
        prefixPlan.Fragments[0].Ranges.Should().Equal(new RootTraceRange(root, 0, 500, 0));
    }

    [Fact]
    public void ReverseTopUp_RejectsUnknownDisposition()
    {
        var fixture = Setup(10);

        FluentActions.Invoking(() => fixture.Service.ReverseTopUp(
                Reverse(fixture.SourceId, 1, (ProviderReversalDisposition)99)))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    private static Fixture Setup(long hardUnits)
    {
        var store = new InMemoryLedgerKernelStore();
        var service = new TransactionalPostingService(store);
        var wallet = WalletId.New();
        var claim = service.ObserveTopUp(Observe(wallet, hardUnits));
        service.ConfirmObservedTopUp(Confirm(claim));
        return new Fixture(store, service, wallet, claim.SourceId);
    }

    private static ObserveHardCoinTopUpCommand Observe(WalletId wallet, long units) => new(
        SourceStampId.New(),
        wallet,
        new ProviderMonetaryLeg("stripe", "live", "acct_gameguild", $"pi_{Guid.NewGuid():N}", "capture"),
        "provider-observation",
        units,
        Time);

    private static ConfirmObservedTopUpCommand Confirm(HardCoinFundingClaim claim)
    {
        var key = new IdempotencyKey($"confirm-{claim.SourceId.Value:N}");
        var confirmedAt = Time.AddMinutes(1);
        return new ConfirmObservedTopUpCommand(
            PostingId.New(), key, claim.SourceId, CreditLotId.New(),
            new ReserveVersion(1), new PolicyVersion(1), "provider-confirmation", confirmedAt,
            FundingAuthorizationFixture.Create(
                PostingTemplateKind.ConfirmedTopUpMint, key, claim.WalletId, claim.Amount,
                [claim.SourceId], confirmedAt));
    }

    private static ConvertHardToSoftCommand Convert(Fixture fixture, long principal, long fee = 0)
    {
        var key = new IdempotencyKey($"convert-{Guid.NewGuid():N}");
        var total = new CoinAmount(CurrencyCode.HardCoin, principal + fee);
        var requestedAt = Time.AddMinutes(2);
        return new ConvertHardToSoftCommand(
            PostingId.New(), PostingId.New(), key, fixture.WalletId, CreditLotId.New(),
            principal, fee, new ReserveVersion(1), new PolicyVersion(1), requestedAt,
            FundingAuthorizationFixture.Create(
                PostingTemplateKind.HardToSoftConversion, key, fixture.WalletId, total,
                [fixture.SourceId], requestedAt,
                new CoinAmount(CurrencyCode.SoftCoin, principal * 1_000)));
    }

    private static ReverseTopUpCommand Reverse(
        SourceStampId sourceId,
        long cumulativeHardUnits,
        ProviderReversalDisposition disposition) => new(
        PostingId.New(),
        new IdempotencyKey($"reversal-{cumulativeHardUnits}-{Guid.NewGuid():N}"),
        sourceId,
        cumulativeHardUnits,
        disposition,
        "provider-reversal-evidence",
        new ReserveVersion(1),
        new PolicyVersion(1),
        Time.AddDays(1));

    private static CreditLot Lot(SourceStampId root, RootTraceRange range) => new(
        CreditLotId.New(),
        WalletId.New(),
        new CoinAmount(CurrencyCode.HardCoin, range.Length / CurrencyTraceScale.HardCoinTraceUnitsPerCoin),
        ProvenanceKind.PurchasedHard,
        Time,
        Time.AddDays(120),
        1,
        CreditLotState.Active,
        [range],
        CurrencyTraceScale.HardCoinTraceUnitsPerCoin);

    private static CreditLot SoftLot(SourceStampId root, RootTraceRange range) => new(
        CreditLotId.New(),
        WalletId.New(),
        new CoinAmount(CurrencyCode.SoftCoin, range.Length),
        ProvenanceKind.ConvertedSoft,
        Time,
        Time.AddDays(120),
        1,
        CreditLotState.Active,
        [range],
        CurrencyTraceScale.SoftCoinTraceUnitsPerCoin);

    private sealed record Fixture(
        InMemoryLedgerKernelStore Store,
        TransactionalPostingService Service,
        WalletId WalletId,
        SourceStampId SourceId);
}
