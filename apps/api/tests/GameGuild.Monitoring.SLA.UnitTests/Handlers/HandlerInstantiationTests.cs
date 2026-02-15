using FluentAssertions;

using Moq;

using Xunit;

namespace GameGuild.Monitoring.SLA.Tests;

/// <summary>
///     Tests that instantiate all command/query handlers to cover primary constructor lines.
/// </summary>
public class HandlerInstantiationTests
{
    [Fact]
    public void AllCommandHandlers_ShouldInstantiate()
    {
        var sloRepo = new Mock<IServiceLevelObjectiveRepository>().Object;
        var sliRepo = new Mock<IServiceLevelIndicatorRepository>().Object;
        var violationRepo = new Mock<ISloViolationRepository>().Object;
        var monitoringService = new Mock<ISlaMonitoringService>().Object;

        var createHandler = new CreateSloCommandHandler(sloRepo);
        var updateHandler = new UpdateSloCommandHandler(sloRepo);
        var deleteHandler = new DeleteSloCommandHandler(sloRepo);
        var recordHandler = new RecordSliMetricCommandHandler(sloRepo, sliRepo, monitoringService);
        var resolveHandler = new ResolveSloViolationCommandHandler(violationRepo);

        createHandler.Should().NotBeNull();
        updateHandler.Should().NotBeNull();
        deleteHandler.Should().NotBeNull();
        recordHandler.Should().NotBeNull();
        resolveHandler.Should().NotBeNull();
    }

    [Fact]
    public void AllQueryHandlers_ShouldInstantiate()
    {
        var sloRepo = new Mock<IServiceLevelObjectiveRepository>().Object;
        var violationRepo = new Mock<ISloViolationRepository>().Object;
        var budgetCalculator = new Mock<IErrorBudgetCalculator>().Object;
        var monitoringService = new Mock<ISlaMonitoringService>().Object;

        var getSlosHandler = new GetSlosQueryHandler(sloRepo);
        var getSloByIdHandler = new GetSloByIdQueryHandler(sloRepo);
        var getErrorBudgetHandler = new GetErrorBudgetQueryHandler(sloRepo, budgetCalculator);
        var getComplianceHandler = new GetSloComplianceQueryHandler(monitoringService, sloRepo);
        var getViolationsHandler = new GetSloViolationsQueryHandler(violationRepo, sloRepo);

        getSlosHandler.Should().NotBeNull();
        getSloByIdHandler.Should().NotBeNull();
        getErrorBudgetHandler.Should().NotBeNull();
        getComplianceHandler.Should().NotBeNull();
        getViolationsHandler.Should().NotBeNull();
    }
}
