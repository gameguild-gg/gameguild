using FluentAssertions;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Funding;
using GameGuild.Economy.Ledger;

namespace GameGuild.Economy.UnitTests.Funding;

public sealed class HardToSoftConversionServiceTests
{
    private static readonly DateTimeOffset Time = DateTimeOffset.Parse("2026-07-18T12:00:00Z");

    [Fact]
    public void ConvertHardToSoft_RetiresHardAndCreatesExactConvertedSoftLineage()
    {
        var (store, service, wallet) = Setup(10);

        var conversion = service.ConvertHardToSoft(Convert(wallet, 4));

        conversion.PrincipalPosting.Status.Should().Be(PostingStatus.Accepted);
        conversion.FeePosting.Should().BeNull();
        conversion.OutputLot.Amount.Should().Be(new CoinAmount(CurrencyCode.SoftCoin, 4_000));
        conversion.OutputLot.Provenance.Should().Be(ProvenanceKind.ConvertedSoft);
        conversion.OutputLot.TraceUnitsPerCoinUnit.Should().Be(1);
        store.GetAvailableLots(wallet, CurrencyCode.HardCoin)
            .Should().ContainSingle().Which.Amount.Units.Should().Be(6);
        store.GetAvailableLots(wallet, CurrencyCode.SoftCoin)
            .Should().ContainSingle().Which.Amount.Units.Should().Be(4_000);
        store.FragmentConsumptions.Should().ContainSingle().Which.Amount.Units.Should().Be(4);
        store.Lineages.Should().ContainSingle().Which.Lot.Should().BeSameAs(conversion.OutputLot);
        store.ProjectionUpdates.TakeLast(2).Select(update => update.DeltaUnits).Should().Equal(-4, 4_000);
    }

    [Fact]
    public void ConvertHardToSoft_PostsConfiguredHardFeeSeparately()
    {
        var (store, service, wallet) = Setup(10);

        var conversion = service.ConvertHardToSoft(Convert(wallet, 4, 1));

        conversion.FeePosting.Should().NotBeNull();
        store.JournalEntries.Should().HaveCount(3);
        store.JournalEntries[^1].Lines.Select(line => line.Account).Should().Equal(
            EconomyAccountCode.PurchasedHardLiability,
            EconomyAccountCode.FeeRevenueHard);
        store.FragmentConsumptions.Select(item => item.Amount.Units).Should().Equal(4, 1);
        store.GetAvailableLots(wallet, CurrencyCode.HardCoin)
            .Should().ContainSingle().Which.Amount.Units.Should().Be(5);
        store.ProjectionUpdates.TakeLast(3).Select(update => update.DeltaUnits).Should().Equal(-4, 4_000, -1);
    }

    [Fact]
    public void ConvertHardToSoft_IsIdempotentAcrossPrincipalAndFee()
    {
        var (store, service, wallet) = Setup(10);
        var command = Convert(wallet, 4, 1);

        var first = service.ConvertHardToSoft(command);
        var duplicate = service.ConvertHardToSoft(command);

        duplicate.Should().BeEquivalentTo(first);
        store.JournalEntries.Should().HaveCount(3);
        store.FragmentConsumptions.Should().HaveCount(2);
        store.Lineages.Should().ContainSingle();
    }

    [Fact]
    public void ConvertHardToSoft_RollsBackWhenPrincipalAndFeeExceedAvailableHardCoin()
    {
        var (store, service, wallet) = Setup(4);
        var before = store.SnapshotCounts();

        FluentActions.Invoking(() => service.ConvertHardToSoft(Convert(wallet, 4, 1)))
            .Should().Throw<InsufficientFragmentsException>();

        store.SnapshotCounts().Should().Be(before);
        store.GetAvailableLots(wallet, CurrencyCode.HardCoin)
            .Should().ContainSingle().Which.Amount.Units.Should().Be(4);
        store.GetAvailableLots(wallet, CurrencyCode.SoftCoin).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConvertHardToSoft_RejectsNonPositivePrincipal(long units)
    {
        var (_, service, wallet) = Setup(4);

        FluentActions.Invoking(() => service.ConvertHardToSoft(Convert(wallet, units)))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void EconomyContract_ExposesNoSoftToHardCommand()
    {
        typeof(ConvertHardToSoftCommand).Assembly.GetExportedTypes()
            .Where(type => type.Name.Contains("SoftToHard", StringComparison.OrdinalIgnoreCase))
            .Should().BeEmpty();
    }

    private static (InMemoryLedgerKernelStore Store, TransactionalPostingService Service, WalletId Wallet) Setup(long units)
    {
        var store = new InMemoryLedgerKernelStore();
        var service = new TransactionalPostingService(store);
        var wallet = WalletId.New();
        var observed = service.ObserveTopUp(new ObserveHardCoinTopUpCommand(
            SourceStampId.New(),
            wallet,
            new ProviderMonetaryLeg("stripe", "live", "acct_gameguild", $"pi_{Guid.NewGuid():N}", "capture"),
            "provider-observation",
            units,
            Time));
        service.ConfirmObservedTopUp(new ConfirmObservedTopUpCommand(
            PostingId.New(),
            new IdempotencyKey($"fund-{Guid.NewGuid():N}"),
            observed.SourceId,
            CreditLotId.New(),
            new ReserveVersion(1),
            new PolicyVersion(1),
            "provider-confirmation",
            Time.AddMinutes(1)));
        return (store, service, wallet);
    }

    private static ConvertHardToSoftCommand Convert(WalletId wallet, long principal, long fee = 0) => new(
        PostingId.New(),
        PostingId.New(),
        new IdempotencyKey($"convert-{Guid.NewGuid():N}"),
        wallet,
        CreditLotId.New(),
        principal,
        fee,
        new ReserveVersion(1),
        new PolicyVersion(1),
        Time.AddMinutes(2));
}
