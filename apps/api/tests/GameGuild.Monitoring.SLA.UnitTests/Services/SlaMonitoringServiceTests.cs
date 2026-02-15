using FluentAssertions;

using GameGuild.Notifications;
using GameGuild.Notifications.Services;

using Moq;

using Xunit;

namespace GameGuild.Monitoring.SLA.Tests;

/// <summary>
///     Tests for SlaMonitoringService to boost coverage on all service methods.
/// </summary>
public class SlaMonitoringServiceTests
{
    private readonly Mock<IServiceLevelObjectiveRepository> _sloRepo = new();
    private readonly Mock<IServiceLevelIndicatorRepository> _sliRepo = new();
    private readonly Mock<ISloViolationRepository> _violationRepo = new();
    private readonly Mock<IErrorBudgetCalculator> _budgetCalculator = new();
    private readonly Mock<IAlertManager> _alertManager = new();
    private readonly SlaMonitoringService _sut;

    public SlaMonitoringServiceTests()
    {
        _sut = new SlaMonitoringService(
            _sloRepo.Object,
            _sliRepo.Object,
            _violationRepo.Object,
            _budgetCalculator.Object,
            _alertManager.Object);
    }

    [Fact]
    public async Task RecordMetricAsync_ShouldThrowNotImplemented()
    {
        var metric = new SliMetricDto();

        var act = () => _sut.RecordMetricAsync(metric);

        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task EvaluateAllSlosAsync_ShouldEvaluateEnabledSlosOnly()
    {
        var tenantId = Guid.NewGuid();
        var enabledSlo = CreateSlo(tenantId, isEnabled: true);
        var disabledSlo = CreateSlo(tenantId, isEnabled: false);

        _sloRepo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceLevelObjective> { enabledSlo, disabledSlo });

        SetupSuccessfulEvaluation(enabledSlo);

        await _sut.EvaluateAllSlosAsync(tenantId);

        _budgetCalculator.Verify(c => c.CalculateAsync(enabledSlo.Id, It.IsAny<CancellationToken>()), Times.Once);
        _budgetCalculator.Verify(c => c.CalculateAsync(disabledSlo.Id, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateSloAsync_DisabledSlo_ShouldReturn()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: false);
        _sloRepo.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);

        await _sut.EvaluateSloAsync(slo.Id);

        _budgetCalculator.Verify(c => c.CalculateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateSloAsync_NullSlo_ShouldReturn()
    {
        _sloRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?)null);

        await _sut.EvaluateSloAsync(Guid.NewGuid());

        _budgetCalculator.Verify(c => c.CalculateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateSloAsync_EnabledSlo_ShouldUpdateStatusAndAlert()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: true);
        _sloRepo.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);

        var budget = new ErrorBudgetDto { ActualPercentage = 99.5, RemainingBudgetPercentage = 50 };
        _budgetCalculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        await _sut.EvaluateSloAsync(slo.Id);

