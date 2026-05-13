using FluentAssertions;

using Moq;

using GameGuild.CQRS;

using Xunit;

using static GameGuild.Monitoring.SLA.UnitTests.Handlers.SlaHandlerTestData;

namespace GameGuild.Monitoring.SLA.UnitTests.Handlers;

public class CreateSloCommandHandlerTests
{
    private readonly Mock<IServiceLevelObjectiveRepository> _repository = new();
    private readonly CreateSloCommandHandler _sut;

    public CreateSloCommandHandlerTests()
    {
        _sut = new CreateSloCommandHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ShouldThrowInvalidOperationException()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), "API", null, "svc", 99.9, 30, 0.1, 50);
        _repository.Setup(repository => repository.ExistsByNameAsync(command.Name, command.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = () => _sut.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldAddSloAndReturnDto()
    {
        var command = new CreateSloCommand(Guid.NewGuid(), "API", "desc", "svc", 99.9, 30, 0.1, 50);
        ServiceLevelObjective? captured = null;

        _repository.Setup(repository => repository.ExistsByNameAsync(command.Name, command.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(repository => repository.AddAsync(It.IsAny<ServiceLevelObjective>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceLevelObjective, CancellationToken>((slo, _) => captured = slo)
            .ReturnsAsync((ServiceLevelObjective slo, CancellationToken _) => slo);

        var result = await _sut.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(command.TenantId);
        captured.Name.Should().Be(command.Name);
        result.TenantId.Should().Be(command.TenantId);
        result.Name.Should().Be(command.Name);
        result.IsEnabled.Should().BeTrue();
        result.Status.Should().Be(SloStatus.Active);
    }
}

public class UpdateSloCommandHandlerTests
{
    private readonly Mock<IServiceLevelObjectiveRepository> _repository = new();
    private readonly UpdateSloCommandHandler _sut;

    public UpdateSloCommandHandlerTests()
    {
        _sut = new UpdateSloCommandHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_WhenSloMissing_ShouldThrowInvalidOperationException()
    {
        _repository.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?) null);

        var action = () => _sut.Handle(new UpdateSloCommand(Guid.NewGuid(), Guid.NewGuid(), "Name", null, "svc", 99.9, 30, 0.1, 50, true), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenTenantMismatch_ShouldThrowUnauthorizedAccessException()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: true);
        var command = new UpdateSloCommand(slo.Id, Guid.NewGuid(), "Name", null, "svc", 99.9, 30, 0.1, 50, true);
        _repository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var action = () => _sut.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenEnableRequested_ShouldEnableAndUpdate()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId, isEnabled: false);
        var command = new UpdateSloCommand(slo.Id, tenantId, "Enabled", "desc", "svc", 99.9, 30, 0.1, 50, true);
        _repository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var result = await _sut.Handle(command, CancellationToken.None);

        _repository.Verify(repository => repository.UpdateAsync(slo, It.IsAny<CancellationToken>()), Times.Once);
        result.IsEnabled.Should().BeTrue();
        result.Name.Should().Be("Enabled");
        slo.Status.Should().Be(SloStatus.Active);
    }

    [Fact]
    public async Task Handle_WhenDisableRequested_ShouldDisableAndUpdate()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId, isEnabled: true);
        var command = new UpdateSloCommand(slo.Id, tenantId, "Disabled", "desc", "svc", 99.9, 30, 0.1, 50, false);
        _repository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsEnabled.Should().BeFalse();
        slo.Status.Should().Be(SloStatus.Disabled);
    }

    [Fact]
    public async Task Handle_WhenStateUnchanged_ShouldStillUpdateMappedFields()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId, isEnabled: true);
        var command = new UpdateSloCommand(slo.Id, tenantId, "Renamed", "desc", "svc", 99.5, 7, 0.5, 40, true);
        _repository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Name.Should().Be("Renamed");
        result.TargetPercentage.Should().Be(99.5);
        result.TimeWindowDays.Should().Be(7);
        result.AlertThresholdPercentage.Should().Be(40);
    }
}

public class DeleteSloCommandHandlerTests
{
    private readonly Mock<IServiceLevelObjectiveRepository> _repository = new();
    private readonly DeleteSloCommandHandler _sut;

