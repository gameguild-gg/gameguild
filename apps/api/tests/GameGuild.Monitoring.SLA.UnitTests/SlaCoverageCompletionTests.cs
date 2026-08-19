using FluentAssertions;

using Microsoft.AspNetCore.Mvc;

using Moq;

using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;

using Xunit;

using static GameGuild.Monitoring.SLA.UnitTests.Coverage.SlaCoverageGapTestData;

namespace GameGuild.Monitoring.SLA.UnitTests.Coverage;

public class SlaEntityCoverageGapTests
{
    [Fact]
    public void ServiceLevelIndicator_ShouldExposeNavigationProperty()
    {
        var slo = CreateSlo(Guid.NewGuid());
        var indicator = new ServiceLevelIndicator
        {
            ServiceLevelObjectiveId = slo.Id,
            ServiceLevelObjective = slo,
            Timestamp = DateTimeOffset.UtcNow,
            Value = 99.9,
            IsSuccessful = true
        };

        indicator.ServiceLevelObjective.Should().BeSameAs(slo);
    }

    [Fact]
    public void SloViolation_ShouldExposeNavigationProperty_AndResolvedDuration()
    {
        var slo = CreateSlo(Guid.NewGuid());
        var start = DateTimeOffset.UtcNow.AddHours(-2);
        var end = DateTimeOffset.UtcNow.AddHours(-1);
        var violation = new SloViolation
        {
            ServiceLevelObjectiveId = slo.Id,
            ServiceLevelObjective = slo,
            StartedAt = start,
            EndedAt = end,
            ActualValue = 98,
            TargetValue = 99.9,
            Severity = ViolationSeverity.High
        };

        violation.ServiceLevelObjective.Should().BeSameAs(slo);
        violation.GetDuration().TotalHours.Should().BeApproximately(1.0, 0.01);
    }

    [Fact]
    public void SloComplianceDto_ShouldStoreRemainingBudgetAndCalculatedAt()
    {
        var calculatedAt = DateTimeOffset.UtcNow;
        var dto = new SloComplianceDto
        {
            RemainingErrorBudget = 44.4,
            CalculatedAt = calculatedAt
        };

        dto.RemainingErrorBudget.Should().Be(44.4);
        dto.CalculatedAt.Should().Be(calculatedAt);
    }
}

public class SlaValidatorCoverageGapTests
{
    [Fact]
    public void GetSloComplianceQuery_WithOnlyStartDate_ShouldPass()
    {
        var validator = new GetSloComplianceQueryValidator();
        var query = new GetSloComplianceQuery(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1), null);

