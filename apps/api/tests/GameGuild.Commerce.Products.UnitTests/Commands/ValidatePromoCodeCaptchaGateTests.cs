using FluentAssertions;
using GameGuild.Commerce.Products;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Tests.Commerce.Products.Unit.Commands;

/// <summary>
/// Issue #418 anti-farming: the validation endpoint is CAPTCHA-gated for untrusted
/// (anonymous) contexts. A denied gate fails closed before any code lookup, records the
/// denial as a CaptchaRequired attempt (observable for anomaly alerting, without
/// resolving any code), and feeds the velocity evaluator. Trusted callers and verifiable
/// tokens proceed to normal validation.
/// </summary>
public class ValidatePromoCodeCaptchaGateTests
{
    private readonly Mock<IPricingEngineService> _mockPricingEngine = new();
    private readonly Mock<IPromoCodeRepository> _mockPromoRepository = new();
    private readonly Mock<IPromoUsageAnomalyEvaluator> _mockAnomalyEvaluator = new();

    private static PromoRedemptionCaptchaGate CreateGate(bool enabled, string? secret = null)
        => new(Options.Create(new PromoRedemptionCaptchaOptions
        {
            Enabled = enabled,
            VerificationSecret = secret
        }));

    private ValidatePromoCodeCommandHandler CreateHandler(
        PromoRedemptionCaptchaGate? gate = null,
        PromoAbuseDetectionOptions? abuseOptions = null)
        => new(
            _mockPricingEngine.Object,
            _mockPromoRepository.Object,
            NullLogger<ValidatePromoCodeCommandHandler>.Instance,
            _mockAnomalyEvaluator.Object,
            abuseOptions is null ? null : Options.Create(abuseOptions),
            gate);

    private static ValidatePromoCodeCommand AnonymousCommand(string code = "FARM-ME-2026") =>
        new(Code: code, OrderAmount: 100m, ProductId: null, UserId: null, PlanId: null, CaptchaToken: null);

    [Fact]
    public async Task Handle_GateEnabled_AnonymousWithoutToken_FailsClosedBeforeCodeLookup()
    {
        var handler = CreateHandler(CreateGate(enabled: true, secret: "s3cret"));
        var command = AnonymousCommand();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Outcome.Should().Be(PromoCodeValidationOutcome.CaptchaRequired);
        result.ErrorMessage.Should().Contain("CAPTCHA");
        result.PromoCodeId.Should().BeNull();

        // Fail closed before any pricing work: the farmed code string never reaches the engine.
        _mockPricingEngine.Verify(
            p => p.ValidatePromoCodeAsync(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_GateEnabled_Denial_RecordsCaptchaRequiredAttemptWithoutResolvingTheCode()
    {
        var handler = CreateHandler(CreateGate(enabled: true, secret: "s3cret"));
        var command = AnonymousCommand("WinBack2026");

        await handler.Handle(command, CancellationToken.None);

        // The denial is observable: recorded with the CaptchaRequired outcome and no
        // promo code id, so campaign conversion denominators are never inflated.
        // (Normalization to uppercase happens inside the repository.)
        _mockPromoRepository.Verify(
            r => r.RecordValidationAttemptAsync(
                "WinBack2026",
                null,
                null,
                PromoCodeValidationOutcome.CaptchaRequired,
                It.Is<string>(message => message.Contains("CAPTCHA")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_GateEnabled_Denial_FeedsAnomalyVelocity()
    {
        var handler = CreateHandler(
            CreateGate(enabled: true, secret: "s3cret"),
            new PromoAbuseDetectionOptions { ValidationWindowMinutes = 10 });
        var command = AnonymousCommand();

        // 29 prior probes in the window + the denied one that triggered the check.
        _mockPromoRepository
            .Setup(r => r.CountRecentValidationAttemptsAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(29);

        await handler.Handle(command, CancellationToken.None);

        _mockAnomalyEvaluator.Verify(
            e => e.EvaluateValidationVelocityAsync(
                "FARM-ME-2026",
                null,
                30,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_GateEnabled_AnonymousWithValidToken_ProceedsToValidation()
    {
        var gate = CreateGate(enabled: true, secret: "s3cret");
        var handler = CreateHandler(gate);
        var token = PromoRedemptionCaptchaGate.IssueToken("checkout-challenge", "s3cret");
        var command = AnonymousCommand() with { CaptchaToken = token };

        _mockPricingEngine
            .Setup(p => p.ValidatePromoCodeAsync(
                command.Code,
                command.OrderAmount,
                command.ProductId,
                command.UserId,
                command.PlanId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromoCodeValidationResult(
                IsValid: true,
                Code: command.Code,
                Outcome: PromoCodeValidationOutcome.Valid));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.Outcome.Should().Be(PromoCodeValidationOutcome.Valid);
    }

    [Fact]
    public async Task Handle_GateEnabled_TrustedContext_ProceedsWithoutToken()
    {
        var handler = CreateHandler(CreateGate(enabled: true, secret: "s3cret"));
        var userId = Guid.NewGuid();
        var command = AnonymousCommand() with { UserId = userId };

        _mockPricingEngine
            .Setup(p => p.ValidatePromoCodeAsync(
                command.Code,
                command.OrderAmount,
                command.ProductId,
                userId,
                command.PlanId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromoCodeValidationResult(
                IsValid: true,
                Code: command.Code,
                PromoCodeId: Guid.NewGuid(),
                Outcome: PromoCodeValidationOutcome.Valid));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        _mockPricingEngine.Verify(
            p => p.ValidatePromoCodeAsync(
                command.Code,
                command.OrderAmount,
                command.ProductId,
                userId,
                command.PlanId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_GateDisabled_AnonymousValidationKeepsWorking()
    {
        var handler = CreateHandler(CreateGate(enabled: false));
        var command = AnonymousCommand();

        _mockPricingEngine
            .Setup(p => p.ValidatePromoCodeAsync(
                command.Code,
                command.OrderAmount,
                command.ProductId,
                command.UserId,
                command.PlanId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromoCodeValidationResult(
                IsValid: false,
                Code: command.Code,
                ErrorMessage: "Promo code not found",
                Outcome: PromoCodeValidationOutcome.NotFound));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Outcome.Should().Be(PromoCodeValidationOutcome.NotFound);
    }
}
