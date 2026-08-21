using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using GameGuild.CQRS;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;

using Xunit;

namespace GameGuild.Monitoring.SLA.UnitTests.Services;

public class SlaMonitoringServiceTests
{
    private readonly Mock<IServiceLevelObjectiveRepository> _sloRepository = new();
    private readonly Mock<IServiceLevelIndicatorRepository> _sliRepository = new();
    private readonly Mock<ISloViolationRepository> _violationRepository = new();
    private readonly Mock<IErrorBudgetCalculator> _errorBudgetCalculator = new();
    private readonly Mock<IAlertManager> _alertManager = new();
    private readonly SlaMonitoringService _sut;

    public SlaMonitoringServiceTests()
    {
        _sut = new SlaMonitoringService(
            _sloRepository.Object,
            _sliRepository.Object,
            _violationRepository.Object,
            _errorBudgetCalculator.Object,
            _alertManager.Object);
    }

    [Fact]
    public async Task RecordMetricAsync_ShouldPersistMetricAndEvaluateSlo()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId, isEnabled: true);
        var recordedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        ServiceLevelIndicator? persisted = null;

        SetupSuccessfulEvaluation(slo);
        _sliRepository
            .Setup(repository => repository.AddAsync(It.IsAny<ServiceLevelIndicator>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceLevelIndicator, CancellationToken>((indicator, _) => persisted = indicator)
            .ReturnsAsync((ServiceLevelIndicator indicator, CancellationToken _) => indicator);

        await _sut.RecordMetricAsync(new SliMetricDto
        {
            ServiceLevelObjectiveId = slo.Id,
            Value = 98.5,
            IsSuccessful = false,
            ResponseTimeMs = 1200,
            StatusCode = 503,
            Endpoint = "/health",
            Metadata = """{"region":"us-east-1"}""",
            ErrorMessage = "timeout",
            Timestamp = recordedAt
        }, CancellationToken.None);

        persisted.Should().NotBeNull();
        persisted!.ServiceLevelObjectiveId.Should().Be(slo.Id);
        persisted.TenantId.Should().Be(tenantId);
        persisted.IsSuccessful.Should().BeFalse();
        persisted.Value.Should().Be(98.5);
        persisted.ErrorMessage.Should().Be("timeout");
        persisted.Timestamp.Should().Be(recordedAt);
        persisted.Metadata.Should().Be("""{"region":"us-east-1"}""");
        _errorBudgetCalculator.Verify(calculator => calculator.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()), Times.Once);
        _alertManager.Verify(manager => manager.CheckAndTriggerAlertAsync(slo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAllSlosAsync_ShouldEvaluateOnlyEnabledSlos()
    {
        var tenantId = Guid.NewGuid();
        var enabledSlo = CreateSlo(tenantId, isEnabled: true);
        var disabledSlo = CreateSlo(tenantId, isEnabled: false);

        _sloRepository.Setup(repository => repository.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceLevelObjective> { enabledSlo, disabledSlo });

        SetupSuccessfulEvaluation(enabledSlo);

        await _sut.EvaluateAllSlosAsync(tenantId, CancellationToken.None);

        _errorBudgetCalculator.Verify(calculator => calculator.CalculateAsync(enabledSlo.Id, It.IsAny<CancellationToken>()), Times.Once);
        _errorBudgetCalculator.Verify(calculator => calculator.CalculateAsync(disabledSlo.Id, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateSloAsync_DisabledSlo_ShouldReturn()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: false);

        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);

        await _sut.EvaluateSloAsync(slo.Id, CancellationToken.None);

        _errorBudgetCalculator.Verify(calculator => calculator.CalculateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateSloAsync_MissingSlo_ShouldReturn()
    {
        _sloRepository.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?)null);

        await _sut.EvaluateSloAsync(Guid.NewGuid(), CancellationToken.None);

        _errorBudgetCalculator.Verify(calculator => calculator.CalculateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateSloAsync_EnabledSlo_ShouldUpdateAndAlert()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: true);

        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);
        _errorBudgetCalculator.Setup(calculator => calculator.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 99.5, RemainingBudgetPercentage = 50 });

        await _sut.EvaluateSloAsync(slo.Id, CancellationToken.None);

        _sloRepository.Verify(repository => repository.UpdateAsync(slo, It.IsAny<CancellationToken>()), Times.Once);
        _alertManager.Verify(manager => manager.CheckAndTriggerAlertAsync(slo, It.IsAny<CancellationToken>()), Times.Once);
        slo.CurrentActualPercentage.Should().Be(99.5);
        slo.RemainingErrorBudget.Should().Be(50);
    }

    [Fact]
    public async Task GetComplianceAsync_ShouldReturnDto()
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

        _sloRepository.Setup(repository => repository.GetByIdWithViolationsAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);
        _errorBudgetCalculator.Setup(calculator => calculator.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 99.5, TotalRequests = 1000, SuccessfulRequests = 995 });

        var result = await _sut.GetComplianceAsync(slo.Id, cancellationToken: CancellationToken.None);

        result.ServiceLevelObjectiveId.Should().Be(slo.Id);
        result.ActualPercentage.Should().Be(99.5);
        result.TotalMeasurements.Should().Be(1000);
    }

    [Fact]
    public async Task GetComplianceAsync_MissingSlo_ShouldThrow()
    {
        _sloRepository.Setup(repository => repository.GetByIdWithViolationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?)null);

        var act = () => _sut.GetComplianceAsync(Guid.NewGuid(), cancellationToken: CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetErrorBudgetAsync_ShouldDelegateToCalculator()
    {
        var sloId = Guid.NewGuid();
        var budget = new ErrorBudgetDto { ActualPercentage = 99.9 };

        _errorBudgetCalculator.Setup(calculator => calculator.CalculateAsync(sloId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        var result = await _sut.GetErrorBudgetAsync(sloId, CancellationToken.None);

        result.Should().Be(budget);
    }

    [Fact]
    public async Task CheckErrorBudgetAlertsAsync_DisabledSlo_ShouldReturn()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: false);

        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);

        await _sut.CheckErrorBudgetAlertsAsync(slo.Id, CancellationToken.None);

        _errorBudgetCalculator.Verify(calculator => calculator.CalculateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckErrorBudgetAlertsAsync_ExhaustedBudget_ShouldCreateViolation()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: true);

        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);
        _errorBudgetCalculator.Setup(calculator => calculator.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto
            {
                ActualPercentage = 98,
                TargetPercentage = 99.9,
                RemainingBudgetPercentage = -5
            });
        _violationRepository.Setup(repository => repository.GetOngoingViolationsAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SloViolation>());

        await _sut.CheckErrorBudgetAlertsAsync(slo.Id, CancellationToken.None);

        _violationRepository.Verify(repository => repository.AddAsync(It.IsAny<SloViolation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetErrorBudgetBurnRateAsync_MissingSlo_ShouldReturnZero()
    {
        _sloRepository.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?)null);

        var result = await _sut.GetErrorBudgetBurnRateAsync(Guid.NewGuid(), TimeSpan.FromDays(1), CancellationToken.None);

        result.Should().Be(0);
    }

    [Fact]
    public async Task GetErrorBudgetBurnRateAsync_NoRequests_ShouldReturnZero()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: true);

        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);
        _sliRepository.Setup(repository => repository.GetTotalCountAsync(slo.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _sut.GetErrorBudgetBurnRateAsync(slo.Id, TimeSpan.FromDays(1), CancellationToken.None);

        result.Should().Be(0);
    }

    [Fact]
    public async Task GetErrorBudgetBurnRateAsync_WithRequests_ShouldCalculateRate()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: true);

        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);
        _sliRepository.Setup(repository => repository.GetTotalCountAsync(slo.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1000);
        _sliRepository.Setup(repository => repository.GetSuccessfulCountAsync(slo.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(990);

        var result = await _sut.GetErrorBudgetBurnRateAsync(slo.Id, TimeSpan.FromDays(7), CancellationToken.None);

        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetActiveSloViolationsAsync_ShouldReturnDtos()
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

        _violationRepository.Setup(repository => repository.GetAllOngoingViolationsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SloViolation> { violation });
        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);

        var result = await _sut.GetActiveSloViolationsAsync(cancellationToken: CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Id.Should().Be(violation.Id);
    }

    [Fact]
    public async Task GenerateComplianceReportAsync_ShouldReturnReport()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId, isEnabled: true);

        _sloRepository.Setup(repository => repository.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceLevelObjective> { slo });
        _errorBudgetCalculator.Setup(calculator => calculator.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 99.9, RemainingBudgetPercentage = 80 });
        _violationRepository.Setup(repository => repository.GetBySloIdAndTimeRangeAsync(slo.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SloViolation>());

        var result = await _sut.GenerateComplianceReportAsync(tenantId, DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow, CancellationToken.None);

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
        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);
        _errorBudgetCalculator.Setup(calculator => calculator.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 99.9, RemainingBudgetPercentage = 80 });
    }
}