        validator.Validate(query).IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetSloComplianceQuery_WithOnlyEndDate_ShouldPass()
    {
        var validator = new GetSloComplianceQueryValidator();
        var query = new GetSloComplianceQuery(Guid.NewGuid(), Guid.NewGuid(), null, DateTimeOffset.UtcNow.AddDays(-1));

        validator.Validate(query).IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetSloComplianceQuery_WithValidRange_ShouldPass()
    {
        var validator = new GetSloComplianceQueryValidator();
        var start = DateTimeOffset.UtcNow.AddDays(-10);
        var end = DateTimeOffset.UtcNow.AddDays(-1);
        var query = new GetSloComplianceQuery(Guid.NewGuid(), Guid.NewGuid(), start, end);

        validator.Validate(query).IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetSloViolationsQuery_WithOnlyStartDate_ShouldPass()
    {
        var validator = new GetSloViolationsQueryValidator();
        var query = new GetSloViolationsQuery(StartDate: DateTimeOffset.UtcNow.AddDays(-2));

        validator.Validate(query).IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetSloViolationsQuery_WithOnlyEndDate_ShouldPass()
    {
        var validator = new GetSloViolationsQueryValidator();
        var query = new GetSloViolationsQuery(EndDate: DateTimeOffset.UtcNow.AddDays(-1));

        validator.Validate(query).IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetSloViolationsQuery_WithValidRange_ShouldPass()
    {
        var validator = new GetSloViolationsQueryValidator();
        var start = DateTimeOffset.UtcNow.AddDays(-5);
        var end = DateTimeOffset.UtcNow.AddDays(-2);
        var query = new GetSloViolationsQuery(StartDate: start, EndDate: end);

        validator.Validate(query).IsValid.Should().BeTrue();
    }
}

public class SlaHandlerCoverageGapTests
{
    [Fact]
    public async Task DeleteSloCommandHandler_WhenSloTenantIsNull_ShouldThrowUnauthorizedAccessException()
    {
        var slo = new ServiceLevelObjective { Id = Guid.NewGuid(), Name = "Test", ServiceName = "svc", IsEnabled = true };
        var repository = new Mock<IServiceLevelObjectiveRepository>();
        var sut = new DeleteSloCommandHandler(repository.Object);
        repository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var action = () => sut.Handle(new DeleteSloCommand(slo.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateSloCommandHandler_WhenRepositoryAddFails_ShouldPropagate()
    {
        var repository = new Mock<IServiceLevelObjectiveRepository>();
        var sut = new CreateSloCommandHandler(repository.Object);
        var command = new CreateSloCommand(Guid.NewGuid(), "API", null, "svc", 99.9, 30, 0.1, 50);

        repository.Setup(r => r.ExistsByNameAsync(command.Name, command.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(r => r.AddAsync(It.IsAny<ServiceLevelObjective>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db write failed"));

        var action = () => sut.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("db write failed");
    }

    [Fact]
    public async Task UpdateSloCommandHandler_WhenRepositoryUpdateFails_ShouldPropagate()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId);
        var repository = new Mock<IServiceLevelObjectiveRepository>();
        var sut = new UpdateSloCommandHandler(repository.Object);

        repository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        repository.Setup(r => r.UpdateAsync(slo, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("update failed"));

        var action = () => sut.Handle(new UpdateSloCommand(slo.Id, tenantId, "Renamed", null, "svc", 99.9, 30, 0.1, 50, true), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("update failed");
    }

    [Fact]
    public async Task UpdateSloCommandHandler_WhenSloTenantIsNull_ShouldThrowUnauthorizedAccessException()
    {
        var slo = new ServiceLevelObjective { Id = Guid.NewGuid(), Name = "Test", ServiceName = "svc", IsEnabled = true };
        var repository = new Mock<IServiceLevelObjectiveRepository>();
        var sut = new UpdateSloCommandHandler(repository.Object);

        repository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var action = () => sut.Handle(new UpdateSloCommand(slo.Id, Guid.NewGuid(), "Name", null, "svc", 99.9, 30, 0.1, 50, true), CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task DeleteSloCommandHandler_WhenRepositoryDeleteFails_ShouldPropagate()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId);
        var repository = new Mock<IServiceLevelObjectiveRepository>();
        var sut = new DeleteSloCommandHandler(repository.Object);

        repository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        repository.Setup(r => r.DeleteAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("delete failed"));

        var action = () => sut.Handle(new DeleteSloCommand(slo.Id, tenantId), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("delete failed");
    }

    [Fact]
    public async Task ResolveSloViolationCommandHandler_WhenRepositoryUpdateFails_ShouldPropagate()
    {
        var tenantId = Guid.NewGuid();
        var violation = CreateViolation(tenantId);
        var repository = new Mock<ISloViolationRepository>();
        var sut = new ResolveSloViolationCommandHandler(repository.Object);

        repository.Setup(r => r.GetByIdAsync(violation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(violation);
        repository.Setup(r => r.UpdateAsync(violation, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("save failed"));

        var action = () => sut.Handle(new CoverageResolveViolationCommand(violation.Id, tenantId, "resolved"), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("save failed");
    }

    [Fact]
    public async Task ResolveSloViolationCommandHandler_WhenViolationTenantIsNull_ShouldThrowUnauthorizedAccessException()
    {
        var violation = new SloViolation
        {
            Id = Guid.NewGuid(),
            ServiceLevelObjectiveId = Guid.NewGuid(),
            StartedAt = DateTimeOffset.UtcNow.AddHours(-1),
            ActualValue = 98,
            TargetValue = 99.9,
            Severity = ViolationSeverity.High
        };
        var repository = new Mock<ISloViolationRepository>();
        var sut = new ResolveSloViolationCommandHandler(repository.Object);

        repository.Setup(r => r.GetByIdAsync(violation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(violation);

        var action = () => sut.Handle(new CoverageResolveViolationCommand(violation.Id, Guid.NewGuid(), "resolved"), CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RecordSliMetricCommandHandler_WhenRepositoryAddFails_ShouldPropagate()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId);
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sliRepository = new Mock<IServiceLevelIndicatorRepository>();
        var monitoringService = new Mock<ISlaMonitoringService>();
        var sut = new RecordSliMetricCommandHandler(sloRepository.Object, sliRepository.Object, monitoringService.Object);

        sloRepository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        sliRepository.Setup(r => r.AddAsync(It.IsAny<ServiceLevelIndicator>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("metric insert failed"));

        var action = () => sut.Handle(new RecordSliMetricCommand(tenantId, slo.Id, true, 99.9), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("metric insert failed");
    }

    [Fact]
    public async Task RecordSliMetricCommandHandler_WhenBackgroundEvaluationFails_ShouldSwallowFailure()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId);
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sliRepository = new Mock<IServiceLevelIndicatorRepository>();
        var monitoringService = new Mock<ISlaMonitoringService>();
        var sut = new RecordSliMetricCommandHandler(sloRepository.Object, sliRepository.Object, monitoringService.Object);
        var backgroundStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        sloRepository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        sliRepository.Setup(r => r.AddAsync(It.IsAny<ServiceLevelIndicator>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelIndicator sli, CancellationToken _) => sli);
        monitoringService.Setup(s => s.EvaluateSloAsync(slo.Id, It.IsAny<CancellationToken>()))
            .Returns<Guid, CancellationToken>((_, _) =>
            {
                backgroundStarted.TrySetResult();
                throw new InvalidOperationException("background failure");
            });

        var result = await sut.Handle(new RecordSliMetricCommand(tenantId, slo.Id, true, 99.9), CancellationToken.None);
        await backgroundStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task RecordSliMetricCommandHandler_WhenSloTenantIsNull_ShouldThrowUnauthorizedAccessException()
    {
        var slo = new ServiceLevelObjective { Id = Guid.NewGuid(), Name = "Test", ServiceName = "svc", IsEnabled = true };
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sliRepository = new Mock<IServiceLevelIndicatorRepository>();
        var monitoringService = new Mock<ISlaMonitoringService>();
        var sut = new RecordSliMetricCommandHandler(sloRepository.Object, sliRepository.Object, monitoringService.Object);

        sloRepository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var action = () => sut.Handle(new RecordSliMetricCommand(Guid.NewGuid(), slo.Id, true, 99.9), CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetErrorBudgetQueryHandler_WhenCalculatorThrows_ShouldPropagate()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId);
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var sut = new GetErrorBudgetQueryHandler(sloRepository.Object, calculator.Object);

        sloRepository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        calculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("calc failed"));

        var action = () => sut.Handle(new GetErrorBudgetQuery(slo.Id, tenantId), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("calc failed");
    }

    [Fact]
    public async Task GetErrorBudgetQueryHandler_WhenSloTenantIsNull_ShouldReturnNull()
    {
        var slo = new ServiceLevelObjective { Id = Guid.NewGuid(), Name = "Test", ServiceName = "svc", IsEnabled = true };
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var sut = new GetErrorBudgetQueryHandler(sloRepository.Object, calculator.Object);

        sloRepository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var result = await sut.Handle(new GetErrorBudgetQuery(slo.Id, Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSloByIdQueryHandler_WhenRepositoryThrows_ShouldPropagate()
    {
        var repository = new Mock<IServiceLevelObjectiveRepository>();
        var sut = new GetSloByIdQueryHandler(repository.Object);
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("lookup failed"));

        var action = () => sut.Handle(new GetSloByIdQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("lookup failed");
    }

    [Fact]
    public async Task GetSloByIdQueryHandler_WhenSloTenantIsNull_ShouldReturnNull()
    {
        var slo = new ServiceLevelObjective { Id = Guid.NewGuid(), Name = "Test", ServiceName = "svc", IsEnabled = true };
        var repository = new Mock<IServiceLevelObjectiveRepository>();
        var sut = new GetSloByIdQueryHandler(repository.Object);

        repository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var result = await sut.Handle(new GetSloByIdQuery(slo.Id, Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSloComplianceQueryHandler_WhenMonitoringServiceThrows_ShouldPropagate()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId);
        var monitoringService = new Mock<ISlaMonitoringService>();
        var repository = new Mock<IServiceLevelObjectiveRepository>();
        var sut = new GetSloComplianceQueryHandler(monitoringService.Object, repository.Object);

        repository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        monitoringService.Setup(s => s.GetComplianceAsync(slo.Id, It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("compliance failed"));

        var action = () => sut.Handle(new GetSloComplianceQuery(slo.Id, tenantId), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("compliance failed");
    }

    [Fact]
    public async Task GetSloComplianceQueryHandler_WhenSloTenantIsNull_ShouldThrowUnauthorizedAccessException()
    {
        var slo = new ServiceLevelObjective { Id = Guid.NewGuid(), Name = "Test", ServiceName = "svc", IsEnabled = true };
        var monitoringService = new Mock<ISlaMonitoringService>();
        var repository = new Mock<IServiceLevelObjectiveRepository>();
        var sut = new GetSloComplianceQueryHandler(monitoringService.Object, repository.Object);

        repository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var action = () => sut.Handle(new GetSloComplianceQuery(slo.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private sealed record CoverageResolveViolationCommand(Guid ViolationId, Guid TenantId, string? ResolutionNotes = null)
        : ResolveSloViolationCommand(ViolationId, TenantId, ResolutionNotes);
}

public class SlaControllerCoverageGapTests
{
    [Fact]
    public async Task UpdateSlo_WhenTenantMissing_ShouldUseActorTenant()
    {
        var sender = new Mock<ISender>();
        var actorTenantId = Guid.NewGuid();
        var controller = CreateController(sender, actorTenantId);
        var id = Guid.NewGuid();
        var command = new UpdateSloCommand(id, Guid.Empty, "Name", null, "Svc", 99.9, 30, 0.1, 50, true);
        sender.Setup(s => s.Send(It.Is<UpdateSloCommand>(value => value.TenantId == actorTenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SloDto { Id = id, TenantId = actorTenantId, Name = "Name" });

        var result = await controller.UpdateSlo(id, command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetViolations_WithExplicitTenant_ShouldPreferExplicitTenant()
    {
        var sender = new Mock<ISender>();
        var explicitTenantId = Guid.NewGuid();
        var controller = CreateController(sender, Guid.NewGuid());
        sender.Setup(s => s.Send(It.Is<GetSloViolationsQuery>(value => value.TenantId == explicitTenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await controller.GetViolations(tenantId: explicitTenantId, cancellationToken: CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RecordSliMetric_WhenTenantMissing_ShouldUseActorTenant()
    {
        var sender = new Mock<ISender>();
        var actorTenantId = Guid.NewGuid();
        var controller = CreateController(sender, actorTenantId);
        sender.Setup(s => s.Send(It.Is<RecordSliMetricCommand>(value => value.TenantId == actorTenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SliMetricDto());

        var result = await controller.RecordSliMetric(new RecordSliMetricCommand(Guid.Empty, Guid.NewGuid(), true, 99.9), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateSlo_WhenSenderThrows_ShouldPropagate()
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender, Guid.NewGuid());
        var id = Guid.NewGuid();
        var command = new UpdateSloCommand(id, Guid.NewGuid(), "Name", null, "Svc", 99.9, 30, 0.1, 50, true);
        sender.Setup(s => s.Send(It.IsAny<UpdateSloCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("send failed"));

        var action = () => controller.UpdateSlo(id, command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("send failed");
    }

    [Fact]
    public async Task GetViolations_WhenSenderThrows_ShouldPropagate()
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender, Guid.NewGuid());
        sender.Setup(s => s.Send(It.IsAny<GetSloViolationsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("query failed"));

        var action = () => controller.GetViolations(cancellationToken: CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("query failed");
    }

    [Fact]
    public async Task RecordSliMetric_WhenSenderThrows_ShouldPropagate()
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender, Guid.NewGuid());
        sender.Setup(s => s.Send(It.IsAny<RecordSliMetricCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("metric send failed"));

        var action = () => controller.RecordSliMetric(new RecordSliMetricCommand(Guid.NewGuid(), Guid.NewGuid(), true, 99.9), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("metric send failed");
    }

    private static SlaMonitoringController CreateController(Mock<ISender> sender, Guid tenantId)
    {
        var actorContextAccessor = new Mock<IActorContextAccessor>();
        actorContextAccessor.Setup(accessor => accessor.ActorContext)
            .Returns(new ActorContext
            {
                TenantId = tenantId,
                ActorKind = ActorKind.User,
                Roles = new HashSet<string>(),
                Permissions = new HashSet<string>(),
                IsAuthenticated = true
            });

        return new SlaMonitoringController(sender.Object, actorContextAccessor.Object);
    }
}

public class AlertManagerCoverageGapTests
{
    [Fact]
    public async Task CheckAndTriggerAlertAsync_WithExistingOngoingViolation_ShouldNotCreateDuplicate()
    {
        var slo = CreateSlo(Guid.NewGuid());
        var violationRepository = new Mock<ISloViolationRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var notificationService = new Mock<INotificationService>();
        var sut = new AlertManager(violationRepository.Object, calculator.Object, notificationService.Object);

        calculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 98.0, RemainingBudgetPercentage = 80.0, BurnRate = 0.0 });
        violationRepository.Setup(r => r.GetOngoingViolationsAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateViolation(slo.TenantId!.Value, slo.Id)]);

        var result = await sut.CheckAndTriggerAlertAsync(slo, CancellationToken.None);

        result.Should().BeTrue();
        violationRepository.Verify(r => r.AddAsync(It.IsAny<SloViolation>(), It.IsAny<CancellationToken>()), Times.Never);
        notificationService.Verify(service => service.SendAsync(
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
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckAndTriggerAlertAsync_WhenThresholdReached_ShouldSendErrorBudgetAlert()
    {
        var slo = CreateSlo(Guid.NewGuid());
        slo.Status = SloStatus.AtRisk;
        slo.RemainingErrorBudget = 40.0;
        var violationRepository = new Mock<ISloViolationRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var notificationService = new Mock<INotificationService>();
        var sut = new AlertManager(violationRepository.Object, calculator.Object, notificationService.Object);

        calculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 99.95, RemainingBudgetPercentage = 40.0, BurnRate = 0.0 });
        SetupSuccessfulNotification(notificationService);

        var result = await sut.CheckAndTriggerAlertAsync(slo, CancellationToken.None);

        result.Should().BeTrue();
        notificationService.Verify(service => service.SendAsync(
            It.IsAny<Guid>(),
            It.IsAny<NotificationType>(),
            It.Is<string>(title => title.Contains("Error Budget Alert")),
            It.IsAny<string>(),
            It.IsAny<NotificationChannel>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<NotificationPriority>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckAndTriggerAlertAsync_WhenBurnRateExhaustsWithinDay_ShouldSendErrorBudgetAlert()
    {
        var slo = CreateSlo(Guid.NewGuid());
        var violationRepository = new Mock<ISloViolationRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var notificationService = new Mock<INotificationService>();
        var sut = new AlertManager(violationRepository.Object, calculator.Object, notificationService.Object);

        calculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto
            {
                ActualPercentage = 99.95,
                RemainingBudgetPercentage = 80.0,
                BurnRate = 1.0,
                TimeToExhaustionHours = 12.0
            });
        SetupSuccessfulNotification(notificationService);

        var result = await sut.CheckAndTriggerAlertAsync(slo, CancellationToken.None);

        result.Should().BeTrue();
        notificationService.Verify(service => service.SendAsync(
            It.IsAny<Guid>(),
            It.IsAny<NotificationType>(),
            It.Is<string>(title => title.Contains("Error Budget Alert")),
            It.IsAny<string>(),
            It.IsAny<NotificationChannel>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<NotificationPriority>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class ErrorBudgetCalculatorCoverageGapTests
{
    [Fact]
    public async Task CalculateAsync_WhenSloMissing_ShouldThrowInvalidOperationException()
    {
        var indicatorRepository = new Mock<IServiceLevelIndicatorRepository>();
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sut = new ErrorBudgetCalculator(indicatorRepository.Object, sloRepository.Object);

        sloRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?) null);

        var action = () => sut.CalculateAsync(Guid.NewGuid(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CalculateAsync_ShouldUseConfiguredWindow_AndIgnoreOutOfRangeIndicators()
    {
        var sloId = Guid.NewGuid();
        var slo = CreateSlo(Guid.NewGuid());
        slo.Id = sloId;
        slo.TimeWindowDays = 1;
        var inRange = new ServiceLevelIndicator
        {
            ServiceLevelObjectiveId = sloId,
            Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
            Value = 100,
            IsSuccessful = true
        };
        var outOfRange = new ServiceLevelIndicator
        {
            ServiceLevelObjectiveId = sloId,
            Timestamp = DateTimeOffset.UtcNow.AddDays(-5),
            Value = 0,
            IsSuccessful = false
        };
        var indicatorRepository = new Mock<IServiceLevelIndicatorRepository>();
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sut = new ErrorBudgetCalculator(indicatorRepository.Object, sloRepository.Object);

        sloRepository.Setup(r => r.GetByIdAsync(sloId, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        indicatorRepository.Setup(r => r.GetBySloIdAsync(sloId, It.IsAny<CancellationToken>())).ReturnsAsync([inRange, outOfRange]);

        var result = await sut.CalculateAsync(sloId, CancellationToken.None);

        result.TotalRequests.Should().Be(1);
        result.SuccessfulRequests.Should().Be(1);
        result.FailedRequests.Should().Be(0);
    }

    [Fact]
    public async Task CalculateForPeriodAsync_WhenBurnRateIsZeroAndBudgetRemains_ShouldKeepTimeToExhaustionNull()
    {
        var sloId = Guid.NewGuid();
        var slo = CreateSlo(Guid.NewGuid());
        slo.Id = sloId;
        slo.TargetPercentage = 99.0;
        slo.ErrorBudgetPercentage = 1.0;
        var indicatorRepository = new Mock<IServiceLevelIndicatorRepository>();
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sut = new ErrorBudgetCalculator(indicatorRepository.Object, sloRepository.Object);
        var indicators = Enumerable.Range(0, 100)
            .Select(index => new ServiceLevelIndicator
            {
                ServiceLevelObjectiveId = sloId,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-index),
                Value = 100,
                IsSuccessful = true
            })
            .ToList();

        sloRepository.Setup(r => r.GetByIdAsync(sloId, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        indicatorRepository.Setup(r => r.GetBySloIdAsync(sloId, It.IsAny<CancellationToken>())).ReturnsAsync(indicators);

        var result = await sut.CalculateForPeriodAsync(sloId, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow, CancellationToken.None);

        result.BurnRate.Should().Be(0);
        result.TimeToExhaustionHours.Should().BeNull();
    }

    [Fact]
    public async Task CalculateForPeriodAsync_WhenPeriodLengthIsZero_ShouldUseZeroBurnRate()
    {
        var sloId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var slo = CreateSlo(Guid.NewGuid());
        slo.Id = sloId;
        var indicatorRepository = new Mock<IServiceLevelIndicatorRepository>();
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sut = new ErrorBudgetCalculator(indicatorRepository.Object, sloRepository.Object);

        sloRepository.Setup(r => r.GetByIdAsync(sloId, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        indicatorRepository.Setup(r => r.GetBySloIdAsync(sloId, It.IsAny<CancellationToken>())).ReturnsAsync([
            new ServiceLevelIndicator
            {
                ServiceLevelObjectiveId = sloId,
                Timestamp = timestamp,
                Value = 0,
                IsSuccessful = false
            }
        ]);

        var result = await sut.CalculateForPeriodAsync(sloId, timestamp, timestamp, CancellationToken.None);

        result.BurnRate.Should().Be(0);
        result.TimeToExhaustionHours.Should().BeNull();
    }
}

public class SlaMonitoringServiceCoverageGapTests
{
    [Fact]
    public async Task CheckErrorBudgetAlertsAsync_WhenSloIsMissing_ShouldReturnWithoutWork()
    {
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sliRepository = new Mock<IServiceLevelIndicatorRepository>();
        var violationRepository = new Mock<ISloViolationRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var alertManager = new Mock<IAlertManager>();
        var sut = new SlaMonitoringService(sloRepository.Object, sliRepository.Object, violationRepository.Object, calculator.Object, alertManager.Object);

        sloRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?) null);

        await sut.CheckErrorBudgetAlertsAsync(Guid.NewGuid(), CancellationToken.None);

        calculator.Verify(c => c.CalculateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        alertManager.Verify(a => a.CheckAndTriggerAlertAsync(It.IsAny<ServiceLevelObjective>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAllSlosAsync_WhenOneEvaluationFails_ShouldContinueWithNextSlo()
    {
        var tenantId = Guid.NewGuid();
        var failingSlo = CreateSlo(tenantId);
        var succeedingSlo = CreateSlo(tenantId);
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sliRepository = new Mock<IServiceLevelIndicatorRepository>();
        var violationRepository = new Mock<ISloViolationRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var alertManager = new Mock<IAlertManager>();
        var sut = new SlaMonitoringService(sloRepository.Object, sliRepository.Object, violationRepository.Object, calculator.Object, alertManager.Object);

        sloRepository.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([failingSlo, succeedingSlo]);
        sloRepository.Setup(r => r.GetByIdAsync(failingSlo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(failingSlo);
        sloRepository.Setup(r => r.GetByIdAsync(succeedingSlo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(succeedingSlo);
        calculator.Setup(c => c.CalculateAsync(failingSlo.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        calculator.Setup(c => c.CalculateAsync(succeedingSlo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 99.9, RemainingBudgetPercentage = 80.0 });

        await sut.EvaluateAllSlosAsync(tenantId, CancellationToken.None);

        sloRepository.Verify(r => r.UpdateAsync(succeedingSlo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetComplianceAsync_WithExplicitRange_ShouldFilterViolationsAndDowntime()
    {
        var slo = CreateSlo(Guid.NewGuid());
        var start = DateTimeOffset.UtcNow.AddDays(-2);
        var end = DateTimeOffset.UtcNow;
        slo.Violations.Add(new SloViolation
        {
            ServiceLevelObjectiveId = slo.Id,
            StartedAt = DateTimeOffset.UtcNow.AddDays(-1),
            EndedAt = DateTimeOffset.UtcNow.AddHours(-20),
            ActualValue = 98,
            TargetValue = 99.9,
            Severity = ViolationSeverity.High
        });
        slo.Violations.Add(new SloViolation
        {
            ServiceLevelObjectiveId = slo.Id,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-10),
            EndedAt = null,
            ActualValue = 97,
            TargetValue = 99.9,
            Severity = ViolationSeverity.High
        });
        slo.Violations.Add(new SloViolation
        {
            ServiceLevelObjectiveId = slo.Id,
            StartedAt = DateTimeOffset.UtcNow.AddDays(-10),
            EndedAt = DateTimeOffset.UtcNow.AddDays(-9),
            ActualValue = 97,
            TargetValue = 99.9,
            Severity = ViolationSeverity.High
        });
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sliRepository = new Mock<IServiceLevelIndicatorRepository>();
        var violationRepository = new Mock<ISloViolationRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var alertManager = new Mock<IAlertManager>();
        var sut = new SlaMonitoringService(sloRepository.Object, sliRepository.Object, violationRepository.Object, calculator.Object, alertManager.Object);

        sloRepository.Setup(r => r.GetByIdWithViolationsAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        calculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 99.5, TotalRequests = 200, SuccessfulRequests = 199 });

        var result = await sut.GetComplianceAsync(slo.Id, start, end, CancellationToken.None);

        result.ViolationCount.Should().Be(2);
        result.TotalDowntimeMinutes.Should().BeApproximately(240.0, 0.1);
    }

    [Fact]
    public async Task CheckErrorBudgetAlertsAsync_WhenViolationAlreadyExists_ShouldNotCreateDuplicate()
    {
        var slo = CreateSlo(Guid.NewGuid());
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sliRepository = new Mock<IServiceLevelIndicatorRepository>();
        var violationRepository = new Mock<ISloViolationRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var alertManager = new Mock<IAlertManager>();
        var sut = new SlaMonitoringService(sloRepository.Object, sliRepository.Object, violationRepository.Object, calculator.Object, alertManager.Object);

        sloRepository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        calculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 98.0, TargetPercentage = 99.9, RemainingBudgetPercentage = -1.0 });
        violationRepository.Setup(r => r.GetOngoingViolationsAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateViolation(slo.TenantId!.Value, slo.Id)]);

        await sut.CheckErrorBudgetAlertsAsync(slo.Id, CancellationToken.None);

        violationRepository.Verify(r => r.AddAsync(It.IsAny<SloViolation>(), It.IsAny<CancellationToken>()), Times.Never);
        alertManager.Verify(a => a.CheckAndTriggerAlertAsync(slo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckErrorBudgetAlertsAsync_WhenBudgetIsFarBelowZero_ShouldCreateCriticalViolation()
    {
        var slo = CreateSlo(Guid.NewGuid());
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sliRepository = new Mock<IServiceLevelIndicatorRepository>();
        var violationRepository = new Mock<ISloViolationRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var alertManager = new Mock<IAlertManager>();
        var sut = new SlaMonitoringService(sloRepository.Object, sliRepository.Object, violationRepository.Object, calculator.Object, alertManager.Object);
        SloViolation? capturedViolation = null;

        sloRepository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        calculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 95.0, TargetPercentage = 99.9, RemainingBudgetPercentage = -15.0 });
        violationRepository.Setup(r => r.GetOngoingViolationsAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        violationRepository.Setup(r => r.AddAsync(It.IsAny<SloViolation>(), It.IsAny<CancellationToken>()))
            .Callback<SloViolation, CancellationToken>((violation, _) => capturedViolation = violation)
            .ReturnsAsync((SloViolation violation, CancellationToken _) => violation);

        await sut.CheckErrorBudgetAlertsAsync(slo.Id, CancellationToken.None);

        capturedViolation.Should().NotBeNull();
        capturedViolation!.Severity.Should().Be(ViolationSeverity.Critical);
    }

    [Fact]
    public async Task CheckErrorBudgetAlertsAsync_WhenBudgetRemains_ShouldOnlyEvaluateAlerts()
    {
        var slo = CreateSlo(Guid.NewGuid());
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sliRepository = new Mock<IServiceLevelIndicatorRepository>();
        var violationRepository = new Mock<ISloViolationRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var alertManager = new Mock<IAlertManager>();
        var sut = new SlaMonitoringService(sloRepository.Object, sliRepository.Object, violationRepository.Object, calculator.Object, alertManager.Object);

        sloRepository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        calculator.Setup(c => c.CalculateAsync(slo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 99.95, TargetPercentage = 99.9, RemainingBudgetPercentage = 10.0 });

        await sut.CheckErrorBudgetAlertsAsync(slo.Id, CancellationToken.None);

        violationRepository.Verify(r => r.GetOngoingViolationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        alertManager.Verify(a => a.CheckAndTriggerAlertAsync(slo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetErrorBudgetBurnRateAsync_WhenTargetAllowsNoErrors_ShouldReturnZero()
    {
        var slo = CreateSlo(Guid.NewGuid());
        slo.TargetPercentage = 100.0;
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sliRepository = new Mock<IServiceLevelIndicatorRepository>();
        var violationRepository = new Mock<ISloViolationRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var alertManager = new Mock<IAlertManager>();
        var sut = new SlaMonitoringService(sloRepository.Object, sliRepository.Object, violationRepository.Object, calculator.Object, alertManager.Object);

        sloRepository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        sliRepository.Setup(r => r.GetTotalCountAsync(slo.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        sliRepository.Setup(r => r.GetSuccessfulCountAsync(slo.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(90);

        var result = await sut.GetErrorBudgetBurnRateAsync(slo.Id, TimeSpan.FromDays(1), CancellationToken.None);

        result.Should().Be(0);
    }

    [Fact]
    public async Task GetErrorBudgetBurnRateAsync_WhenWindowIsZero_ShouldReturnRawBurnRate()
    {
        var slo = CreateSlo(Guid.NewGuid());
        slo.TargetPercentage = 99.0;
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sliRepository = new Mock<IServiceLevelIndicatorRepository>();
        var violationRepository = new Mock<ISloViolationRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var alertManager = new Mock<IAlertManager>();
        var sut = new SlaMonitoringService(sloRepository.Object, sliRepository.Object, violationRepository.Object, calculator.Object, alertManager.Object);

        sloRepository.Setup(r => r.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        sliRepository.Setup(r => r.GetTotalCountAsync(slo.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(1000);
        sliRepository.Setup(r => r.GetSuccessfulCountAsync(slo.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(990);

        var result = await sut.GetErrorBudgetBurnRateAsync(slo.Id, TimeSpan.Zero, CancellationToken.None);

        result.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public async Task GetActiveSloViolationsAsync_WhenSloLookupFails_ShouldSkipViolation()
    {
        var violation = CreateViolation(Guid.NewGuid());
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sliRepository = new Mock<IServiceLevelIndicatorRepository>();
        var violationRepository = new Mock<ISloViolationRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var alertManager = new Mock<IAlertManager>();
        var sut = new SlaMonitoringService(sloRepository.Object, sliRepository.Object, violationRepository.Object, calculator.Object, alertManager.Object);

        violationRepository.Setup(r => r.GetAllOngoingViolationsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync([violation]);
        sloRepository.Setup(r => r.GetByIdAsync(violation.ServiceLevelObjectiveId, It.IsAny<CancellationToken>())).ReturnsAsync((ServiceLevelObjective?) null);

        var result = await sut.GetActiveSloViolationsAsync(cancellationToken: CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateComplianceReportAsync_WhenTenantIsNull_ShouldUseAllSlos()
    {
        var compliant = CreateSlo(Guid.NewGuid(), name: "API");
        var violated = CreateSlo(Guid.NewGuid(), name: "Billing");
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sliRepository = new Mock<IServiceLevelIndicatorRepository>();
        var violationRepository = new Mock<ISloViolationRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var alertManager = new Mock<IAlertManager>();
        var sut = new SlaMonitoringService(sloRepository.Object, sliRepository.Object, violationRepository.Object, calculator.Object, alertManager.Object);

        sloRepository.Setup(r => r.GetAllSlosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([compliant, violated]);
        calculator.Setup(c => c.CalculateAsync(compliant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 99.95, RemainingBudgetPercentage = 80.0 });
        calculator.Setup(c => c.CalculateAsync(violated.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new ErrorBudgetDto { ActualPercentage = 98.0, RemainingBudgetPercentage = -5.0 });
        violationRepository.Setup(r => r.GetBySloIdAndTimeRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await sut.GenerateComplianceReportAsync(null, DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, CancellationToken.None);

        result.TotalSlos.Should().Be(2);
        result.CompliantSlos.Should().Be(1);
        result.ViolatedSlos.Should().Be(1);
        result.OverallCompliancePercentage.Should().Be(50.0);
    }

    [Fact]
    public async Task GenerateComplianceReportAsync_WhenNoSlosExist_ShouldReturnPerfectCompliance()
    {
        var sloRepository = new Mock<IServiceLevelObjectiveRepository>();
        var sliRepository = new Mock<IServiceLevelIndicatorRepository>();
        var violationRepository = new Mock<ISloViolationRepository>();
        var calculator = new Mock<IErrorBudgetCalculator>();
        var alertManager = new Mock<IAlertManager>();
        var sut = new SlaMonitoringService(sloRepository.Object, sliRepository.Object, violationRepository.Object, calculator.Object, alertManager.Object);

        sloRepository.Setup(r => r.GetAllSlosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await sut.GenerateComplianceReportAsync(null, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow, CancellationToken.None);

        result.TotalSlos.Should().Be(0);
        result.OverallCompliancePercentage.Should().Be(100.0);
    }
}

internal static class SlaCoverageGapTestData
{
    public static ServiceLevelObjective CreateSlo(Guid tenantId, string name = "Test SLO")
    {
        var slo = new ServiceLevelObjective
        {
            Id = Guid.NewGuid(),
            Name = name,
            ServiceName = "test-service",
            TargetPercentage = 99.9,
            TimeWindowDays = 30,
            ErrorBudgetPercentage = 0.1,
            AlertThresholdPercentage = 50.0,
            IsEnabled = true,
            Status = SloStatus.Active
        };
        slo.SetTenantId(tenantId);

        return slo;
    }

    public static SloViolation CreateViolation(Guid tenantId, Guid? sloId = null)
    {
        var violation = new SloViolation
        {
            Id = Guid.NewGuid(),
            ServiceLevelObjectiveId = sloId ?? Guid.NewGuid(),
            StartedAt = DateTimeOffset.UtcNow.AddHours(-1),
            ActualValue = 98.0,
            TargetValue = 99.9,
            Severity = ViolationSeverity.High,
            Description = "violation"
        };
        violation.SetTenantId(tenantId);

        return violation;
    }

    public static void SetupSuccessfulNotification(Mock<INotificationService> notificationService)
    {
        notificationService.Setup(service => service.SendAsync(
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
}
