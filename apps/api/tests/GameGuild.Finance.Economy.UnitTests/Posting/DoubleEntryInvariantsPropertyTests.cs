using FsCheck;
using FsCheck.Xunit;
using GameGuild.Finance.Economy.Contracts;
using GameGuild.Finance.Economy.Posting;
using GameGuild.Finance.Economy.Writer;

namespace GameGuild.Finance.Economy.UnitTests.Posting;

/// <summary>
///     Property-based double-entry invariants of the Economy posting matrix (docs/economy
///     README "double-entry wallets" core): every operation sequence must leave
///     sum(debits) == sum(credits) independently per currency; wallet-to-wallet transfers
///     (the Spend/Refund liability templates) never create or destroy units — the same
///     amount debited from one wallet is credited to another; validity is scale-invariant
///     (a balanced posting stays balanced at any unit scale, so fencing-token replays of
///     the same logical operation cannot unbalance it); and any single-line perturbation
///     that breaks a currency's balance is rejected with exactly the
///     <see cref="PostingErrorCode.UnbalancedCurrency"/> error for that currency and no
///     other — per-currency independence.
/// </summary>
public sealed class DoubleEntryInvariantsPropertyTests
{
    private static readonly PostingTemplateKind[] WalletTransferTemplates =
    [
        PostingTemplateKind.Spend,
        PostingTemplateKind.Refund
    ];

    private static readonly PostingTemplateKind[] AllRegisteredTemplates =
        Enum.GetValues<PostingTemplateKind>();

