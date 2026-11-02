using FluentAssertions;
using GameGuild.Commerce.Products;
using GameGuild.Compliance.Audit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Tests.Commerce.Products.Unit.Services;

/// <summary>
/// Issue #418 unusual-usage alerting: velocity anomalies publish a
/// PromoUsageAnomalyDetected security event through the central pipeline (where
/// SecurityAlertRuleEvaluator raises the alert); below-threshold counts publish nothing.
/// </summary>
public class PromoUsageAnomalyEvaluatorTests
{
    private readonly Mock<ISecurityEventLogger> _securityEventLogger = new();

    private PromoUsageAnomalyEvaluator CreateEvaluator(
        int validationThreshold = 30,
        int redemptionThreshold = 10)
        => new(
            _securityEventLogger.Object,
            Options.Create(new PromoAbuseDetectionOptions
            {
                ValidationBurstThreshold = validationThreshold,
                ValidationWindowMinutes = 10,
                RedemptionBurstThreshold = redemptionThreshold,
                RedemptionWindowMinutes = 10
            }),
            Mock.Of<ILogger<PromoUsageAnomalyEvaluator>>());

    [Fact]
    public async Task ValidationVelocity_BelowThreshold_PublishesNothing()
    {
        var evaluator = CreateEvaluator(validationThreshold: 5);

        await evaluator.EvaluateValidationVelocityAsync("SUMMER", userId: Guid.NewGuid(), recentAttemptCount: 4);

        _securityEventLogger.Verify(
            logger => logger.RecordAsync(It.IsAny<CreateAuditLogRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidationVelocity_AtThreshold_PublishesSecurityEvent()
    {
        var evaluator = CreateEvaluator(validationThreshold: 5);
        var userId = Guid.NewGuid();
        CreateAuditLogRequest? captured = null;
        _securityEventLogger
            .Setup(logger => logger.RecordAsync(It.IsAny<CreateAuditLogRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateAuditLogRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new SecurityEventCaptureResult(
                SecurityEventCaptureOutcome.PersistedToDatabase,
                SecurityEventTaxonomy.Classify("PromoUsageAnomalyDetected", AuditCategory.Security, false, AuditRiskLevel.High),
                Guid.NewGuid()));

        await evaluator.EvaluateValidationVelocityAsync("SUMMER", userId, recentAttemptCount: 5);

        captured.Should().NotBeNull();
        captured!.ActionType.Should().Be(AuditActionTypes.PromoUsageAnomalyDetected);
        captured.Category.Should().Be(AuditCategory.Security);
        captured.RiskLevel.Should().Be(AuditRiskLevel.High);
        captured.Success.Should().BeFalse();
        captured.UserId.Should().Be(userId);
        captured.ErrorMessage.Should().Contain("5 promo code validation attempts");
    }

    [Fact]
    public async Task RedemptionVelocity_AtThreshold_PublishesSecurityEvent()
    {
        var evaluator = CreateEvaluator(redemptionThreshold: 3);
        CreateAuditLogRequest? captured = null;
        _securityEventLogger
            .Setup(logger => logger.RecordAsync(It.IsAny<CreateAuditLogRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateAuditLogRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new SecurityEventCaptureResult(
                SecurityEventCaptureOutcome.PersistedToDatabase,
                SecurityEventTaxonomy.Classify("PromoUsageAnomalyDetected", AuditCategory.Security, false, AuditRiskLevel.High),
                Guid.NewGuid()));

        var userId = Guid.NewGuid();
        await evaluator.EvaluateRedemptionVelocityAsync(userId, "SUMMER", recentUsageCount: 3);

        captured.Should().NotBeNull();
        captured!.ActionType.Should().Be(AuditActionTypes.PromoUsageAnomalyDetected);
        captured.ErrorMessage.Should().Contain("3 promo code redemptions");
        captured.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task RedemptionVelocity_BelowThreshold_PublishesNothing()
    {
        var evaluator = CreateEvaluator(redemptionThreshold: 3);

        await evaluator.EvaluateRedemptionVelocityAsync(Guid.NewGuid(), "SUMMER", recentUsageCount: 2);

        _securityEventLogger.Verify(
            logger => logger.RecordAsync(It.IsAny<CreateAuditLogRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PipelineFailure_IsSwallowed_NeverPropagates()
    {
        var evaluator = CreateEvaluator(validationThreshold: 2);
        _securityEventLogger
            .Setup(logger => logger.RecordAsync(It.IsAny<CreateAuditLogRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("pipeline down"));

        var act = async () => await evaluator.EvaluateValidationVelocityAsync(
            "SUMMER", Guid.NewGuid(), recentAttemptCount: 5);

        await act.Should().NotThrowAsync();
    }
}