        _sloRepo.Verify(r => r.UpdateAsync(slo, It.IsAny<CancellationToken>()), Times.Once);
        _alertManager.Verify(a => a.CheckAndTriggerAlertAsync(slo, It.IsAny<CancellationToken>()), Times.Once);
        slo.CurrentActualPercentage.Should().Be(99.5);
        slo.RemainingErrorBudget.Should().Be(50);
    }

    [Fact]
    public async Task GetComplianceAsync_ShouldReturnComplianceDto()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: true);
        slo.Violations.Add(new SloViolation
        {
            Id = Guid.NewGuid(),
            ServiceLevelObjectiveId = slo.Id,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-2),
            EndedAt = DateTimeOffset.UtcNow.AddHours(-1),
            ActualValue = 98,
            TargetValue = 99.9,
            Severity = ViolationSeverity.High
        });

        _sloRepo.Setup(r => r.GetByIdWithViolationsAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);

        var budget = new ErrorBudgetDto
        {
            ActualPercentage = 99.5,
            TotalRequests = 1000,
            SuccessfulRequests = 995
        };
        _budgetCalculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        var result = await _sut.GetComplianceAsync(slo.Id);

        result.ServiceLevelObjectiveId.Should().Be(slo.Id);
        result.ActualPercentage.Should().Be(99.5);
        result.TotalMeasurements.Should().Be(1000);
    }

    [Fact]
    public async Task GetComplianceAsync_NullSlo_ShouldThrow()
    {
        _sloRepo.Setup(r => r.GetByIdWithViolationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?)null);

        var act = () => _sut.GetComplianceAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetErrorBudgetAsync_ShouldDelegateToCalculator()
    {
        var sloId = Guid.NewGuid();
        var budget = new ErrorBudgetDto { ActualPercentage = 99.9 };
        _budgetCalculator.Setup(c => c.CalculateAsync(sloId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        var result = await _sut.GetErrorBudgetAsync(sloId);

        result.Should().Be(budget);
    }

    [Fact]
    public async Task CheckErrorBudgetAlertsAsync_DisabledSlo_ShouldReturn()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: false);
        _sloRepo.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);

        await _sut.CheckErrorBudgetAlertsAsync(slo.Id);

        _budgetCalculator.Verify(c => c.CalculateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckErrorBudgetAlertsAsync_BudgetExhausted_ShouldCreateViolation()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: true);
        _sloRepo.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);

        var budget = new ErrorBudgetDto
        {
            ActualPercentage = 98,
            TargetPercentage = 99.9,
            RemainingBudgetPercentage = -5
        };
        _budgetCalculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        _violationRepo.Setup(r => r.GetOngoingViolationsAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SloViolation>());

        await _sut.CheckErrorBudgetAlertsAsync(slo.Id);

        _violationRepo.Verify(r => r.AddAsync(It.IsAny<SloViolation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetErrorBudgetBurnRateAsync_NullSlo_ShouldReturnZero()
    {
        _sloRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?)null);

        var result = await _sut.GetErrorBudgetBurnRateAsync(Guid.NewGuid(), TimeSpan.FromDays(1));

        result.Should().Be(0);
    }

    [Fact]
    public async Task GetErrorBudgetBurnRateAsync_NoRequests_ShouldReturnZero()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: true);
        _sloRepo.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);

        _sliRepo.Setup(r => r.GetTotalCountAsync(slo.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _sut.GetErrorBudgetBurnRateAsync(slo.Id, TimeSpan.FromDays(1));

        result.Should().Be(0);
    }

    [Fact]
    public async Task GetErrorBudgetBurnRateAsync_WithRequests_ShouldCalculateRate()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: true);
        _sloRepo.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);

        _sliRepo.Setup(r => r.GetTotalCountAsync(slo.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1000);
        _sliRepo.Setup(r => r.GetSuccessfulCountAsync(slo.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(990);

        var result = await _sut.GetErrorBudgetBurnRateAsync(slo.Id, TimeSpan.FromDays(7));

        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetActiveSloViolationsAsync_ShouldReturnViolationDtos()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: true);
        var violation = new SloViolation
        {
            Id = Guid.NewGuid(),
            ServiceLevelObjectiveId = slo.Id,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-1),
            ActualValue = 98,
            TargetValue = 99.9,
            Severity = ViolationSeverity.High,
            Description = "Test violation"
        };

        _violationRepo.Setup(r => r.GetAllOngoingViolationsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SloViolation> { violation });
        _sloRepo.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);

        var result = await _sut.GetActiveSloViolationsAsync();

        result.Should().HaveCount(1);
        result.First().Id.Should().Be(violation.Id);
    }

    [Fact]
    public async Task GenerateComplianceReportAsync_ShouldReturnReport()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId, isEnabled: true);

        _sloRepo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceLevelObjective> { slo });

        var budget = new ErrorBudgetDto
        {
            ActualPercentage = 99.9,
            RemainingBudgetPercentage = 80
        };
        _budgetCalculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        _violationRepo.Setup(r => r.GetBySloIdAndTimeRangeAsync(
                slo.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SloViolation>());

        var start = DateTimeOffset.UtcNow.AddDays(-30);
        var end = DateTimeOffset.UtcNow;

        var result = await _sut.GenerateComplianceReportAsync(tenantId, start, end);

        result.TotalSlos.Should().Be(1);
        result.CompliantSlos.Should().Be(1);
        result.OverallCompliancePercentage.Should().Be(100);
    }

    private static ServiceLevelObjective CreateSlo(Guid tenantId, bool isEnabled)
    {
        var slo = new ServiceLevelObjective
        {
            Id = Guid.NewGuid(),
            Name = "Test SLO",
            ServiceName = "test-service",
            TargetPercentage = 99.9,
            TimeWindowDays = 30,
            ErrorBudgetPercentage = 0.1,
            IsEnabled = isEnabled,
            Status = isEnabled ? SloStatus.Active : SloStatus.Disabled
        };
        slo.SetTenantId(tenantId);

        return slo;
    }

    private void SetupSuccessfulEvaluation(ServiceLevelObjective slo)
    {
        _sloRepo.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);

        var budget = new ErrorBudgetDto { ActualPercentage = 99.9, RemainingBudgetPercentage = 80 };
        _budgetCalculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);
    }
}

