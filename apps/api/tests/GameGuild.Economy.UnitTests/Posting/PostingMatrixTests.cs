using FluentAssertions;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Posting;

namespace GameGuild.Economy.UnitTests.Posting;

public sealed class PostingMatrixTests
{
    public static TheoryData<PostingTemplateKind> SupportedTemplates => new()
    {
        PostingTemplateKind.ConfirmedTopUpMint,
        PostingTemplateKind.ProviderReversalFull,
        PostingTemplateKind.ProviderReversalPartial,
        PostingTemplateKind.Spend,
        PostingTemplateKind.HardToSoftConversion,
        PostingTemplateKind.HardToSoftConversionFee,
        PostingTemplateKind.SystemBackedGrant,
        PostingTemplateKind.Burn,
        PostingTemplateKind.Escrow,
        PostingTemplateKind.Reclaim,
        PostingTemplateKind.Refund,
        PostingTemplateKind.PayoutReservation,
        PostingTemplateKind.PayoutSuccess,
        PostingTemplateKind.PayoutFailure,
        PostingTemplateKind.AdminWithdrawalReservation,
        PostingTemplateKind.AdminWithdrawalSuccess,
        PostingTemplateKind.AdminWithdrawalFailure
    };

    [Fact]
    public void MatrixCoversEveryRegisteredTemplateAndHasNoPreConfirmationMint()
    {
        var registered = Enum.GetValues<PostingTemplateKind>();

        registered.Should().HaveCount(17);
        registered.Select(kind => kind.ToString()).Should().NotContain(name =>
            name.Contains("Observed", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("FailedMint", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [MemberData(nameof(SupportedTemplates))]
    public void SupportedTemplate_AcceptsItsExactPostingShape(PostingTemplateKind kind)
    {
        var result = PostingMatrix.Validate(PostingFixture.Valid(kind));

        result.IsValid.Should().BeTrue(string.Join(Environment.NewLine, result.Errors));
        result.Errors.Should().BeEmpty();
        FluentActions.Invoking(() => PostingMatrix.EnsureValid(PostingFixture.Valid(kind))).Should().NotThrow();
    }

    [Fact]
    public void NullOrUnregisteredRequests_FailClosed()
    {
        FluentActions.Invoking(() => PostingMatrix.Validate(null!)).Should().Throw<ArgumentNullException>();
        var request = PostingFixture.Valid(PostingTemplateKind.Spend) with { Template = default };

        PostingMatrix.Validate(request).Errors.Should().ContainSingle(error => error.Code == PostingErrorCode.UnsupportedTemplate);
    }

    [Fact]
    public void UnknownTemplateVersion_IsRejected()
    {
        var request = PostingFixture.Valid(PostingTemplateKind.Spend) with
        {
            Template = new PostingTemplate(PostingTemplateKind.Spend, 2)
        };

        PostingMatrix.Validate(request).Errors.Should().Contain(error => error.Code == PostingErrorCode.UnsupportedTemplateVersion);
        FluentActions.Invoking(() => PostingMatrix.EnsureValid(request)).Should().Throw<PostingValidationException>();
    }

    [Fact]
    public void WrongAuthority_IsRejectedEvenWhenPostingBalances()
    {
        var request = PostingFixture.Valid(PostingTemplateKind.ConfirmedTopUpMint) with
        {
            Authority = PostingAuthority.WalletOwner
        };

        PostingMatrix.Validate(request).Errors.Should().Contain(error => error.Code == PostingErrorCode.UnauthorizedAuthority);
    }

    [Fact]
    public void BalancedButWrongAccountShape_IsRejected()
    {
        var request = PostingFixture.Valid(PostingTemplateKind.ConfirmedTopUpMint);
        request = request with
        {
            Lines =
            [
                request.Lines[0] with { Account = EconomyAccountCode.PlatformHardTreasury },
                request.Lines[1]
            ]
        };

        PostingMatrix.Validate(request).Errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidAccountShape);
    }

    [Fact]
    public void UnbalancedPosting_IsRejectedPerCurrency()
    {
        var request = PostingFixture.Valid(PostingTemplateKind.Spend);
        request = request with { Lines = [request.Lines[0], request.Lines[1] with { Amount = new CoinAmount(CurrencyCode.HardCoin, 9) }] };

        PostingMatrix.Validate(request).Errors.Should().Contain(error => error.Code == PostingErrorCode.UnbalancedCurrency);
    }

    [Fact]
    public void DuplicateOrNonContiguousSequences_AreRejected()
    {
        var request = PostingFixture.Valid(PostingTemplateKind.Spend);
        var duplicate = request with { Lines = [request.Lines[0], request.Lines[1] with { Sequence = 1 }] };
        var gap = request with { Lines = [request.Lines[0], request.Lines[1] with { Sequence = 3 }] };

        PostingMatrix.Validate(duplicate).Errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidSequence);
        PostingMatrix.Validate(gap).Errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidSequence);
    }

    [Fact]
    public void Mint_RequiresConfirmedProviderEvidence()
    {
        var request = PostingFixture.Valid(PostingTemplateKind.ConfirmedTopUpMint) with
        {
            Source = PostingFixture.Source(SourceConfirmationState.Observed)
        };

        PostingMatrix.Validate(request).Errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidSourceState);

        PostingMatrix.Validate(request with { Source = null }).Errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidSourceState);
    }

    [Fact]
    public void ProviderReversal_RequiresReversedProviderEvidence()
    {
        var request = PostingFixture.Valid(PostingTemplateKind.ProviderReversalPartial) with
        {
            Source = PostingFixture.Source(SourceConfirmationState.Confirmed)
        };

        PostingMatrix.Validate(request).Errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidSourceState);
    }

    [Fact]
    public void Conversion_RequiresExactPrincipalRatioAndSeparateFeePosting()
    {
        var request = PostingFixture.Valid(PostingTemplateKind.HardToSoftConversion);
        var wrongRatio = request with
        {
            Lines =
            [
                request.Lines[0], request.Lines[1], request.Lines[2] with { Amount = new CoinAmount(CurrencyCode.SoftCoin, 9_999) },
                request.Lines[3] with { Amount = new CoinAmount(CurrencyCode.SoftCoin, 9_999) }
            ]
        };
        var embeddedFee = request with
        {
            Lines = [.. request.Lines, PostingFixture.Line(5, EntrySide.Credit, EconomyAccountCode.FeeRevenueHard, CurrencyCode.HardCoin, 1)]
        };

        PostingMatrix.Validate(wrongRatio).Errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidParity);
        PostingMatrix.Validate(embeddedFee).Errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidLineCount);
    }

    [Fact]
    public void SoftSpend_UsesSoftLiabilityAndRemainsBalanced()
    {
        var request = PostingFixture.Valid(PostingTemplateKind.Spend);
        request = request with
        {
            Lines =
            [
                PostingFixture.Line(1, EntrySide.Debit, EconomyAccountCode.SoftCoinLiability, CurrencyCode.SoftCoin, 15, WalletId.New(), ProvenanceKind.AdRewardSoft),
                PostingFixture.Line(2, EntrySide.Credit, EconomyAccountCode.SoftCoinLiability, CurrencyCode.SoftCoin, 15, WalletId.New(), ProvenanceKind.AdRewardSoft)
            ]
        };

        PostingMatrix.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ZeroLineAndMissingWalletShapes_AreRejected()
    {
        FluentActions.Invoking(() => PostingFixture.Line(1, EntrySide.Debit, EconomyAccountCode.PurchasedHardLiability, CurrencyCode.HardCoin, 0))
            .Should().Throw<ArgumentOutOfRangeException>();

        var request = PostingFixture.Valid(PostingTemplateKind.Spend);
        request = request with { Lines = [request.Lines[0] with { WalletId = null }, request.Lines[1]] };
        PostingMatrix.Validate(request).Errors.Should().Contain(error => error.Code == PostingErrorCode.MissingWallet);
    }

    [Fact]
    public void DeserializedZeroAmountAndOverflowingTotals_AreRejected()
    {
        var request = PostingFixture.Valid(PostingTemplateKind.Spend);
        var zero = request with { Lines = [request.Lines[0] with { Amount = default }, request.Lines[1]] };
        var overflow = request with
        {
            Lines =
            [
                request.Lines[0] with { Amount = new CoinAmount(CurrencyCode.HardCoin, long.MaxValue) },
                request.Lines[0] with { Sequence = 2, Amount = new CoinAmount(CurrencyCode.HardCoin, 1) },
                request.Lines[1] with { Sequence = 3, Amount = new CoinAmount(CurrencyCode.HardCoin, long.MaxValue) }
            ]
        };

        PostingMatrix.Validate(zero).Errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidAmount);
        PostingMatrix.Validate(overflow).Errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidAmount);
    }

    [Fact]
    public void MalformedLiabilityTransfer_IsRejected()
    {
        var request = PostingFixture.Valid(PostingTemplateKind.Spend);
        var malformed = request with
        {
            Lines =
            [
                request.Lines[0] with { Side = EntrySide.Credit, Account = EconomyAccountCode.HardCoinReserve },
                request.Lines[1] with { Account = EconomyAccountCode.SoftCoinLiability, Amount = new CoinAmount(CurrencyCode.SoftCoin, 10) }
            ]
        };

        var errors = PostingMatrix.Validate(malformed).Errors;
        errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidAccountShape);
        errors.Should().Contain(error => error.Code == PostingErrorCode.UnbalancedCurrency);

        var refund = PostingFixture.Valid(PostingTemplateKind.Refund);
        refund = refund with { Lines = [refund.Lines[0], refund.Lines[1] with { Provenance = ProvenanceKind.PurchasedHard }] };
        PostingMatrix.Validate(refund).Errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidProvenance);
    }

    [Fact]
    public void SoftBurnEscrowAndReclaim_UseSoftSystemAccounts()
    {
        var wallet = WalletId.New();
        var softBurn = PostingFixture.Valid(PostingTemplateKind.Burn) with
        {
            Lines =
            [
                PostingFixture.Line(1, EntrySide.Debit, EconomyAccountCode.SoftCoinLiability, CurrencyCode.SoftCoin, 20, wallet, ProvenanceKind.AdRewardSoft),
                PostingFixture.Line(2, EntrySide.Credit, EconomyAccountCode.SoftCoinReserve, CurrencyCode.SoftCoin, 20)
            ]
        };
        var softEscrow = PostingFixture.Valid(PostingTemplateKind.Escrow) with
        {
            Lines =
            [
                PostingFixture.Line(1, EntrySide.Debit, EconomyAccountCode.SoftCoinLiability, CurrencyCode.SoftCoin, 20, wallet, ProvenanceKind.AdRewardSoft),
                PostingFixture.Line(2, EntrySide.Credit, EconomyAccountCode.SoftCoinEscrow, CurrencyCode.SoftCoin, 20)
            ]
        };
        var softReclaim = PostingFixture.Valid(PostingTemplateKind.Reclaim) with
        {
            Lines =
            [
                PostingFixture.Line(1, EntrySide.Debit, EconomyAccountCode.SoftCoinEscrow, CurrencyCode.SoftCoin, 20),
                PostingFixture.Line(2, EntrySide.Credit, EconomyAccountCode.SoftCoinLiability, CurrencyCode.SoftCoin, 20, wallet, ProvenanceKind.EscrowReturn)
            ]
        };

        PostingMatrix.Validate(softBurn).IsValid.Should().BeTrue();
        PostingMatrix.Validate(softEscrow).IsValid.Should().BeTrue();
        PostingMatrix.Validate(softReclaim).IsValid.Should().BeTrue();
    }

    [Fact]
    public void InvalidCurrencyFromDeserialization_IsReportedWithoutEscapingValidator()
    {
        var burn = PostingFixture.Valid(PostingTemplateKind.Burn);
        burn = burn with { Lines = [burn.Lines[0] with { Amount = default }, burn.Lines[1]] };
        var reclaim = PostingFixture.Valid(PostingTemplateKind.Reclaim);
        reclaim = reclaim with { Lines = [reclaim.Lines[0], reclaim.Lines[1] with { Amount = default }] };

        PostingMatrix.Validate(burn).Errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidCurrency);
        PostingMatrix.Validate(reclaim).Errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidCurrency);
    }

    [Fact]
    public void ExactShapeMatcher_ReportsEveryMaterialMismatch()
    {
        var request = PostingFixture.Valid(PostingTemplateKind.ConfirmedTopUpMint);
        request = request with
        {
            Lines =
            [
                request.Lines[0] with
                {
                    Side = EntrySide.Credit,
                    Account = EconomyAccountCode.PlatformHardTreasury,
                    Amount = new CoinAmount(CurrencyCode.SoftCoin, 10),
                    WalletId = WalletId.New(),
                    Provenance = ProvenanceKind.AdRewardSoft
                },
                request.Lines[1] with { WalletId = null, Provenance = ProvenanceKind.EarnedHard }
            ]
        };

        var errors = PostingMatrix.Validate(request).Errors;
        errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidAccountShape);
        errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidCurrency);
        errors.Should().Contain(error => error.Code == PostingErrorCode.MissingWallet);
        errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidProvenance);
    }

    [Fact]
    public void ConversionParityOverflow_IsRejected()
    {
        var request = PostingFixture.Valid(PostingTemplateKind.HardToSoftConversion);
        request = request with
        {
            Lines =
            [
                request.Lines[0] with { Amount = new CoinAmount(CurrencyCode.HardCoin, long.MaxValue) },
                request.Lines[1] with { Amount = new CoinAmount(CurrencyCode.HardCoin, long.MaxValue) },
                request.Lines[2] with { Amount = new CoinAmount(CurrencyCode.SoftCoin, long.MaxValue) },
                request.Lines[3] with { Amount = new CoinAmount(CurrencyCode.SoftCoin, long.MaxValue) }
            ]
        };

        PostingMatrix.Validate(request).Errors.Should().Contain(error => error.Code == PostingErrorCode.InvalidParity);
    }
}

