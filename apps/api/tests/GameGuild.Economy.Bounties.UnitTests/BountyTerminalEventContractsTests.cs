using FluentAssertions;
using GameGuild.Economy.Contracts;

namespace GameGuild.Economy.Bounties.UnitTests;

public sealed class BountyTerminalEventContractsTests
{
    [Fact]
    public void Claim_requires_risk_and_immutable_proceeds_identifiers()
    {
        var command = CreateCommand(BountyStatus.Claimed) with
        {
            RiskDecisionId = null,
            ProceedsSourceStampId = null,
            ProceedsLotId = null
        };

        FluentActions.Invoking(command.Validate)
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reclaim_rejects_claim_only_evidence_and_negative_units()
    {
        var command = CreateCommand(BountyStatus.Reclaimed) with { RiskDecisionId = Guid.NewGuid() };
        FluentActions.Invoking(command.Validate)
            .Should().Throw<ArgumentException>();

        command = CreateCommand(BountyStatus.Reclaimed) with { ReturnedUnits = -1 };
        FluentActions.Invoking(command.Validate)
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Terminal_output_lots_must_be_unique_and_keep_maturity_order()
    {
        var output = CreateOutputLot();
        var duplicate = CreateCommand(BountyStatus.Claimed) with { OutputLots = [output, output] };
        FluentActions.Invoking(duplicate.Validate)
            .Should().Throw<ArgumentException>();

        var invalidMaturity = output with { OriginalMaturesAt = output.ConfirmedAt.AddTicks(-1) };
        var invalid = CreateCommand(BountyStatus.Claimed) with { OutputLots = [invalidMaturity] };
        FluentActions.Invoking(invalid.Validate)
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Valid_claim_and_reclaim_terminal_evidence_passes_validation()
    {
        FluentActions.Invoking(CreateCommand(BountyStatus.Claimed).Validate)
            .Should().NotThrow();
        FluentActions.Invoking(CreateCommand(BountyStatus.Reclaimed).Validate)
            .Should().NotThrow();
    }

    private static CompleteBountyTerminalEventCommand CreateCommand(BountyStatus status)
    {
        var claim = status == BountyStatus.Claimed;
        return new CompleteBountyTerminalEventCommand(
            Guid.NewGuid(),
            new BountyId(Guid.NewGuid()),
            status,
            Guid.NewGuid(),
            new WalletId(Guid.NewGuid()),
            new IdempotencyKey($"bounty-terminal-{Guid.NewGuid():N}"),
            claim ? Guid.NewGuid() : null,
            claim ? new SourceStampId(Guid.NewGuid()) : null,
            claim ? new CreditLotId(Guid.NewGuid()) : null,
            claim ? 0 : 80,
            claim ? 0 : 20,
            1,
            claim ? [CreateOutputLot()] : [],
            DateTimeOffset.UtcNow);
    }

    private static BountyTerminalOutputLot CreateOutputLot()
    {
        var confirmedAt = DateTimeOffset.UtcNow;
        return new BountyTerminalOutputLot(
            new CreditLotId(Guid.NewGuid()),
            new WalletId(Guid.NewGuid()),
            new CoinAmount(CurrencyCode.HardCoin, 100),
            ProvenanceKind.EarnedHard,
            new SourceStampId(Guid.NewGuid()),
            confirmedAt,
            confirmedAt.AddDays(120),
            true);
    }
}
