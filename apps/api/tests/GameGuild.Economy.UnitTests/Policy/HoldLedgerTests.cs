using FluentAssertions;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;
using GameGuild.Economy.Policy;

namespace GameGuild.Economy.UnitTests.Policy;

public sealed class HoldLedgerTests
{
    private static readonly DateTimeOffset Time = new(2026, 7, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void HoldLifecycleIsAppendOnlyAndProducesCurrentContracts()
    {
        var wallet = WalletId.New();
        var (store, ledger) = LedgerWithBalance(wallet, CurrencyCode.HardCoin, 20);
        var hold = ledger.Place(
            HoldId.New(), wallet, new CoinAmount(CurrencyCode.HardCoin, 20), HoldReason.RiskReview, Time);

        ledger.ActiveFor(wallet).Should().ContainSingle().Which.Should().Be(hold);
        ledger.Release(hold.Id, Time.AddMinutes(1));

        ledger.ActiveFor(wallet).Should().BeEmpty();
        ledger.Events.Should().HaveCount(2);
        ledger.Events.Select(item => item.Kind).Should().Equal(HoldEventKind.Placed, HoldEventKind.Released);
        ledger.Current(hold.Id).Status.Should().Be(HoldStatus.Released);
        ledger.Events.Should().OnlyContain(item => item.Sequence > 0);
        store.Holds.Should().ContainSingle().Which.Status.Should().Be(HoldStatus.Released);
    }

    [Fact]
    public async Task ConcurrentTerminalTransitionsAllowExactlyOneWinner()
    {
        var wallet = WalletId.New();
        var (_, ledger) = LedgerWithBalance(wallet, CurrencyCode.HardCoin, 20);
        var hold = ledger.Place(
            HoldId.New(), wallet, new CoinAmount(CurrencyCode.HardCoin, 20), HoldReason.Dispute, Time);

        var outcomes = await Task.WhenAll(
            Task.Run(() => Try(() => ledger.Release(hold.Id, Time.AddMinutes(1)))),
            Task.Run(() => Try(() => ledger.Consume(hold.Id, Time.AddMinutes(1)))));

        outcomes.Should().ContainSingle(value => value);
        ledger.Events.Should().HaveCount(2);
        ledger.Current(hold.Id).Status.Should().BeOneOf(HoldStatus.Released, HoldStatus.Consumed);
    }

    [Fact]
    public void HoldLedgerRejectsUnknownRepeatedOrBackdatedTransitions()
    {
        var wallet = WalletId.New();
        var (_, ledger) = LedgerWithBalance(wallet, CurrencyCode.SoftCoin, 20);
        var hold = ledger.Place(
            HoldId.New(), wallet, new CoinAmount(CurrencyCode.SoftCoin, 20), HoldReason.Compliance, Time);

        FluentActions.Invoking(() => ledger.Place(
                HoldId.New(), wallet, new CoinAmount(CurrencyCode.SoftCoin, 1), HoldReason.Compliance, Time))
            .Should().Throw<InsufficientFragmentsException>();
        FluentActions.Invoking(() => ledger.Release(HoldId.New(), Time)).Should().Throw<KeyNotFoundException>();
        FluentActions.Invoking(() => ledger.Release(hold.Id, Time.AddTicks(-1))).Should().Throw<ArgumentException>();
        ledger.Release(hold.Id, Time.AddMinutes(1));
        FluentActions.Invoking(() => ledger.Release(hold.Id, Time.AddMinutes(2))).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => ledger.Place(
                hold.Id, WalletId.New(), new CoinAmount(CurrencyCode.SoftCoin, 1), HoldReason.Compliance, Time))
            .Should().Throw<InvalidOperationException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task HoldPlacementAndTransferSerializeAgainstTheSameWalletFragments()
    {
        var wallet = WalletId.New();
        var (store, ledger) = LedgerWithBalance(wallet, CurrencyCode.HardCoin, 10);
        var posting = new TransactionalPostingService(store);
        var transfer = new TransferFragmentsCommand(
            PostingId.New(), new IdempotencyKey("hold-transfer-race"), wallet, WalletId.New(),
            new CoinAmount(CurrencyCode.HardCoin, 10), ProvenanceKind.EarnedHard,
            new ReserveVersion(1), new PolicyVersion(1), Time.AddMinutes(1));

        var outcomes = await Task.WhenAll(
            Task.Run(() => Try(() => ledger.Place(
                HoldId.New(), wallet, new CoinAmount(CurrencyCode.HardCoin, 10), HoldReason.RiskReview, Time))),
            Task.Run(() => Try(() => posting.Transfer(transfer))));

        outcomes.Should().ContainSingle(value => value);
        (store.Holds.Count + store.FragmentConsumptions.Count).Should().Be(1);
    }

    [Fact]
    public void PartialHoldLeavesOnlyTheUnheldWalletAmountSpendable()
    {
        var wallet = WalletId.New();
        var (store, ledger) = LedgerWithBalance(wallet, CurrencyCode.HardCoin, 10);
        var posting = new TransactionalPostingService(store);
        ledger.Place(HoldId.New(), wallet, new CoinAmount(CurrencyCode.HardCoin, 6), HoldReason.Compliance, Time);

        FluentActions.Invoking(() => posting.Transfer(new TransferFragmentsCommand(
                PostingId.New(), new IdempotencyKey("held-five"), wallet, WalletId.New(),
                new CoinAmount(CurrencyCode.HardCoin, 5), ProvenanceKind.EarnedHard,
                new ReserveVersion(1), new PolicyVersion(1), Time.AddMinutes(1))))
            .Should().Throw<InsufficientFragmentsException>();
        posting.Transfer(new TransferFragmentsCommand(
            PostingId.New(), new IdempotencyKey("held-four"), wallet, WalletId.New(),
            new CoinAmount(CurrencyCode.HardCoin, 4), ProvenanceKind.EarnedHard,
            new ReserveVersion(1), new PolicyVersion(1), Time.AddMinutes(1)));

        store.GetAvailableLots(wallet, CurrencyCode.HardCoin).Single().Amount.Units.Should().Be(6);
    }

    private static bool Try(Action operation)
    {
        try
        {
            operation();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static (InMemoryLedgerKernelStore Store, AppendOnlyHoldLedger Ledger) LedgerWithBalance(
        WalletId wallet,
        CurrencyCode currency,
        long units)
    {
        var store = new InMemoryLedgerKernelStore();
        var scale = CurrencyTraceScale.For(currency);
        store.Execute(transaction =>
        {
            transaction.AddCreditLot(new CreditLot(
                CreditLotId.New(),
                wallet,
                new CoinAmount(currency, units),
                currency == CurrencyCode.HardCoin ? ProvenanceKind.EarnedHard : ProvenanceKind.AdRewardSoft,
                Time.AddDays(-120),
                Time,
                1,
                CreditLotState.Active,
                [new RootTraceRange(SourceStampId.New(), 0, units * scale, 0)],
                scale));
            return true;
        });
        return (store, new AppendOnlyHoldLedger(store));
    }
}
