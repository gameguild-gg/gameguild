using FluentAssertions;
using GameGuild.API.Dashboard;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Authorization;
using GameGuild.TestingLab;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GameGuild.API.UnitTests.Dashboard;

public sealed class DashboardContextsControllerTests
{
    [Fact]
    public async Task Get_ReturnsUnauthorizedForAnonymousActor()
    {
        var controller = Controller(ActorContext.Anonymous, []);

        var result = await controller.Get(CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Get_ReturnsWorkspaceWithoutOperationsForRegularMember()
    {
        var controller = Controller(Actor("Member"), []);

        var result = await controller.Get(CancellationToken.None);

        var response = result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<DashboardContextsResponse>().Subject;
        response.Capabilities.Should().BeEmpty();
        response.Contexts.Select(context => context.Type).Should().Equal(DashboardContextTypes.Workspace);
        response.Counts.Should().Be(new DashboardWorkspaceCounts(0, 0, 0, 0));
        response.Navigation.Should().ContainSingle(group => group.Label == "Overview");
        response.Navigation.SelectMany(group => group.Items).Should().NotContain(item =>
            item.Title == "Testing Lab" || item.Title == "Launch Pad");
    }

    [Fact]
    public async Task Get_AddsOperationsOnlyWhenActorHasManagementCapability()
    {
        var controller = Controller(
            Actor("Member"),
            [new TestingLabUserPermission
            {
                Action = TestingLabActions.Create,
                ResourceType = TestingLabResourceTypes.Event,
            }]);

        var result = await controller.Get(CancellationToken.None);

        var response = result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<DashboardContextsResponse>().Subject;
        response.Capabilities.Should().Contain(DashboardCapabilities.TestingLabManageEvents);
        response.Contexts.Select(context => context.Type).Should().Equal(
            DashboardContextTypes.Workspace,
            DashboardContextTypes.Operations);
        var testingLab = response.Navigation
            .Single(group => group.Label == "Community Management")
            .Items.Single(item => item.Title == "Testing Lab");
        testingLab.Children.Select(item => item.Title).Should().Equal("Overview", "Events");
        response.Navigation.SelectMany(group => group.Items).Should().NotContain(item => item.Title == "Launch Pad");
    }

    [Fact]
    public async Task Get_RejectsEvenSystemAdminWhenSelectedTenantMembershipIsInactive()
    {
        var controller = Controller(Actor("SystemAdmin"), [], hasMembership: false);

        var result = await controller.Get(CancellationToken.None);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    private static DashboardContextsController Controller(
        ActorContext actor,
        IReadOnlyList<TestingLabUserPermission> testingPermissions,
        bool hasMembership = true)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(candidate => candidate.ActorContext).Returns(actor);

        var testingLab = new Mock<ITestingLabPermissionService>();
        testingLab
            .Setup(service => service.GetUserPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(testingPermissions);

        var workspace = new Mock<IDashboardWorkspaceContextService>();
        workspace.Setup(service => service.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardWorkspaceContextData([], new DashboardWorkspaceCounts(0, 0, 0, 0)));

        var membership = new Mock<ITenantMembershipChecker>();
        membership.Setup(service => service.IsUserMemberOfTenantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasMembership);

        return new DashboardContextsController(accessor.Object, testingLab.Object, workspace.Object, membership.Object);
    }

    private static ActorContext Actor(string role) => new()
    {
        ActorKind = ActorKind.User,
        SubjectId = Guid.NewGuid().ToString(),
        TenantId = Guid.NewGuid(),
        Roles = new HashSet<string> { role },
        Permissions = new HashSet<string>(),
        TypedAttributes = ActorAttributes.Empty,
        AuthScheme = "Bearer",
        IsAuthenticated = true,
    };
}
