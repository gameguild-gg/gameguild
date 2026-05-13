using FluentAssertions;

using Moq;

using Xunit;

namespace GameGuild.Monitoring.SLA.UnitTests.Services;

public class ErrorBudgetCalculatorTests
{
    private readonly Mock<IServiceLevelIndicatorRepository> _sliRepository = new();
    private readonly Mock<IServiceLevelObjectiveRepository> _sloRepository = new();
    private readonly ErrorBudgetCalculator _sut;

    public ErrorBudgetCalculatorTests()
    {
        _sut = new ErrorBudgetCalculator(_sliRepository.Object, _sloRepository.Object);
    }

    [Fact]
    public async Task CalculateForPeriodAsync_WhenSloNotFound_ShouldThrowInvalidOperationException()
    {
        _sloRepository.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?) null);

        var action = () => _sut.CalculateForPeriodAsync(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CalculateForPeriodAsync_WhenNoRequests_ShouldReturn100PercentActual()
    {
        var slo = CreateSlo();
        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _sliRepository.Setup(repository => repository.GetBySloIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.CalculateForPeriodAsync(slo.Id, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);

        result.ActualPercentage.Should().Be(100.0);
        result.TotalRequests.Should().Be(0);
        result.FailedRequests.Should().Be(0);
        result.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task CalculateForPeriodAsync_ShouldCalculateCorrectPercentages()
    {
        var slo = CreateSlo(target: 99.9, errorBudget: 0.1);
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var end = DateTimeOffset.UtcNow;
        var indicators = CreateIndicators(slo.Id, 990, 10, start);

        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _sliRepository.Setup(repository => repository.GetBySloIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(indicators);

        var result = await _sut.CalculateForPeriodAsync(slo.Id, start, end);

        result.TotalRequests.Should().Be(1000);
        result.SuccessfulRequests.Should().Be(990);
        result.FailedRequests.Should().Be(10);
        result.ActualPercentage.Should().Be(99.0);
        result.AllowedFailures.Should().Be(1);
        result.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task CalculateForPeriodAsync_WhenHealthy_ShouldReturnIsHealthyTrue()
    {
        var slo = CreateSlo(target: 99.0, errorBudget: 1.0);
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var end = DateTimeOffset.UtcNow;
        var indicators = CreateIndicators(slo.Id, 999, 1, start);

        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _sliRepository.Setup(repository => repository.GetBySloIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(indicators);

        var result = await _sut.CalculateForPeriodAsync(slo.Id, start, end);

        result.ActualPercentage.Should().Be(99.9);
        result.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task CalculateForPeriodAsync_ShouldCalculateBurnRateAsFailuresPerDay()
    {
        var slo = CreateSlo(target: 99.0, errorBudget: 1.0);
        var start = DateTimeOffset.UtcNow.AddDays(-2);
        var end = DateTimeOffset.UtcNow;
        var indicators = CreateIndicators(slo.Id, 990, 10, start);

        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _sliRepository.Setup(repository => repository.GetBySloIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(indicators);

        var result = await _sut.CalculateForPeriodAsync(slo.Id, start, end);

        result.BurnRate.Should().BeApproximately(5.0, 0.01);
    }

    [Fact]
    public async Task CalculateForPeriodAsync_WhenBudgetExhausted_ShouldReturnNullTimeToExhaustion()
    {
        var slo = CreateSlo(target: 99.9, errorBudget: 0.1);
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var end = DateTimeOffset.UtcNow;
        var indicators = CreateIndicators(slo.Id, 900, 100, start);

        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _sliRepository.Setup(repository => repository.GetBySloIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(indicators);

        var result = await _sut.CalculateForPeriodAsync(slo.Id, start, end);

        result.RemainingBudget.Should().Be(0);
        result.TimeToExhaustionHours.Should().BeNull();
    }

    [Fact]
    public async Task CalculateForPeriodAsync_WhenZeroAllowedFailures_ShouldReturnZeroRemainingPercentage()
    {
        var slo = CreateSlo(target: 99.99, errorBudget: 0.01);
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var end = DateTimeOffset.UtcNow;
        var indicators = CreateIndicators(slo.Id, 99, 1, start);

        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _sliRepository.Setup(repository => repository.GetBySloIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(indicators);

        var result = await _sut.CalculateForPeriodAsync(slo.Id, start, end);

        result.AllowedFailures.Should().Be(0);
        result.RemainingBudgetPercentage.Should().Be(0.0);
    }

    private static ServiceLevelObjective CreateSlo(Guid? id = null, double target = 99.9, double errorBudget = 0.1, int windowDays = 30)
    {
        return new ServiceLevelObjective
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Test SLO",
            ServiceName = "test-api",
            TargetPercentage = target,
            ErrorBudgetPercentage = errorBudget,
            TimeWindowDays = windowDays,
            IsEnabled = true
        };
    }

    private static List<ServiceLevelIndicator> CreateIndicators(Guid sloId, int successful, int failed, DateTimeOffset baseTime)
    {
        var indicators = new List<ServiceLevelIndicator>();

        for (var index = 0; index < successful; index++)
        {
            indicators.Add(new ServiceLevelIndicator
            {
                ServiceLevelObjectiveId = sloId,
                Timestamp = baseTime.AddMinutes(index),
                IsSuccessful = true
            });
        }

        for (var index = 0; index < failed; index++)
        {
            indicators.Add(new ServiceLevelIndicator
            {
                ServiceLevelObjectiveId = sloId,
                Timestamp = baseTime.AddMinutes(successful + index),
                IsSuccessful = false
            });
        }

        return indicators;
    }
}