/// <summary>
///     Tests for AlertManager to boost coverage.
/// </summary>
public class AlertManagerTests
{
    private readonly Mock<ISloViolationRepository> _violationRepo = new();
    private readonly Mock<IErrorBudgetCalculator> _budgetCalculator = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly AlertManager _sut;

    public AlertManagerTests()
    {
        _sut = new AlertManager(
            _violationRepo.Object,
            _budgetCalculator.Object,
            _notificationService.Object);
    }

    [Fact]
    public async Task CheckAndTriggerAlertAsync_NoBreaches_ShouldReturnFalse()
    {
        var slo = CreateSlo();
        var budget = new ErrorBudgetDto
        {
            ActualPercentage = 99.99,
            RemainingBudgetPercentage = 90,
            BurnRate = 0
        };

        _budgetCalculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        var result = await _sut.CheckAndTriggerAlertAsync(slo);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAndTriggerAlertAsync_BreachedTarget_ShouldTriggerAlert()
    {
        var slo = CreateSlo();
        var budget = new ErrorBudgetDto
        {
            ActualPercentage = 98,
            RemainingBudgetPercentage = -5,
            BurnRate = 0
        };

        _budgetCalculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        _violationRepo.Setup(r => r.GetOngoingViolationsAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SloViolation>());

        _notificationService.Setup(n => n.SendAsync(
                It.IsAny<Guid>(), It.IsAny<NotificationType>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<NotificationChannel>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<NotificationPriority>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Notification>(Notification.Create(Guid.Empty, NotificationType.System, NotificationChannel.InApp, "Test", "Test")));

        var result = await _sut.CheckAndTriggerAlertAsync(slo);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendViolationAlertAsync_ShouldSendNotification()
    {
        var violation = new SloViolation
        {
            Id = Guid.NewGuid(),
            ActualValue = 98,
            TargetValue = 99.9,
            Severity = ViolationSeverity.High,
            Description = "Test",
            StartedAt = DateTimeOffset.UtcNow
        };

        _notificationService.Setup(n => n.SendAsync(
                It.IsAny<Guid>(), It.IsAny<NotificationType>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<NotificationChannel>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<NotificationPriority>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Notification>(Notification.Create(Guid.Empty, NotificationType.System, NotificationChannel.InApp, "Test", "Test")));

        var result = await _sut.SendViolationAlertAsync(violation);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendErrorBudgetAlertAsync_ShouldSendNotification()
    {
        var slo = CreateSlo();

        _notificationService.Setup(n => n.SendAsync(
                It.IsAny<Guid>(), It.IsAny<NotificationType>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<NotificationChannel>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<NotificationPriority>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Notification>(Notification.Create(Guid.Empty, NotificationType.System, NotificationChannel.InApp, "Test", "Test")));

        var result = await _sut.SendErrorBudgetAlertAsync(slo, 5.0);

        result.Should().BeTrue();
    }

    private static ServiceLevelObjective CreateSlo()
    {
        var slo = new ServiceLevelObjective
        {
            Id = Guid.NewGuid(),
            Name = "Test SLO",
            ServiceName = "test-service",
            TargetPercentage = 99.9,
            TimeWindowDays = 30,
            ErrorBudgetPercentage = 0.1,
            IsEnabled = true,
            Status = SloStatus.Active
        };
        slo.SetTenantId(Guid.NewGuid());
        return slo;
    }
}
