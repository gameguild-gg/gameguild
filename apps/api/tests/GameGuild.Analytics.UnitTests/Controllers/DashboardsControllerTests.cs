using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Analytics.UnitTests.Controllers;

public sealed class DashboardsControllerTests
{
    [Fact]
    public async Task List_ShouldSendQueryAndReturnDashboards()
    {
        var sender = new Mock<ISender>();
        var tenantId = Guid.NewGuid();
        var expected = new List<DashboardDto>
        {
            new(Guid.NewGuid(), tenantId, "Ops", "ops", null, true, [], DateTime.UtcNow, DateTime.UtcNow)
        };
        sender
            .Setup(service => service.Send(It.Is<GetDashboardsQuery>(query => query.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new DashboardsController(sender.Object);

        var result = await controller.List(tenantId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Create_ShouldSendCommandAndReturnCreatedRoute()
    {
        var sender = new Mock<ISender>();
        var request = new CreateDashboardRequest("Ops", "ops");
        var expected = new DashboardDto(Guid.NewGuid(), null, "Ops", "ops", null, false, [], DateTime.UtcNow, DateTime.UtcNow);
        sender
            .Setup(service => service.Send(It.Is<CreateDashboardCommand>(command => command.Request == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new DashboardsController(sender.Object);

        var result = await controller.Create(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        created.RouteName.Should().Be("GetAnalyticsDashboardById");
        created.Value.Should().Be(expected);
    }
}