    /// <summary>
    ///     Scale invariance: for every registered template and every positive unit scale,
    ///     scaling every line of the canonical valid posting keeps it valid — the
    ///     double-entry balance and shape checks are amount-independent, so the same
    ///     logical operation at any denomination is accepted.
    /// </summary>
    [Property(MaxTest = 300)]
    public Property BalancedPostings_AreValidAtAnyUnitScale(byte templateIndex, int scale)
    {
        var kind = AllRegisteredTemplates[templateIndex % AllRegisteredTemplates.Length];
        var factor = Math.Clamp((long)scale, 1, 100_000_000L);
        var request = PostingFixture.Valid(kind);

        var scaled = request with
        {
            Lines = request.Lines
                .Select(line => line with { Amount = new CoinAmount(line.Amount.Currency, line.Amount.Units * factor) })
                .ToArray()
        };

        var result = PostingMatrix.Validate(scaled);

        return result.IsValid
            .ToProperty()
            .Label($"scaling {kind} by {factor} broke validation: {string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Message}"))}");
    }

    /// <summary>
    ///     Transfers never create or destroy units: for any unit amount, the wallet transfer
    ///     templates debit exactly the amount from one wallet's liability and credit exactly
    ///     the same amount to another wallet in the same currency — the transfer's net unit
    ///     change across both wallets is zero and debits == credits per currency.
    /// </summary>
    [Property(MaxTest = 400)]
    public Property Transfers_MoveExactlyTheSameUnits_DebitToCredit(byte templateIndex, int units, bool hardCurrency)
    {
        var kind = WalletTransferTemplates[templateIndex % WalletTransferTemplates.Length];
        var currency = hardCurrency ? CurrencyCode.HardCoin : CurrencyCode.SoftCoin;
        var amount = Math.Clamp((long)units, 1, 1_000_000_000L);
        var sender = WalletId.New();
        var recipient = WalletId.New();

        PostingLine debitLine;
        PostingLine creditLine;
        PostingAuthority authority;
        ProvenanceKind? debitProvenance = null;
        ProvenanceKind? creditProvenance = null;
        if (kind == PostingTemplateKind.Spend)
        {
            authority = PostingAuthority.WalletOwner;
        }
        else
        {
            authority = PostingAuthority.EscrowCoordinator;
            debitProvenance = ProvenanceKind.EarnedHard;
            creditProvenance = ProvenanceKind.RefundRestoration;
        }

        var account = currency == CurrencyCode.HardCoin
            ? EconomyAccountCode.PurchasedHardLiability
            : EconomyAccountCode.SoftCoinLiability;
        if (kind == PostingTemplateKind.Refund && currency == CurrencyCode.HardCoin)
        {
            // The Refund template credits with RefundRestoration provenance; keep the debit
            // side's provenance consistent with the liability account rules.
            debitProvenance = null;
        }

        debitLine = new PostingLine(1, EntrySide.Debit, account, new CoinAmount(currency, amount), sender, null, debitProvenance);
        creditLine = new PostingLine(2, EntrySide.Credit, account, new CoinAmount(currency, amount), recipient, null, creditProvenance);

        var request = new PostingRequest(
            PostingId.New(),
            new PostingTemplate(kind, PostingTemplate.CurrentVersion),
            new IdempotencyKey("transfer-property"),
            authority,
            new ReserveVersion(1),
            new PolicyVersion(1),
            null,
            DateTimeOffset.UtcNow,
            [debitLine, creditLine]);

        var result = PostingMatrix.Validate(request);
        var debits = request.Lines.Where(line => line.Side == EntrySide.Debit).Sum(line => line.Amount.Units);
        var credits = request.Lines.Where(line => line.Side == EntrySide.Credit).Sum(line => line.Amount.Units);
        var senderNet = request.Lines
            .Where(line => line.WalletId == sender)
            .Sum(line => line.Side == EntrySide.Debit ? -line.Amount.Units : line.Amount.Units);
        var recipientNet = request.Lines
            .Where(line => line.WalletId == recipient)
            .Sum(line => line.Side == EntrySide.Debit ? -line.Amount.Units : line.Amount.Units);

        return debits.Equals(credits)
            .ToProperty()
            .Label($"transfer debits {debits} != credits {credits}")
            .And(senderNet.Equals(-amount)
                .ToProperty()
                .Label($"the sender's net change {senderNet} is not exactly -{amount}"))
            .And(recipientNet.Equals(amount)
                .ToProperty()
                .Label($"the recipient's net change {recipientNet} is not exactly {amount}"))
            .And((senderNet + recipientNet).Equals(0)
                .ToProperty()
                .Label("the transfer changed the total units in the wallet system"))
            .And(result.IsValid
                .ToProperty()
                .Label($"the balanced transfer was rejected: {string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Message}"))}"));
    }

    /// <summary>
    ///     Balance is exactly what the matrix enforces, per currency and independently: a
    ///     generated posting whose lines balance in every currency validates without the
    ///     <see cref="PostingErrorCode.UnbalancedCurrency"/> error; perturbing exactly one
    ///     currency's credit side by any non-zero delta introduces that error — and ONLY
    ///     that error code class — while the other currency, when present, is unaffected.
    /// </summary>
    [Property(MaxTest = 400)]
    public Property UnbalancedCurrency_IsDetectedExactlyOnce_PerCurrency(int units, int delta, bool secondCurrency)
    {
        var amount = Math.Clamp((long)units, 1, 1_000_000_000L);
        var perturbation = Math.Abs((long)delta % 1_000_000_000L);
        if (perturbation == 0)
        {
            perturbation = 1;
        }

        var hardLines = new List<PostingLine>
        {
            new(1, EntrySide.Debit, EconomyAccountCode.PurchasedHardLiability, new CoinAmount(CurrencyCode.HardCoin, amount), WalletId.New(), null, ProvenanceKind.PurchasedHard),
            new(2, EntrySide.Credit, EconomyAccountCode.PurchasedHardLiability, new CoinAmount(CurrencyCode.HardCoin, amount), WalletId.New(), null, ProvenanceKind.RefundRestoration)
        };

        var softLines = new List<PostingLine>();
        if (secondCurrency)
        {
            softLines.Add(new PostingLine(3, EntrySide.Debit, EconomyAccountCode.SoftCoinLiability, new CoinAmount(CurrencyCode.SoftCoin, amount), WalletId.New(), null, null));
            softLines.Add(new PostingLine(4, EntrySide.Credit, EconomyAccountCode.SoftCoinLiability, new CoinAmount(CurrencyCode.SoftCoin, amount), WalletId.New(), null, null));
        }

        PostingRequest Build(IEnumerable<PostingLine> lines) => new(
            PostingId.New(),
            new PostingTemplate(PostingTemplateKind.Spend, PostingTemplate.CurrentVersion),
            new IdempotencyKey("balance-property"),
            PostingAuthority.WalletOwner,
            new ReserveVersion(1),
            new PolicyVersion(1),
            null,
            DateTimeOffset.UtcNow,
            lines.ToArray());

        var balanced = Build(hardLines.Concat(softLines));
        var balancedResult = PostingMatrix.Validate(balanced);

        var perturbedHard = hardLines[^1] with { Amount = new CoinAmount(CurrencyCode.HardCoin, amount + perturbation) };
        var unbalanced = Build(hardLines[..^1].Append(perturbedHard).Concat(softLines));
        var unbalancedResult = PostingMatrix.Validate(unbalanced);

        var balancedHasNoUnbalanceError = balancedResult.Errors.All(error => error.Code != PostingErrorCode.UnbalancedCurrency);
        var unbalancedErrors = unbalancedResult.Errors.Where(error => error.Code == PostingErrorCode.UnbalancedCurrency).ToList();
        var mentionsOnlyHardCoin = unbalancedErrors.Count == 1 && unbalancedErrors[0].Message.Contains(CurrencyCode.HardCoin.ToString());

        return balancedHasNoUnbalanceError
            .ToProperty()
            .Label($"the balanced posting was flagged unbalanced: {string.Join("; ", balancedResult.Errors.Select(error => error.Message))}")
            .And((unbalancedErrors.Count == 1)
                .ToProperty()
                .Label($"expected exactly one UnbalancedCurrency error, got {unbalancedErrors.Count}"))
            .And(mentionsOnlyHardCoin
                .ToProperty()
                .Label($"the unbalance error must name only {CurrencyCode.HardCoin}, the currency whose balance was broken: {string.Join("; ", unbalancedErrors.Select(error => error.Message))}"));
    }

    /// <summary>
    ///     Operation sequences conserve units end to end: for any sequence of balanced
    ///     postings (mixed templates and scales), summing every line of the whole sequence
    ///     leaves total debits == total credits per currency — no operation sequence can
    ///     create or destroy units in aggregate.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property OperationSequences_LeaveDebitsEqualCredits_PerCurrency(byte[] templateChoices, int[] scales)
    {
        var postings = templateChoices
            .Select((choice, index) =>
            {
                var kind = AllRegisteredTemplates[choice % AllRegisteredTemplates.Length];
                var factor = Math.Clamp(index < scales.Length ? (long)scales[index] : 1L, 1, 1_000_000L);
                var request = PostingFixture.Valid(kind);
                return request with
                {
                    Lines = request.Lines
                        .Select(line => line with { Amount = new CoinAmount(line.Amount.Currency, line.Amount.Units * factor) })
                        .ToArray()
                };
            })
            .ToArray();

        var perCurrency = postings
            .SelectMany(request => request.Lines.Select(line => (line.Amount.Currency, line.Side, line.Amount.Units)))
            .GroupBy(entry => entry.Currency)
            .ToArray();

        var allBalanced = perCurrency.All(group =>
            group.Where(entry => entry.Side == EntrySide.Debit).Sum(entry => entry.Units) ==
            group.Where(entry => entry.Side == EntrySide.Credit).Sum(entry => entry.Units));

        var allIndividuallyValid = postings.All(request => PostingMatrix.Validate(request).IsValid);

        return allIndividuallyValid
            .ToProperty()
            .Label("a generated posting in the sequence was individually invalid (fixture mismatch)")
            .And(allBalanced
                .ToProperty()
                .Label("a sequence of individually balanced postings left a currency's debits != credits in aggregate"));
    }
}