public class AlertManagerTests
{
    private readonly Mock<ISloViolationRepository> _violationRepository = new();
    private readonly Mock<IErrorBudgetCalculator> _errorBudgetCalculator = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly AlertManager _sut;

    public AlertManagerTests()
    {
        _sut = new AlertManager(
            _violationRepository.Object,
            _errorBudgetCalculator.Object,
            _notificationService.Object);
    }

    [Fact]
    public async Task CheckAndTriggerAlertAsync_NoBreaches_ShouldReturnFalse()
    {
        var slo = CreateSlo();

        _errorBudgetCalculator.Setup(calculator => calculator.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 99.99, RemainingBudgetPercentage = 90, BurnRate = 0 });

        var result = await _sut.CheckAndTriggerAlertAsync(slo, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAndTriggerAlertAsync_BreachedTarget_ShouldTriggerAlert()
    {
        var slo = CreateSlo();

        _errorBudgetCalculator.Setup(calculator => calculator.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 98, RemainingBudgetPercentage = -5, BurnRate = 0 });
        _violationRepository.Setup(repository => repository.GetOngoingViolationsAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SloViolation>());
        SetupSuccessfulNotification();

        var result = await _sut.CheckAndTriggerAlertAsync(slo, CancellationToken.None);

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

        SetupSuccessfulNotification();

        var result = await _sut.SendViolationAlertAsync(violation, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendErrorBudgetAlertAsync_ShouldSendNotification()
    {
        var slo = CreateSlo();

        SetupSuccessfulNotification();

        var result = await _sut.SendErrorBudgetAlertAsync(slo, 5.0, CancellationToken.None);

        result.Should().BeTrue();
    }

    private void SetupSuccessfulNotification()
    {
        _notificationService.Setup(service => service.SendAsync(
                It.IsAny<Guid>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationChannel>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Notification.Create(Guid.Empty, NotificationType.System, NotificationChannel.InApp, "Test", "Test")));
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

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSlaMonitoringApplication_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();

        services.AddSlaMonitoringApplication();

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ISlaMonitoringService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IAlertManager));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IErrorBudgetCalculator));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ICommandHandler<CreateSloCommand, SloDto>));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IQueryHandler<GetSlosQuery, List<SloDto>>));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(FluentValidation.IValidator<CreateSloCommand>));
    }
}
