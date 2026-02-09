using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Monitoring.SLA.Tests;

/// <summary>
/// Unit tests for ErrorBudgetCalculator — core SLO math.
/// </summary>
public class ErrorBudgetCalculatorTests
{
    private readonly Mock<IServiceLevelIndicatorRepository> _sliRepoMock;
    private readonly Mock<IServiceLevelObjectiveRepository> _sloRepoMock;
    private readonly ErrorBudgetCalculator _sut;

    public ErrorBudgetCalculatorTests()
    {
        _sliRepoMock = new Mock<IServiceLevelIndicatorRepository>();
        _sloRepoMock = new Mock<IServiceLevelObjectiveRepository>();
        _sut = new ErrorBudgetCalculator(_sliRepoMock.Object, _sloRepoMock.Object);
    }

    private ServiceLevelObjective CreateSlo(Guid? id = null, double target = 99.9, double errorBudget = 0.1, int windowDays = 30) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Test SLO",
            ServiceName = "test-api",
            TargetPercentage = target,
            ErrorBudgetPercentage = errorBudget,
            TimeWindowDays = windowDays,
            IsEnabled = true
        };

    private List<ServiceLevelIndicator> CreateIndicators(Guid sloId, int successful, int failed, DateTimeOffset baseTime)
    {
        var indicators = new List<ServiceLevelIndicator>();
        for (var i = 0; i < successful; i++)
            indicators.Add(new ServiceLevelIndicator
            {
                ServiceLevelObjectiveId = sloId,
                Timestamp = baseTime.AddMinutes(i),
                IsSuccessful = true
            });
        for (var i = 0; i < failed; i++)
            indicators.Add(new ServiceLevelIndicator
            {
                ServiceLevelObjectiveId = sloId,
                Timestamp = baseTime.AddMinutes(successful + i),
                IsSuccessful = false
            });
        return indicators;
    }

    [Fact]
    public async Task CalculateForPeriodAsync_WhenSloNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _sloRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CalculateForPeriodAsync(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task CalculateForPeriodAsync_WhenNoRequests_ShouldReturn100PercentActual()
    {
        // Arrange
        var slo = CreateSlo();
        _sloRepoMock.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _sliRepoMock.Setup(r => r.GetBySloIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceLevelIndicator>());

        // Act
        var result = await _sut.CalculateForPeriodAsync(slo.Id, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);

        // Assert
        result.ActualPercentage.Should().Be(100.0);
        result.TotalRequests.Should().Be(0);
        result.FailedRequests.Should().Be(0);
        // When no requests, allowedFailures=0 so remainingBudgetPercentage=0, making IsHealthy false
        result.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task CalculateForPeriodAsync_ShouldCalculateCorrectPercentages()
    {
        // Arrange — 990 success, 10 failures = 99.0%
        var slo = CreateSlo(target: 99.9, errorBudget: 0.1);
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var end = DateTimeOffset.UtcNow;
        var indicators = CreateIndicators(slo.Id, 990, 10, start);

        _sloRepoMock.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _sliRepoMock.Setup(r => r.GetBySloIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(indicators);

        // Act
        var result = await _sut.CalculateForPeriodAsync(slo.Id, start, end);

        // Assert
        result.TotalRequests.Should().Be(1000);
        result.SuccessfulRequests.Should().Be(990);
        result.FailedRequests.Should().Be(10);
        result.ActualPercentage.Should().Be(99.0); // 990/1000 * 100
        result.AllowedFailures.Should().Be(1); // floor(1000 * 0.1/100) = 1
        result.IsHealthy.Should().BeFalse(); // 99.0 < 99.9 target
    }

    [Fact]
    public async Task CalculateForPeriodAsync_WhenHealthy_ShouldReturnIsHealthyTrue()
    {
        // Arrange — 999 success, 0 failures = 99.9% meets target with remaining budget
        var slo = CreateSlo(target: 99.0, errorBudget: 1.0);
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var end = DateTimeOffset.UtcNow;
        var indicators = CreateIndicators(slo.Id, 999, 1, start);

        _sloRepoMock.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _sliRepoMock.Setup(r => r.GetBySloIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(indicators);

        // Act
        var result = await _sut.CalculateForPeriodAsync(slo.Id, start, end);

        // Assert
        result.ActualPercentage.Should().Be(99.9);
        result.IsHealthy.Should().BeTrue(); // 99.9 >= 99.0 and allowedFailures=10, used=1, remaining=9
    }

    [Fact]
    public async Task CalculateForPeriodAsync_ShouldCalculateBurnRateAsFailuresPerDay()
    {
        // Arrange — 10 failures over 2 days = 5 failures/day
        var slo = CreateSlo(target: 99.0, errorBudget: 1.0);
        var start = DateTimeOffset.UtcNow.AddDays(-2);
        var end = DateTimeOffset.UtcNow;
        var indicators = CreateIndicators(slo.Id, 990, 10, start);

        _sloRepoMock.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _sliRepoMock.Setup(r => r.GetBySloIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(indicators);

        // Act
        var result = await _sut.CalculateForPeriodAsync(slo.Id, start, end);

        // Assert
        result.BurnRate.Should().BeApproximately(5.0, 0.01); // 10 / ~2 days
    }

    [Fact]
    public async Task CalculateForPeriodAsync_WhenBudgetExhausted_ShouldReturnNullTimeToExhaustion()
    {
        // Arrange — 0 remaining budget, no burn rate meaningful
        var slo = CreateSlo(target: 99.9, errorBudget: 0.1);
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var end = DateTimeOffset.UtcNow;
        var indicators = CreateIndicators(slo.Id, 900, 100, start);

        _sloRepoMock.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _sliRepoMock.Setup(r => r.GetBySloIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(indicators);

        // Act
        var result = await _sut.CalculateForPeriodAsync(slo.Id, start, end);

        // Assert
        result.RemainingBudget.Should().Be(0);
        result.TimeToExhaustionHours.Should().BeNull(); // remaining = 0, so no time-to-exhaustion
    }

    [Fact]
    public async Task CalculateForPeriodAsync_WhenZeroAllowedFailures_ShouldReturnZeroRemainingPercentage()
    {
        // Arrange — very tight budget: 0.01% on 100 requests = 0 allowed failures
        var slo = CreateSlo(target: 99.99, errorBudget: 0.01);
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var end = DateTimeOffset.UtcNow;
        var indicators = CreateIndicators(slo.Id, 99, 1, start);

        _sloRepoMock.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _sliRepoMock.Setup(r => r.GetBySloIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(indicators);

        // Act
        var result = await _sut.CalculateForPeriodAsync(slo.Id, start, end);

        // Assert
        result.AllowedFailures.Should().Be(0); // floor(100 * 0.01/100) = 0
        result.RemainingBudgetPercentage.Should().Be(0.0); // 0 allowed → 0%
    }
}

/// <summary>
/// Unit tests for SloViolation.DetermineSeverity — static pure function.
/// </summary>
public class SloViolationSeverityTests
{
    [Theory]
    [InlineData(94.0, 99.9, ViolationSeverity.Critical)]  // 5.9% below → Critical
    [InlineData(97.5, 99.9, ViolationSeverity.High)]      // 2.4% below → High
    [InlineData(99.0, 99.9, ViolationSeverity.Medium)]    // 0.9% below → Medium
    [InlineData(99.5, 99.9, ViolationSeverity.Low)]       // 0.4% below → Low
    public void DetermineSeverity_ShouldReturnCorrectSeverity(double actual, double target, ViolationSeverity expected)
    {
        SloViolation.DetermineSeverity(actual, target).Should().Be(expected);
    }

    [Theory]
    [InlineData(94.9, 99.9, ViolationSeverity.Critical)]  // Exactly 5.0% → Critical
    [InlineData(97.9, 99.9, ViolationSeverity.High)]      // Exactly 2.0% → High
    [InlineData(99.4, 99.9, ViolationSeverity.Medium)]    // Exactly 0.5% → Medium
    [InlineData(99.41, 99.9, ViolationSeverity.Low)]      // Just under 0.5% → Low
    public void DetermineSeverity_BoundaryValues(double actual, double target, ViolationSeverity expected)
    {
        SloViolation.DetermineSeverity(actual, target).Should().Be(expected);
    }
}

/// <summary>
/// Unit tests for ServiceLevelObjective entity methods.
/// </summary>
public class ServiceLevelObjectiveTests
{
    [Fact]
    public void CalculateErrorBudget_ShouldCompute100MinusTarget()
    {
        var slo = new ServiceLevelObjective { TargetPercentage = 99.9 };
        slo.CalculateErrorBudget();
        slo.ErrorBudgetPercentage.Should().BeApproximately(0.1, 0.001);
    }

    [Fact]
    public void UpdateStatus_WhenBelowTarget_ShouldSetBreached()
    {
        var slo = new ServiceLevelObjective
        {
            TargetPercentage = 99.9,
            ErrorBudgetPercentage = 0.1,
            IsEnabled = true
        };

        slo.UpdateStatus(99.0);

        slo.Status.Should().Be(SloStatus.Breached);
        slo.CurrentActualPercentage.Should().Be(99.0);
        slo.LastEvaluatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateStatus_WhenAboveTargetButLowBudget_ShouldSetAtRisk()
    {
        var slo = new ServiceLevelObjective
        {
            TargetPercentage = 99.0,
            ErrorBudgetPercentage = 1.0,
            AlertThresholdPercentage = 50.0,
            IsEnabled = true
        };

        // actual = 99.5 → used = 0.5, remaining = 0.5/1.0 * 100 = 50% — at threshold
        slo.UpdateStatus(99.5);

        slo.Status.Should().Be(SloStatus.AtRisk);
    }

    [Fact]
    public void UpdateStatus_WhenDisabled_ShouldSetDisabledRegardlessOfActual()
    {
        var slo = new ServiceLevelObjective
        {
            TargetPercentage = 99.9,
            IsEnabled = false
        };

        slo.UpdateStatus(50.0); // Terrible actual, but disabled

        slo.Status.Should().Be(SloStatus.Disabled);
    }

    [Fact]
    public void ShouldTriggerAlert_WhenDisabled_ReturnsFalse()
    {
        var slo = new ServiceLevelObjective { IsEnabled = false, Status = SloStatus.Breached };
        slo.ShouldTriggerAlert().Should().BeFalse();
    }

    [Fact]
    public void ShouldTriggerAlert_WhenBreached_ReturnsTrue()
    {
        var slo = new ServiceLevelObjective { IsEnabled = true, Status = SloStatus.Breached };
        slo.ShouldTriggerAlert().Should().BeTrue();
    }

    [Fact]
    public void SloViolation_Resolve_ShouldBeIdempotent()
    {
        var violation = new SloViolation { StartedAt = DateTimeOffset.UtcNow.AddHours(-1) };
        violation.Resolve();
        var firstEndedAt = violation.EndedAt;

        violation.Resolve(); // Second call should not change EndedAt

        violation.EndedAt.Should().Be(firstEndedAt);
    }

    [Fact]
    public void SloViolation_GetDuration_WhenOngoing_ShouldMeasureFromStart()
    {
        var violation = new SloViolation { StartedAt = DateTimeOffset.UtcNow.AddHours(-2) };
        violation.GetDuration().Should().BeCloseTo(TimeSpan.FromHours(2), TimeSpan.FromSeconds(5));
    }
}