internal static class PostingFixture
{
    private static readonly DateTimeOffset Time = DateTimeOffset.Parse("2026-07-18T12:00:00Z");

    internal static PostingRequest Valid(PostingTemplateKind kind)
    {
        var (authority, lines, source) = kind switch
        {
            PostingTemplateKind.ConfirmedTopUpMint => (PostingAuthority.ProviderConfirmation,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.ExternalClearingHard, CurrencyCode.HardCoin, 10),
                    Line(2, EntrySide.Credit, EconomyAccountCode.PurchasedHardLiability, CurrencyCode.HardCoin, 10, WalletId.New(), ProvenanceKind.PurchasedHard)
                }, Source(SourceConfirmationState.Confirmed)),
            PostingTemplateKind.ProviderReversalFull or PostingTemplateKind.ProviderReversalPartial => (PostingAuthority.ProviderConfirmation,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.PurchasedHardLiability, CurrencyCode.HardCoin, 10, WalletId.New(), ProvenanceKind.PurchasedHard),
                    Line(2, EntrySide.Credit, EconomyAccountCode.ExternalClearingHard, CurrencyCode.HardCoin, 10)
                }, Source(SourceConfirmationState.Reversed)),
            PostingTemplateKind.Spend => (PostingAuthority.WalletOwner,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.PurchasedHardLiability, CurrencyCode.HardCoin, 10, WalletId.New(), ProvenanceKind.PurchasedHard),
                    Line(2, EntrySide.Credit, EconomyAccountCode.PurchasedHardLiability, CurrencyCode.HardCoin, 10, WalletId.New(), ProvenanceKind.PurchasedHard)
                }, null),
            PostingTemplateKind.HardToSoftConversion => (PostingAuthority.WalletOwner,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.PurchasedHardLiability, CurrencyCode.HardCoin, 10, WalletId.New(), ProvenanceKind.PurchasedHard),
                    Line(2, EntrySide.Credit, EconomyAccountCode.HardCoinReserve, CurrencyCode.HardCoin, 10),
                    Line(3, EntrySide.Debit, EconomyAccountCode.SoftCoinReserve, CurrencyCode.SoftCoin, 10_000),
                    Line(4, EntrySide.Credit, EconomyAccountCode.SoftCoinLiability, CurrencyCode.SoftCoin, 10_000, WalletId.New(), ProvenanceKind.ConvertedSoft)
                }, null),
            PostingTemplateKind.HardToSoftConversionFee => (PostingAuthority.WalletOwner,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.PurchasedHardLiability, CurrencyCode.HardCoin, 10, WalletId.New(), ProvenanceKind.PurchasedHard),
                    Line(2, EntrySide.Credit, EconomyAccountCode.FeeRevenueHard, CurrencyCode.HardCoin, 10)
                }, null),
            PostingTemplateKind.SystemBackedGrant => (PostingAuthority.PlatformSystem,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.PlatformHardTreasury, CurrencyCode.HardCoin, 10),
                    Line(2, EntrySide.Credit, EconomyAccountCode.HardCoinReserve, CurrencyCode.HardCoin, 10),
                    Line(3, EntrySide.Debit, EconomyAccountCode.SoftCoinReserve, CurrencyCode.SoftCoin, 10_000),
                    Line(4, EntrySide.Credit, EconomyAccountCode.SoftCoinLiability, CurrencyCode.SoftCoin, 10_000, WalletId.New(), ProvenanceKind.SystemGrantSoft)
                }, null),
            PostingTemplateKind.Burn => (PostingAuthority.WalletOwner,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.PurchasedHardLiability, CurrencyCode.HardCoin, 10, WalletId.New(), ProvenanceKind.PurchasedHard),
                    Line(2, EntrySide.Credit, EconomyAccountCode.HardCoinReserve, CurrencyCode.HardCoin, 10)
                }, null),
            PostingTemplateKind.Escrow => (PostingAuthority.WalletOwner,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.PurchasedHardLiability, CurrencyCode.HardCoin, 10, WalletId.New(), ProvenanceKind.PurchasedHard),
                    Line(2, EntrySide.Credit, EconomyAccountCode.HardCoinEscrow, CurrencyCode.HardCoin, 10)
                }, null),
            PostingTemplateKind.Reclaim => (PostingAuthority.EscrowCoordinator,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.HardCoinEscrow, CurrencyCode.HardCoin, 10),
                    Line(2, EntrySide.Credit, EconomyAccountCode.PurchasedHardLiability, CurrencyCode.HardCoin, 10, WalletId.New(), ProvenanceKind.EscrowReturn)
                }, null),
            PostingTemplateKind.Refund => (PostingAuthority.EscrowCoordinator,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.PurchasedHardLiability, CurrencyCode.HardCoin, 10, WalletId.New(), ProvenanceKind.PurchasedHard),
                    Line(2, EntrySide.Credit, EconomyAccountCode.PurchasedHardLiability, CurrencyCode.HardCoin, 10, WalletId.New(), ProvenanceKind.RefundRestoration)
                }, null),
            PostingTemplateKind.PayoutReservation => (PostingAuthority.PayoutCoordinator,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.EarnedHardLiability, CurrencyCode.HardCoin, 10, WalletId.New(), ProvenanceKind.EarnedHard),
                    Line(2, EntrySide.Credit, EconomyAccountCode.PayoutPayableHard, CurrencyCode.HardCoin, 10)
                }, null),
            PostingTemplateKind.PayoutSuccess => (PostingAuthority.PayoutCoordinator,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.PayoutPayableHard, CurrencyCode.HardCoin, 10),
                    Line(2, EntrySide.Credit, EconomyAccountCode.ExternalClearingHard, CurrencyCode.HardCoin, 10)
                }, null),
            PostingTemplateKind.PayoutFailure => (PostingAuthority.PayoutCoordinator,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.PayoutPayableHard, CurrencyCode.HardCoin, 10),
                    Line(2, EntrySide.Credit, EconomyAccountCode.EarnedHardLiability, CurrencyCode.HardCoin, 10, WalletId.New(), ProvenanceKind.EarnedHard)
                }, null),
            PostingTemplateKind.AdminWithdrawalReservation => (PostingAuthority.Administrator,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.PlatformHardTreasury, CurrencyCode.HardCoin, 10),
                    Line(2, EntrySide.Credit, EconomyAccountCode.AdminWithdrawalPayableHard, CurrencyCode.HardCoin, 10)
                }, null),
            PostingTemplateKind.AdminWithdrawalSuccess => (PostingAuthority.Administrator,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.AdminWithdrawalPayableHard, CurrencyCode.HardCoin, 10),
                    Line(2, EntrySide.Credit, EconomyAccountCode.ExternalClearingHard, CurrencyCode.HardCoin, 10)
                }, null),
            PostingTemplateKind.AdminWithdrawalFailure => (PostingAuthority.Administrator,
                new[]
                {
                    Line(1, EntrySide.Debit, EconomyAccountCode.AdminWithdrawalPayableHard, CurrencyCode.HardCoin, 10),
                    Line(2, EntrySide.Credit, EconomyAccountCode.PlatformHardTreasury, CurrencyCode.HardCoin, 10)
                }, null),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        return new PostingRequest(
            PostingId.New(),
            new PostingTemplate(kind, PostingTemplate.CurrentVersion),
            new IdempotencyKey($"test-{kind}"),
            authority,
            new ReserveVersion(7),
            new PolicyVersion(4),
            source,
            Time,
            lines);
    }

    internal static PostingLine Line(
        int sequence,
        EntrySide side,
        EconomyAccountCode account,
        CurrencyCode currency,
        long units,
        WalletId? walletId = null,
        ProvenanceKind? provenance = null) =>
        new(sequence, side, account, new CoinAmount(currency, units), walletId, null, provenance);

    internal static SourceStampContract Source(SourceConfirmationState state)
    {
        DateTimeOffset? confirmedAt = state == SourceConfirmationState.Confirmed ? Time.AddMinutes(1) : null;
        return new SourceStampContract(SourceStampId.New(), "sha256-source", state, Time, confirmedAt, "pi_test");
    }
}