    public DeleteSloCommandHandlerTests()
    {
        _sut = new DeleteSloCommandHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_WhenSloMissing_ShouldThrowInvalidOperationException()
    {
        _repository.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?) null);

        var action = () => _sut.Handle(new DeleteSloCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenTenantMismatch_ShouldThrowUnauthorizedAccessException()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: true);
        var command = new DeleteSloCommand(slo.Id, Guid.NewGuid());
        _repository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var action = () => _sut.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenAuthorized_ShouldDeleteAndReturnUnit()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId, isEnabled: true);
        var command = new DeleteSloCommand(slo.Id, tenantId);
        _repository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _repository.Verify(repository => repository.DeleteAsync(command.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class RecordSliMetricCommandHandlerTests
{
    private readonly Mock<IServiceLevelObjectiveRepository> _sloRepository = new();
    private readonly Mock<IServiceLevelIndicatorRepository> _sliRepository = new();
    private readonly Mock<ISlaMonitoringService> _monitoringService = new();
    private readonly RecordSliMetricCommandHandler _sut;

    public RecordSliMetricCommandHandlerTests()
    {
        _sut = new RecordSliMetricCommandHandler(_sloRepository.Object, _sliRepository.Object, _monitoringService.Object);
    }

    [Fact]
    public async Task Handle_WhenSloMissing_ShouldThrowInvalidOperationException()
    {
        _sloRepository.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?) null);

        var action = () => _sut.Handle(new RecordSliMetricCommand(Guid.NewGuid(), Guid.NewGuid(), true, 99.9), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenTenantMismatch_ShouldThrowUnauthorizedAccessException()
    {
        var slo = CreateSlo(Guid.NewGuid(), isEnabled: true);
        var command = new RecordSliMetricCommand(Guid.NewGuid(), slo.Id, true, 99.9);
        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var action = () => _sut.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenSuccessful_ShouldPersistSuccessMetric()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId, isEnabled: true);
        var command = new RecordSliMetricCommand(tenantId, slo.Id, true, 99.9, 42, 200, "/health", "{}");
        ServiceLevelIndicator? captured = null;

        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _sliRepository.Setup(repository => repository.AddAsync(It.IsAny<ServiceLevelIndicator>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceLevelIndicator, CancellationToken>((sli, _) => captured = sli)
            .ReturnsAsync((ServiceLevelIndicator sli, CancellationToken _) => sli);
        _monitoringService.Setup(service => service.EvaluateSloAsync(slo.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
        captured.IsSuccessful.Should().BeTrue();
        captured.ErrorMessage.Should().BeNull();
        captured.Metadata.Should().Be("{}");
        result.IsSuccessful.Should().BeTrue();
        result.Endpoint.Should().Be("/health");
    }

    [Fact]
    public async Task Handle_WhenFailureWithoutErrorMessage_ShouldUseFallbackMessage()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId, isEnabled: true);
        var command = new RecordSliMetricCommand(tenantId, slo.Id, false, 0.0, ErrorMessage: null);
        ServiceLevelIndicator? captured = null;

        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _sliRepository.Setup(repository => repository.AddAsync(It.IsAny<ServiceLevelIndicator>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceLevelIndicator, CancellationToken>((sli, _) => captured = sli)
            .ReturnsAsync((ServiceLevelIndicator sli, CancellationToken _) => sli);

        var result = await _sut.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.IsSuccessful.Should().BeFalse();
        captured.ErrorMessage.Should().Be("Unknown error");
        result.ErrorMessage.Should().Be("Unknown error");
    }
}

public class ResolveSloViolationCommandHandlerTests
{
    private readonly Mock<ISloViolationRepository> _repository = new();
    private readonly ResolveSloViolationCommandHandler _sut;

    private sealed record ConcreteResolveSloViolationCommand(Guid ViolationId, Guid TenantId, string? ResolutionNotes = null)
        : ResolveSloViolationCommand(ViolationId, TenantId, ResolutionNotes);

    public ResolveSloViolationCommandHandlerTests()
    {
        _sut = new ResolveSloViolationCommandHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_WhenViolationMissing_ShouldThrowInvalidOperationException()
    {
        _repository.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SloViolation?) null);

        var action = () => _sut.Handle(new ConcreteResolveSloViolationCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenTenantMismatch_ShouldThrowUnauthorizedAccessException()
    {
        var violation = CreateViolation(Guid.NewGuid());
        var command = new ConcreteResolveSloViolationCommand(violation.Id, Guid.NewGuid());
        _repository.Setup(repository => repository.GetByIdAsync(violation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(violation);

        var action = () => _sut.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenAlreadyResolved_ShouldThrowInvalidOperationException()
    {
        var tenantId = Guid.NewGuid();
        var violation = CreateViolation(tenantId, resolved: true);
        var command = new ConcreteResolveSloViolationCommand(violation.Id, tenantId);
        _repository.Setup(repository => repository.GetByIdAsync(violation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(violation);

        var action = () => _sut.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenNotesProvided_ShouldResolveAndStoreNotes()
    {
        var tenantId = Guid.NewGuid();
        var violation = CreateViolation(tenantId);
        var command = new ConcreteResolveSloViolationCommand(violation.Id, tenantId, "resolved");
        _repository.Setup(repository => repository.GetByIdAsync(violation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(violation);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        violation.EndedAt.Should().NotBeNull();
        violation.Notes.Should().Be("resolved");
        _repository.Verify(repository => repository.UpdateAsync(violation, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotesBlank_ShouldResolveWithoutOverwritingNotes()
    {
        var tenantId = Guid.NewGuid();
        var violation = CreateViolation(tenantId);
        var command = new ConcreteResolveSloViolationCommand(violation.Id, tenantId, "   ");
        _repository.Setup(repository => repository.GetByIdAsync(violation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(violation);

        await _sut.Handle(command, CancellationToken.None);

        violation.EndedAt.Should().NotBeNull();
        violation.Notes.Should().BeNull();
    }
}

public class GetSlosQueryHandlerTests
{
    private readonly Mock<IServiceLevelObjectiveRepository> _repository = new();
    private readonly GetSlosQueryHandler _sut;

    public GetSlosQueryHandlerTests()
    {
        _sut = new GetSlosQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_WhenNoFilters_ShouldReturnAllTenantSlos()
    {
        var tenantId = Guid.NewGuid();
        var slos = new List<ServiceLevelObjective>
        {
            CreateSlo(tenantId, "API", "api", true),
            CreateSlo(tenantId, "Billing", "billing", false)
        };

        _repository.Setup(repository => repository.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(slos);

        var result = await _sut.Handle(new GetSlosQuery(tenantId), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldApplyFiltersAndPagination()
    {
        var tenantId = Guid.NewGuid();
        var slos = new List<ServiceLevelObjective>
        {
            CreateSlo(tenantId, "API", "api", true),
            CreateSlo(tenantId, "API-2", "api", true),
            CreateSlo(tenantId, "Billing", "billing", false)
        };

        _repository.Setup(repository => repository.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(slos);

        var result = await _sut.Handle(new GetSlosQuery(tenantId, "api", true, 1, 1), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Name.Should().Be("API-2");
    }
}

public class GetSloByIdQueryHandlerTests
{
    private readonly Mock<IServiceLevelObjectiveRepository> _repository = new();
    private readonly GetSloByIdQueryHandler _sut;

    public GetSloByIdQueryHandlerTests()
    {
        _sut = new GetSloByIdQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_WhenSloMissing_ShouldReturnNull()
    {
        _repository.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?) null);

        var result = await _sut.Handle(new GetSloByIdQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTenantMismatch_ShouldReturnNull()
    {
        var slo = CreateSlo(Guid.NewGuid(), "API", "api", true);
        _repository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var result = await _sut.Handle(new GetSloByIdQuery(slo.Id, Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenAuthorized_ShouldReturnDto()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId, "API", "api", true);
        _repository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var result = await _sut.Handle(new GetSloByIdQuery(slo.Id, tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("API");
    }
}

public class GetErrorBudgetQueryHandlerTests
{
    private readonly Mock<IServiceLevelObjectiveRepository> _sloRepository = new();
    private readonly Mock<IErrorBudgetCalculator> _calculator = new();
    private readonly GetErrorBudgetQueryHandler _sut;

    public GetErrorBudgetQueryHandlerTests()
    {
        _sut = new GetErrorBudgetQueryHandler(_sloRepository.Object, _calculator.Object);
    }

    [Fact]
    public async Task Handle_WhenSloMissing_ShouldReturnNull()
    {
        _sloRepository.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?) null);

        var result = await _sut.Handle(new GetErrorBudgetQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTenantMismatch_ShouldReturnNull()
    {
        var slo = CreateSlo(Guid.NewGuid(), "API", "api", true);
        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var result = await _sut.Handle(new GetErrorBudgetQuery(slo.Id, Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenAuthorized_ShouldReturnCalculatorResult()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId, "API", "api", true);
        var expected = new ErrorBudgetDto { ActualPercentage = 99.9 };
        _sloRepository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _calculator.Setup(calculator => calculator.CalculateAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _sut.Handle(new GetErrorBudgetQuery(slo.Id, tenantId), CancellationToken.None);

        result.Should().Be(expected);
    }
}

public class GetSloComplianceQueryHandlerTests
{
    private readonly Mock<ISlaMonitoringService> _monitoringService = new();
    private readonly Mock<IServiceLevelObjectiveRepository> _repository = new();
    private readonly GetSloComplianceQueryHandler _sut;

    public GetSloComplianceQueryHandlerTests()
    {
        _sut = new GetSloComplianceQueryHandler(_monitoringService.Object, _repository.Object);
    }

    [Fact]
    public async Task Handle_WhenSloMissing_ShouldThrowInvalidOperationException()
    {
        _repository.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?) null);

        var action = () => _sut.Handle(new GetSloComplianceQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenTenantMismatch_ShouldThrowUnauthorizedAccessException()
    {
        var slo = CreateSlo(Guid.NewGuid(), "API", "api", true);
        _repository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);

        var action = () => _sut.Handle(new GetSloComplianceQuery(slo.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenDatesMissing_ShouldUseDefaultWindow()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId, "API", "api", true);
        DateTimeOffset? capturedStart = null;
        DateTimeOffset? capturedEnd = null;
        var expected = new SloComplianceDto();

        _repository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _monitoringService.Setup(service => service.GetComplianceAsync(slo.Id, It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, DateTimeOffset?, DateTimeOffset?, CancellationToken>((_, start, end, _) =>
            {
                capturedStart = start;
                capturedEnd = end;
            })
            .ReturnsAsync(expected);

        var result = await _sut.Handle(new GetSloComplianceQuery(slo.Id, tenantId), CancellationToken.None);

        result.Should().Be(expected);
        capturedStart.Should().NotBeNull();
        capturedEnd.Should().NotBeNull();
        capturedEnd.Should().BeOnOrAfter(capturedStart!.Value);
    }

    [Fact]
    public async Task Handle_WhenDatesProvided_ShouldPassExplicitDates()
    {
        var tenantId = Guid.NewGuid();
        var slo = CreateSlo(tenantId, "API", "api", true);
        var start = DateTimeOffset.UtcNow.AddDays(-7);
        var end = DateTimeOffset.UtcNow.AddDays(-1);
        var expected = new SloComplianceDto();

        _repository.Setup(repository => repository.GetByIdAsync(slo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slo);
        _monitoringService.Setup(service => service.GetComplianceAsync(slo.Id, start, end, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _sut.Handle(new GetSloComplianceQuery(slo.Id, tenantId, start, end), CancellationToken.None);

        result.Should().Be(expected);
    }
}

public class GetSloViolationsQueryHandlerTests
{
    private readonly Mock<ISloViolationRepository> _violationRepository = new();
    private readonly Mock<IServiceLevelObjectiveRepository> _sloRepository = new();
    private readonly GetSloViolationsQueryHandler _sut;

    public GetSloViolationsQueryHandlerTests()
    {
        _sut = new GetSloViolationsQueryHandler(_violationRepository.Object, _sloRepository.Object);
    }

    [Fact]
    public async Task Handle_WhenNoFiltersProvided_ShouldThrowInvalidOperationException()
    {
        var action = () => _sut.Handle(new GetSloViolationsQuery(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenOnlyUnresolvedBySlo_ShouldUseOngoingViolationsAndFallbackLookupValues()
    {
        var violation = CreateViolation(Guid.NewGuid());
        _violationRepository.Setup(repository => repository.GetOngoingViolationsAsync(violation.ServiceLevelObjectiveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([violation]);
        _sloRepository.Setup(repository => repository.GetByIdAsync(violation.ServiceLevelObjectiveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceLevelObjective?) null);

        var result = await _sut.Handle(new GetSloViolationsQuery(SloId: violation.ServiceLevelObjectiveId, OnlyUnresolved: true), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].SloName.Should().BeEmpty();
        result[0].ServiceName.Should().BeEmpty();
        _violationRepository.Verify(repository => repository.GetOngoingViolationsAsync(violation.ServiceLevelObjectiveId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSloProvidedWithoutOnlyUnresolved_ShouldUseAllViolations()
    {
        var tenantId = Guid.NewGuid();
        var violation = CreateViolation(tenantId);
        var slo = CreateSlo(tenantId, "API", "api", true, id: violation.ServiceLevelObjectiveId);
        _violationRepository.Setup(repository => repository.GetBySloIdAsync(violation.ServiceLevelObjectiveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([violation]);
        _sloRepository.Setup(repository => repository.GetByIdAsync(violation.ServiceLevelObjectiveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slo);

        var result = await _sut.Handle(new GetSloViolationsQuery(SloId: violation.ServiceLevelObjectiveId), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].SloName.Should().Be("API");
        _violationRepository.Verify(repository => repository.GetBySloIdAsync(violation.ServiceLevelObjectiveId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTenantProvided_ShouldApplyDateFiltersAndPagination()
    {
        var tenantId = Guid.NewGuid();
        var sloOneId = Guid.NewGuid();
        var sloTwoId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow.AddHours(-3);
        var end = DateTimeOffset.UtcNow.AddMinutes(-30);
        var violations = new List<SloViolation>
        {
            CreateViolation(tenantId, sloOneId, startedAt: DateTimeOffset.UtcNow.AddHours(-5)),
            CreateViolation(tenantId, sloOneId, startedAt: DateTimeOffset.UtcNow.AddHours(-2)),
            CreateViolation(tenantId, sloTwoId, startedAt: DateTimeOffset.UtcNow.AddHours(-1))
        };

        _violationRepository.Setup(repository => repository.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(violations);
        _sloRepository.Setup(repository => repository.GetByIdAsync(sloOneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSlo(tenantId, "API", "api", true, id: sloOneId));
        _sloRepository.Setup(repository => repository.GetByIdAsync(sloTwoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSlo(tenantId, "Billing", "billing", true, id: sloTwoId));

        var result = await _sut.Handle(new GetSloViolationsQuery(TenantId: tenantId, StartDate: start, EndDate: end, Skip: 1, Take: 1), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].SloName.Should().Be("Billing");
    }
}

internal static class SlaHandlerTestData
{
    public static ServiceLevelObjective CreateSlo(Guid tenantId, string name = "Test SLO", string serviceName = "test-api", bool isEnabled = true, Guid? id = null)
    {
        var slo = new ServiceLevelObjective
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            ServiceName = serviceName,
            TargetPercentage = 99.9,
            TimeWindowDays = 30,
            ErrorBudgetPercentage = 0.1,
            AlertThresholdPercentage = 50.0,
            IsEnabled = isEnabled,
            Status = isEnabled ? SloStatus.Active : SloStatus.Disabled
        };
        slo.SetTenantId(tenantId);

        return slo;
    }

    public static SloViolation CreateViolation(Guid tenantId, Guid? sloId = null, bool resolved = false, DateTimeOffset? startedAt = null)
    {
        var violation = new SloViolation
        {
            Id = Guid.NewGuid(),
            ServiceLevelObjectiveId = sloId ?? Guid.NewGuid(),
            StartedAt = startedAt ?? DateTimeOffset.UtcNow.AddHours(-1),
            EndedAt = resolved ? DateTimeOffset.UtcNow.AddMinutes(-5) : null,
            ActualValue = 98.0,
            TargetValue = 99.9,
            Severity = ViolationSeverity.High,
            Description = "violation"
        };
        violation.SetTenantId(tenantId);

        return violation;
    }
}
