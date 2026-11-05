using FsCheck;
using FsCheck.Xunit;
using GameGuild.Commerce.Payments;
using GameGuild.Finance.Economy.Contracts;
using GameGuild.Finance.Economy.Funding;
using GameGuild.Finance.Economy.Integrations;
using GameGuild.Finance.Economy.Ledger;
using GameGuild.Finance.Economy.Money;

namespace GameGuild.Finance.Economy.UnitTests.Integrations.Payments;

/// <summary>
///     Property-based symmetry invariants of the HardCoin top-up mint and its provider
///     reversal (issue #1202 gaps N4, fixed via #1266): HardCoin units equal authoritative
///     USD minor units (1 HardCoin = 1 cent, <see cref="FixedParity"/>); for every top-up
///     refunded fully or partially, the reversal amount equals the minted amount scaled by
///     the refunded fraction; cumulative refunds can never reverse more than was minted;
///     sub-cent refund totals fail closed; and the reversal idempotency keys collide
///     exactly on the same (payment, cumulative level) pair — so webhook redeliveries of
///     the same cumulative level deduplicate while step-ups are distinct protected
///     operations.
/// </summary>
public sealed class ProviderReversalSymmetryPropertyTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly StripeEconomyFundingAdapter Adapter = new();

    private const long MaxCents = 100_000_000L; // $1M in cents — comfortably inside long/decimal range

    private static decimal UsdFromCents(long cents) => cents / 100m;

    private static EconomyTopUpPaymentFact FactOf(decimal amountUsd, string objectId = "pi_property") => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        amountUsd,
        "USD",
        "stripe",
        "test",
        "acct_platform",
        objectId,
        "payment_intent",
        "capture");

    private static ReverseTopUpCommand Reverse(
        EconomyTopUpPaymentFact fact,
        decimal cumulativeRefunded,
        decimal cumulativeDisputed = 0m) =>
        Adapter.CreateReversal(
            fact,
            cumulativeRefunded,
            cumulativeDisputed,
            ProviderReversalDisposition.ResponsibleDebt,
            "property-evidence",
            new ReserveVersion(1),
            new PolicyVersion(1),
            Now);

    /// <summary>True when the action throws exactly the expected exception type (or a derived one).</summary>
    private static bool Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
            return false;
        }
        catch (TException)
        {
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // ---------- Mint parity: HardCoin units == authoritative USD minor units ----------

    /// <summary>
    ///     For every whole-cent USD top-up, the mint observation authorizes exactly
    ///     cents × HardCoin units (1 HardCoin per USD cent —
    ///     <see cref="FixedParity.HardCoinsPerUsd"/> units per whole dollar), and does so
    ///     deterministically (the same payment fact replays to the same command).
    /// </summary>
    [Property(MaxTest = 300)]
    public Property Mint_UnitsEqualUsdMinorUnits_AndIsDeterministic(int cents)
    {
        var boundedCents = Math.Clamp((long)cents, 1, MaxCents);
        var fact = FactOf(UsdFromCents(boundedCents));
        var walletId = new WalletId(Guid.NewGuid());

        var first = Adapter.CreateObservation(fact, walletId, "stripe-event", Now);
        var replay = Adapter.CreateObservation(fact, walletId, "stripe-event", Now);

        return first.AuthoritativeUsdMinorUnits.Equals(boundedCents)
            .ToProperty()
            .Label($"mint for {UsdFromCents(boundedCents)} USD authorized {first.AuthoritativeUsdMinorUnits} units, expected {boundedCents} (USD minor units)")
            .And(first.Equals(replay)
                .ToProperty()
                .Label("the same payment minted different observations across replays"));
    }

    /// <summary>
    ///     Sub-cent mint amounts fail closed: an amount that is not exactly representable
    ///     in whole HardCoin units (the ToHardCoinUnits/ToUsdMinorUnits whole-unit
    ///     requirement) must never produce a mint observation.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Mint_FailsClosedOnSubCentAmounts(int cents, byte subCentMillis)
    {
        var whole = Math.Abs(cents % 1_000_000);
        var amount = whole / 100m + (subCentMillis % 9 + 1) / 1000m; // strictly sub-cent tail (0.001..0.009)

        var fact = FactOf(amount);

        return Throws<ArgumentException>(() =>
                Adapter.CreateObservation(fact, new WalletId(Guid.NewGuid()), "stripe-event", Now))
            .ToProperty()
            .Label($"the sub-cent amount {amount} produced a mint instead of failing closed");
    }

    // ---------- Reversal = mint × refunded fraction (exact HardCoin unit arithmetic) ----------

    /// <summary>
    ///     For every whole-cent payment A and every positive whole-cent refund R ≤ A, the
    ///     reversal carries exactly R HardCoin units — the minted units scaled by the
    ///     refunded fraction R/A (reversal × A == mint × R holds exactly in integer
    ///     arithmetic) — and a full refund reverses exactly the minted total.
    /// </summary>
    [Property(MaxTest = 500)]
    public Property Reversal_EqualsTheMintScaledByTheRefundedFraction(int paymentCents, int refundCents)
    {
        var payment = Math.Clamp((long)paymentCents, 1, MaxCents);
        var refund = Math.Clamp((long)refundCents, 1, payment); // zero refunds never reach the adapter (consumer guard)
        var fact = FactOf(UsdFromCents(payment));

        var command = Reverse(fact, UsdFromCents(refund));

        var mintedUnits = payment; // HardCoin units == USD minor units == cents
        var reversalUnits = command.CumulativeProviderHardUnits;

        return (reversalUnits * payment).Equals(mintedUnits * refund)
            .ToProperty()
            .Label($"reversal {reversalUnits} of mint {mintedUnits} is not the refunded fraction {refund}/{payment} (HardCoin unit arithmetic)")
            .And((reversalUnits <= mintedUnits)
                .ToProperty()
                .Label($"reversal {reversalUnits} exceeds the minted {mintedUnits}"));
    }

    /// <summary>
    ///     Partial refund step-ups never reverse more than minted: for every increasing
    ///     cumulative refund sequence ending at the full payment, every level's reversal
    ///     stays within the mint, the final level equals the mint exactly, and consecutive
    ///     levels differ — each step-up is its own reversal amount.
    /// </summary>
    [Property(MaxTest = 300)]
    public Property PartialRefundSequences_NeverReverseMoreThanMinted(int paymentCents, int stepCount, int stepSize)
    {
        var payment = Math.Clamp((long)paymentCents, 2, 1_000_000);
        var steps = 1 + (Math.Abs(stepCount) % 10);
        var size = Math.Max(1, payment / steps + (Math.Abs(stepSize) % Math.Max(1, payment / steps + 1)));

        var fact = FactOf(UsdFromCents(payment));
        var mintedUnits = payment; // HardCoin units == USD minor units == cents

        var cumulative = 0L;
        var levels = new List<long>();
        while (cumulative < payment)
        {
            cumulative = Math.Min(payment, cumulative + size);
            var command = Reverse(fact, UsdFromCents(cumulative));
            if (command.CumulativeProviderHardUnits > mintedUnits)
            {
                return false.ToProperty()
                    .Label($"cumulative refund {cumulative}/{payment} cents reversed {command.CumulativeProviderHardUnits} > minted {mintedUnits}");
            }

            levels.Add(command.CumulativeProviderHardUnits);
        }

        return (levels.Count > 0 && levels[^1] == mintedUnits)
            .ToProperty()
            .Label($"the final full-refund level {levels.LastOrDefault()} does not equal the minted {mintedUnits}")
            .And(levels.SequenceEqual(levels.Distinct())
                .ToProperty()
                .Label($"cumulative levels {string.Join(",", levels)} are not strictly increasing"));
    }

    /// <summary>
    ///     The cumulative bound holds for combined refund and dispute totals: a total above
    ///     the payment amount fails closed, while exactly the payment amount is accepted
    ///     (a fully refunded-or-disputed top-up is the legal maximum).
    /// </summary>
    [Property(MaxTest = 300)]
    public Property CumulativeRefunds_OverThePaymentAmount_FailClosed(int paymentCents, int refundedCents, int disputedCents)
    {
        var payment = Math.Clamp((long)paymentCents, 1, 1_000_000);
        var refunded = Math.Clamp((long)refundedCents, 0, payment);
        var disputed = Math.Clamp((long)disputedCents, 0, payment);
        if (refunded + disputed == 0)
        {
            refunded = 1; // a zero total never reaches the adapter (the consumer guards it)
        }

        var fact = FactOf(UsdFromCents(payment));

        if (refunded + disputed > payment)
        {
            return Throws<InvalidOperationException>(() =>
                    Reverse(fact, UsdFromCents(refunded), UsdFromCents(disputed)))
                .ToProperty()
                .Label($"refunded {refunded} + disputed {disputed} > payment {payment} was accepted");
        }

        var command = Reverse(fact, UsdFromCents(refunded), UsdFromCents(disputed));

        return (command.CumulativeProviderHardUnits == refunded + disputed)
            .ToProperty()
            .Label($"combined reversal {command.CumulativeProviderHardUnits} is not (refunded + disputed) = {refunded + disputed} HardCoin units");
    }

    /// <summary>
    ///     Sub-cent refund totals fail closed (the ToHardCoinUnits whole-HardCoin
    ///     requirement): any refund that is not exactly representable in whole HardCoin
    ///     units must throw instead of rounding silently.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Reversal_FailsClosedOnSubCentRefunds(int cents, byte subCentMillis)
    {
        var payment = Math.Clamp((long)cents, 1, MaxCents);
        var whole = Math.Max(0L, payment - 1);
        var refund = whole / 100m + (subCentMillis % 9 + 1) / 1000m; // sub-cent tail, always <= payment amount

        var fact = FactOf(UsdFromCents(payment));

        return Throws<ArgumentException>(() => Reverse(fact, refund))
            .ToProperty()
            .Label($"the sub-cent refund {refund} was converted instead of failing closed");
    }

    // ---------- Idempotency keys collide exactly on the same cumulative level ----------

    /// <summary>
    ///     For any payment and any two cumulative HardCoin levels: the reversal idempotency
    ///     keys are equal iff the levels are equal on the same payment; different payments
    ///     never share a key; and
    ///     <see cref="IStripeEconomyFundingAdapter.CreateReversal(EconomyTopUpPaymentFact, decimal, decimal, ProviderReversalDisposition, string, ReserveVersion, PolicyVersion, DateTimeOffset)"/>
    ///     stamps exactly the key that
    ///     <see cref="IStripeEconomyFundingAdapter.ReversalIdempotencyKey"/> derives for
    ///     the same cumulative level — the protected-operation intent and the persisted
    ///     posting deduplicate the same reversal.
    /// </summary>
    [Property(MaxTest = 400)]
    public Property ReversalKeys_CollideExactlyOnTheSameCumulativeLevel(
        int levelA,
        int levelB,
        bool samePayment)
    {
        var first = Math.Clamp((long)levelA, 1, 1_000_000); // cumulative HardCoin level in cents
        var second = Math.Clamp((long)levelB, 1, 1_000_000);
        var paymentCents = Math.Max(first, second); // both levels stay within this payment

        var fact = FactOf(UsdFromCents(paymentCents), "pi_property");
        var otherFact = FactOf(UsdFromCents(paymentCents), "pi_other");

        var keyFirst = Adapter.ReversalIdempotencyKey(fact, first);
        var keySecond = Adapter.ReversalIdempotencyKey(samePayment ? fact : otherFact, second);

        var keysCollide = keyFirst.Equals(keySecond);
        var shouldCollide = samePayment && first.Equals(second);

        return keysCollide.Equals(shouldCollide)
            .ToProperty()
            .Label($"keys for levels {first}/{second} (same payment: {samePayment}) violate collide-iff-same-cumulative-level")
            .And(Adapter.ReversalIdempotencyKey(fact, first).Equals(keyFirst)
                .ToProperty()
                .Label("the reversal key derivation is not deterministic"))
            .And(Reverse(fact, UsdFromCents(first)).IdempotencyKey.Equals(keyFirst)
                .ToProperty()
                .Label("the persisted reversal stamps a different key than the protected-operation intent"));
    }
}
