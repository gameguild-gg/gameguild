using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using GameGuild.CQRS;

using Xunit;

namespace GameGuild.Monitoring.SLA.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSlaMonitoringApplication_ShouldRegisterCommandHandlers()
    {
        var services = new ServiceCollection();

        services.AddSlaMonitoringApplication();

        services.Should().Contain(s => s.ServiceType == typeof(ICommandHandler<CreateSloCommand, SloDto>));
        services.Should().Contain(s => s.ServiceType == typeof(ICommandHandler<UpdateSloCommand, SloDto>));
        services.Should().Contain(s => s.ServiceType == typeof(ICommandHandler<DeleteSloCommand, Unit>));
        services.Should().Contain(s => s.ServiceType == typeof(ICommandHandler<RecordSliMetricCommand, SliMetricDto>));
        services.Should().Contain(s => s.ServiceType == typeof(ICommandHandler<ResolveSloViolationCommand, Unit>));
    }

    [Fact]
    public void AddSlaMonitoringApplication_ShouldRegisterQueryHandlers()
    {
        var services = new ServiceCollection();

        services.AddSlaMonitoringApplication();

        services.Should().Contain(s => s.ServiceType == typeof(IQueryHandler<GetSlosQuery, List<SloDto>>));
        services.Should().Contain(s => s.ServiceType == typeof(IQueryHandler<GetSloByIdQuery, SloDto?>));
        services.Should().Contain(s => s.ServiceType == typeof(IQueryHandler<GetErrorBudgetQuery, ErrorBudgetDto?>));
        services.Should().Contain(s => s.ServiceType == typeof(IQueryHandler<GetSloComplianceQuery, SloComplianceDto>));
        services.Should().Contain(s => s.ServiceType == typeof(IQueryHandler<GetSloViolationsQuery, List<SloViolationDto>>));
    }

    [Fact]
    public void AddSlaMonitoringApplication_ShouldRegisterDomainServices()
    {
        var services = new ServiceCollection();

        services.AddSlaMonitoringApplication();

        services.Should().Contain(s => s.ServiceType == typeof(IErrorBudgetCalculator) && s.ImplementationType == typeof(ErrorBudgetCalculator));
        services.Should().Contain(s => s.ServiceType == typeof(IAlertManager) && s.ImplementationType == typeof(AlertManager));
        services.Should().Contain(s => s.ServiceType == typeof(ISlaMonitoringService) && s.ImplementationType == typeof(SlaMonitoringService));
    }

    [Fact]
    public void AddSlaMonitoringApplication_ShouldRegisterPersistenceRepositories()
    {
        var services = new ServiceCollection();

        services.AddSlaMonitoringApplication();

        services.Should().Contain(s => s.ServiceType == typeof(IServiceLevelObjectiveRepository) && s.ImplementationType == typeof(ServiceLevelObjectiveRepository));
        services.Should().Contain(s => s.ServiceType == typeof(IServiceLevelIndicatorRepository) && s.ImplementationType == typeof(ServiceLevelIndicatorRepository));
        services.Should().Contain(s => s.ServiceType == typeof(ISloViolationRepository) && s.ImplementationType == typeof(SloViolationRepository));
    }

    [Fact]
    public void AddSlaMonitoringApplication_ShouldReturnServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddSlaMonitoringApplication();

        result.Should().BeSameAs(services);
    }
}
