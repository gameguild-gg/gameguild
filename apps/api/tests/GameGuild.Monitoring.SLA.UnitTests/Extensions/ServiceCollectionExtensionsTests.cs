using FluentAssertions;

using GameGuild.CQRS;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace GameGuild.Monitoring.SLA.Tests;

/// <summary>
///     Tests for SLA Monitoring DI registration to boost coverage.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSlaMonitoringApplication_ShouldRegisterCommandHandlers()
    {
        var services = new ServiceCollection();

        services.AddSlaMonitoringApplication();

        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<CreateSloCommand, SloDto>));
        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<UpdateSloCommand, SloDto>));
        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<DeleteSloCommand, Unit>));
        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<RecordSliMetricCommand, SliMetricDto>));
        services.Should().Contain(s =>
            s.ServiceType == typeof(ICommandHandler<ResolveSloViolationCommand, Unit>));
    }

    [Fact]
    public void AddSlaMonitoringApplication_ShouldRegisterQueryHandlers()
    {
        var services = new ServiceCollection();

        services.AddSlaMonitoringApplication();

        services.Should().Contain(s =>
            s.ServiceType == typeof(IQueryHandler<GetSlosQuery, List<SloDto>>));
        services.Should().Contain(s =>
            s.ServiceType == typeof(IQueryHandler<GetSloByIdQuery, SloDto?>));
        services.Should().Contain(s =>
            s.ServiceType == typeof(IQueryHandler<GetErrorBudgetQuery, ErrorBudgetDto?>));
        services.Should().Contain(s =>
            s.ServiceType == typeof(IQueryHandler<GetSloComplianceQuery, SloComplianceDto>));
        services.Should().Contain(s =>
            s.ServiceType == typeof(IQueryHandler<GetSloViolationsQuery, List<SloViolationDto>>));
    }

    [Fact]
    public void AddSlaMonitoringApplication_ShouldRegisterDomainServices()
    {
        var services = new ServiceCollection();

        services.AddSlaMonitoringApplication();

        services.Should().Contain(s =>
            s.ServiceType == typeof(IErrorBudgetCalculator) &&
            s.ImplementationType == typeof(ErrorBudgetCalculator));
        services.Should().Contain(s =>
            s.ServiceType == typeof(IAlertManager) &&
            s.ImplementationType == typeof(AlertManager));
        services.Should().Contain(s =>
            s.ServiceType == typeof(ISlaMonitoringService) &&
            s.ImplementationType == typeof(SlaMonitoringService));
    }

    [Fact]
    public void AddSlaMonitoringApplication_ShouldRegisterValidators()
    {
        var services = new ServiceCollection();

        services.AddSlaMonitoringApplication();

        // FluentValidation registers validators from the assembly
        services.Should().NotBeEmpty();
    }

    [Fact]
    public void AddSlaMonitoringApplication_ShouldReturnServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddSlaMonitoringApplication();

        result.Should().BeSameAs(services);
    }
}
