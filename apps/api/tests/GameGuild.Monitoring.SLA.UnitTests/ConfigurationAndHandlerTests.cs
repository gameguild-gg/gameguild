using FluentAssertions;
using GameGuild.Monitoring.SLA;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Monitoring.SLA.Tests;

public class ConfigurationAndHandlerTests
{
    // --- EF Core Configurations ---
    [Fact]
    public void ServiceLevelIndicatorConfiguration_Configures()
    {
        var mb = new ModelBuilder(new ConventionSet());
        new ServiceLevelIndicatorConfiguration().Configure(mb.Entity<ServiceLevelIndicator>());
    }

    [Fact]
    public void ServiceLevelObjectiveConfiguration_Configures()
    {
        var mb = new ModelBuilder(new ConventionSet());
        new ServiceLevelObjectiveConfiguration().Configure(mb.Entity<ServiceLevelObjective>());
    }

    [Fact]
    public void SloViolationConfiguration_Configures()
    {
        var mb = new ModelBuilder(new ConventionSet());
        new SloViolationConfiguration().Configure(mb.Entity<SloViolation>());
    }

    // --- Handler Constructors ---
    [Fact] public void CreateSloCommandHandler_Ctor() =>
        new CreateSloCommandHandler(Mock.Of<IServiceLevelObjectiveRepository>()).Should().NotBeNull();

    [Fact] public void DeleteSloCommandHandler_Ctor() =>
        new DeleteSloCommandHandler(Mock.Of<IServiceLevelObjectiveRepository>()).Should().NotBeNull();

    [Fact] public void UpdateSloCommandHandler_Ctor() =>
        new UpdateSloCommandHandler(Mock.Of<IServiceLevelObjectiveRepository>()).Should().NotBeNull();

    [Fact] public void RecordSliMetricCommandHandler_Ctor() =>
        new RecordSliMetricCommandHandler(Mock.Of<IServiceLevelObjectiveRepository>(),
            Mock.Of<IServiceLevelIndicatorRepository>(), Mock.Of<ISlaMonitoringService>()).Should().NotBeNull();

    [Fact] public void ResolveSloViolationCommandHandler_Ctor() =>
        new ResolveSloViolationCommandHandler(Mock.Of<ISloViolationRepository>()).Should().NotBeNull();

    [Fact] public void GetErrorBudgetQueryHandler_Ctor() =>
        new GetErrorBudgetQueryHandler(Mock.Of<IServiceLevelObjectiveRepository>(),
            Mock.Of<IErrorBudgetCalculator>()).Should().NotBeNull();

    [Fact] public void GetSloByIdQueryHandler_Ctor() =>
        new GetSloByIdQueryHandler(Mock.Of<IServiceLevelObjectiveRepository>()).Should().NotBeNull();

    [Fact] public void GetSloComplianceQueryHandler_Ctor() =>
        new GetSloComplianceQueryHandler(Mock.Of<ISlaMonitoringService>(),
            Mock.Of<IServiceLevelObjectiveRepository>()).Should().NotBeNull();

    [Fact] public void GetSlosQueryHandler_Ctor() =>
        new GetSlosQueryHandler(Mock.Of<IServiceLevelObjectiveRepository>()).Should().NotBeNull();

    [Fact] public void GetSloViolationsQueryHandler_Ctor() =>
        new GetSloViolationsQueryHandler(Mock.Of<ISloViolationRepository>(),
            Mock.Of<IServiceLevelObjectiveRepository>()).Should().NotBeNull();
}
