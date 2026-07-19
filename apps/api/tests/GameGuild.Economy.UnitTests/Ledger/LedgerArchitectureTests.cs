using FluentAssertions;
using GameGuild.Economy.Ledger;

namespace GameGuild.Economy.UnitTests.Ledger;

public sealed class LedgerArchitectureTests
{
    [Fact]
    public void AppendOnlyLedgerEntities_HaveNoSettersAndDoNotInheritEntityBase()
    {
        Type[] entityTypes =
        [
            typeof(SourceEvidence),
            typeof(CreditLot),
            typeof(JournalEntry),
            typeof(JournalEntryLine),
            typeof(FragmentConsumption),
            typeof(ParentFragmentLineage),
            typeof(DerivedCreditLot),
            typeof(FragmentRetirement),
            typeof(WalletProjectionUpdate),
            typeof(IdempotencyRecord),
            typeof(ImmutableOutboxMessage),
            typeof(ChainAnchor)
        ];

        entityTypes.SelectMany(type => type.GetProperties())
            .Should().OnlyContain(property => property.SetMethod == null);
        entityTypes.Select(type => type.BaseType?.Name)
            .Should().NotContain("EntityBase");
    }

    [Fact]
    public void CorePostingService_ExposesTypedCommandsOnly()
    {
        var methods = typeof(TransactionalPostingService).GetMethods()
            .Where(method => method.DeclaringType == typeof(TransactionalPostingService))
            .ToArray();

        methods.Select(method => method.Name).Should().BeEquivalentTo(
            nameof(TransactionalPostingService.ObserveTopUp),
            nameof(TransactionalPostingService.ConfirmObservedTopUp),
            nameof(TransactionalPostingService.FinalizeObservedTopUp),
            nameof(TransactionalPostingService.ConvertHardToSoft),
            nameof(TransactionalPostingService.IssueSystemBackedGrant),
            nameof(TransactionalPostingService.IssueAdReward),
            nameof(TransactionalPostingService.ReverseTopUp),
            nameof(TransactionalPostingService.Transfer));
        methods.SelectMany(method => method.GetParameters()).Select(parameter => parameter.ParameterType)
            .Should().OnlyContain(type => type.Name.EndsWith("Command", StringComparison.Ordinal));
    }

    [Fact]
    public void AppendOnlyEntityCollections_AreDefensiveCopies()
    {
        var root = new RootTraceRange(global::GameGuild.Economy.Contracts.SourceStampId.New(), 0, 1, 0);
        var ranges = new List<RootTraceRange> { root };
        var lot = new CreditLot(
            global::GameGuild.Economy.Contracts.CreditLotId.New(), global::GameGuild.Economy.Contracts.WalletId.New(),
            new global::GameGuild.Economy.Contracts.CoinAmount(global::GameGuild.Economy.Contracts.CurrencyCode.HardCoin, 1),
            global::GameGuild.Economy.Contracts.ProvenanceKind.EarnedHard,
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, 1,
            CreditLotState.Active, ranges);
        var consumption = new FragmentConsumption(
            global::GameGuild.Economy.Contracts.PostingId.New(), lot.Id, lot.Amount, ranges);
        var parent = new ParentFragmentLineage(lot.Id, lot.Amount, ranges);
        var parents = new List<ParentFragmentLineage> { parent };
        var derived = new DerivedCreditLot(lot, parents);
        var retirement = new FragmentRetirement(global::GameGuild.Economy.Contracts.PostingId.New(), lot.Amount, parents);

        ranges.Clear();
        parents.Clear();

        lot.Ranges.Should().ContainSingle();
        consumption.Ranges.Should().ContainSingle();
        parent.Ranges.Should().ContainSingle();
        derived.Parents.Should().ContainSingle();
        retirement.Parents.Should().ContainSingle();
        lot.Ranges.Should().NotBeAssignableTo<RootTraceRange[]>();
        derived.Parents.Should().NotBeAssignableTo<ParentFragmentLineage[]>();
    }
}
