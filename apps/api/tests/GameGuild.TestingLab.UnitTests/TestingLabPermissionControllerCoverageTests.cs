using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabPermissionControllerCoverageTests
{
    [Fact]
    public void CurrentUserId_FallsBackToEmptyForAnAnonymousActor()
    {
        var controller = Controller(
            ActorContext.Anonymous,
            Mock.Of<ITestingLabPermissionService>(),
            Mock.Of<ISender>());
        var method = typeof(TestingLabPermissionController).GetMethod(
            "GetCurrentUserId",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        method.Invoke(controller, null).Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task RoleTemplateReadsAndWrites_MapEveryPermissionFlag()
    {
        var mappedTemplate = new RoleTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Complete",
            Description = "All permissions",
            IsSystemRole = true,
            PermissionTemplates = EveryPermissionCombination()
        };
        var emptyTemplate = new RoleTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Empty",
            PermissionTemplates = null
        };
        var service = new Mock<ITestingLabPermissionService>();
        service.Setup(value => value.GetRoleTemplatesAsync())
            .ReturnsAsync([mappedTemplate, emptyTemplate]);
        var sender = new Mock<ISender>();
        sender.Setup(value => value.Send(
                It.IsAny<CreateTestingLabRoleTemplateEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mappedTemplate);
        sender.Setup(value => value.Send(
                It.IsAny<UpdateTestingLabRoleTemplateEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mappedTemplate);
        sender.Setup(value => value.Send(
                It.IsAny<DeleteTestingLabRoleTemplateEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = Controller(Actor(Guid.NewGuid()), service.Object, sender.Object);

        var listed = await controller.GetRoleTemplates();
        var listedValue = listed.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeAssignableTo<List<TestingLabRoleTemplate>>().Subject;
        listedValue.Should().HaveCount(2);
        AssertAllFlags(listedValue[0].Permissions, true);
        AssertAllFlags(listedValue[1].Permissions, false);

        var createdAll = await controller.CreateTestingLabRoleTemplate(new CreateTestingLabRoleRequest
        {
            Name = "All",
            Description = "All",
            Permissions = Permissions(true)
        });
        AssertAllFlags(createdAll.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<TestingLabRoleTemplate>().Subject.Permissions, true);

        await controller.CreateTestingLabRoleTemplate(new CreateTestingLabRoleRequest
        {
            Name = "None",
            Description = "None",
            Permissions = Permissions(false)
        });
        var updated = await controller.UpdateTestingLabRoleTemplate("Complete", new UpdateTestingLabRoleRequest
        {
            Name = "Updated",
            Description = "Updated",
            Permissions = Permissions(true)
        });
        updated.Result.Should().BeOfType<OkObjectResult>();
        (await controller.DeleteTestingLabRoleTemplate("Complete")).Should().BeOfType<NoContentResult>();
        (await controller.DeleteTestingLabRoleTemplateByName("Complete")).Should().BeOfType<NoContentResult>();

        sender.Verify(value => value.Send(
            It.Is<CreateTestingLabRoleTemplateEndpointCommand>(command => command.Permissions.Count == 28),
            It.IsAny<CancellationToken>()), Times.Once);
        sender.Verify(value => value.Send(
            It.Is<CreateTestingLabRoleTemplateEndpointCommand>(command => command.Permissions.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RoleTemplateMutations_MapMissingAndConflictOutcomes()
    {
        var service = Mock.Of<ITestingLabPermissionService>();
        var sender = new Mock<ISender>();
        sender.Setup(value => value.Send(
                It.IsAny<CreateTestingLabRoleTemplateEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("conflict"));
        sender.Setup(value => value.Send(
                It.IsAny<UpdateTestingLabRoleTemplateEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleTemplate?)null);
        sender.Setup(value => value.Send(
                It.IsAny<DeleteTestingLabRoleTemplateEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = Controller(Actor(Guid.NewGuid()), service, sender.Object);

        (await controller.CreateTestingLabRoleTemplate(new CreateTestingLabRoleRequest()))
            .Result.Should().BeOfType<ConflictObjectResult>();
        (await controller.UpdateTestingLabRoleTemplate("missing", new UpdateTestingLabRoleRequest()))
            .Result.Should().BeOfType<NotFoundObjectResult>();
        (await controller.DeleteTestingLabRoleTemplate("missing")).Should().BeOfType<NotFoundObjectResult>();
        (await controller.DeleteTestingLabRoleTemplateByName("missing")).Should().BeOfType<NotFoundObjectResult>();

        sender.Setup(value => value.Send(
                It.IsAny<UpdateTestingLabRoleTemplateEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("conflict"));
        sender.Setup(value => value.Send(
                It.IsAny<DeleteTestingLabRoleTemplateEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("conflict"));

        (await controller.UpdateTestingLabRoleTemplate("conflict", new UpdateTestingLabRoleRequest()))
            .Result.Should().BeOfType<ConflictObjectResult>();
        (await controller.DeleteTestingLabRoleTemplate("conflict")).Should().BeOfType<ConflictObjectResult>();
        (await controller.DeleteTestingLabRoleTemplateByName("conflict")).Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task UserPermissionRead_MapsRolesResourceGrantsAndEffectiveCapabilities()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = new Mock<ITestingLabPermissionService>();
        service.Setup(value => value.GetUserRolesAsync(userId, tenantId))
            .ReturnsAsync([new TestingLabAssignedRole { RoleName = "Manager" }]);
        var permissions = EveryPermissionCombination()
            .Select(value => new TestingLabUserPermission
            {
                Action = value.Action,
                ResourceType = value.ResourceType
            })
            .ToList();
        permissions.Add(new TestingLabUserPermission
        {
            Action = TestingLabActions.Read,
            ResourceType = TestingLabResourceTypes.Event,
            ResourceId = Guid.NewGuid(),
            ExpiresAt = SystemClock.UtcNow.AddHours(1)
        });
        service.Setup(value => value.GetUserPermissionsAsync(userId, tenantId)).ReturnsAsync(permissions);
        var controller = Controller(Actor(tenantId), service.Object, Mock.Of<ISender>());

        var result = await controller.GetUserTestingLabPermissions(userId);
        var value = result.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<UserTestingLabPermissions>().Subject;

        value.TenantId.Should().Be(tenantId);
        value.AssignedRoles.Should().Equal("Manager");
        value.ResourcePermissions.Should().ContainSingle();
        AssertAllFlags(value.Permissions, true);
    }

    [Fact]
    public async Task TenantAndResourceGuards_RejectInvalidScopeAndExerciseValidCommands()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var service = new Mock<ITestingLabPermissionService>();
        service.Setup(value => value.HasPermissionAsync(
                userId, tenantId, TestingLabActions.Read, TestingLabResourceTypes.Event, resourceId))
            .ReturnsAsync(true);
        var sender = new Mock<ISender>();
        var controller = Controller(Actor(tenantId), service.Object, sender.Object);

        (await controller.AssignTestingLabRole(userId, new AssignTestingLabRoleRequest
        {
            RoleName = "Manager"
        })).Should().BeOfType<OkResult>();
        (await controller.AssignTestingLabRole(userId, new AssignTestingLabRoleRequest
        {
            TenantId = tenantId,
            RoleName = "Manager"
        })).Should().BeOfType<OkResult>();
        (await controller.RevokeTestingLabRole(userId, "Manager")).Should().BeOfType<NoContentResult>();
        (await controller.GrantResourcePermission(userId, TestingLabResourceTypes.Event, resourceId,
            new GrantResourcePermissionRequest { Action = TestingLabActions.Read }))
            .Should().BeOfType<OkResult>();
        (await controller.RevokeResourcePermission(userId, TestingLabResourceTypes.Event, resourceId,
            TestingLabActions.Read)).Should().BeOfType<NoContentResult>();
        var check = await controller.CheckTestingLabPermission(
            userId, TestingLabResourceTypes.Event, TestingLabActions.Read, resourceId);
        check.Result.Should().BeOfType<OkObjectResult>();

        var invalid = "Other";
        (await controller.GrantResourcePermission(userId, invalid, resourceId,
            new GrantResourcePermissionRequest())).Should().BeOfType<BadRequestObjectResult>();
        (await controller.RevokeResourcePermission(userId, invalid, resourceId, TestingLabActions.Read))
            .Should().BeOfType<BadRequestObjectResult>();
        (await controller.CheckTestingLabPermission(userId, invalid, TestingLabActions.Read))
            .Result.Should().BeOfType<BadRequestObjectResult>();

        var noTenant = Controller(ActorContext.Anonymous, service.Object, sender.Object);
        (await noTenant.GetUserTestingLabPermissions(userId)).Result.Should().BeOfType<ForbidResult>();
        (await noTenant.AssignTestingLabRole(userId, new AssignTestingLabRoleRequest()))
            .Should().BeOfType<ForbidResult>();
        (await noTenant.RevokeTestingLabRole(userId, "Manager")).Should().BeOfType<ForbidResult>();
        (await noTenant.GrantResourcePermission(userId, TestingLabResourceTypes.Event, resourceId,
            new GrantResourcePermissionRequest())).Should().BeOfType<ForbidResult>();
        (await noTenant.RevokeResourcePermission(userId, TestingLabResourceTypes.Event, resourceId,
            TestingLabActions.Read)).Should().BeOfType<ForbidResult>();
        (await noTenant.CheckTestingLabPermission(userId, TestingLabResourceTypes.Event,
            TestingLabActions.Read)).Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task AssignRole_MapsServiceFailureToNotFound()
    {
        var tenantId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(value => value.Send(
                It.IsAny<AssignTestingLabRoleEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("missing"));
        var controller = Controller(Actor(tenantId), Mock.Of<ITestingLabPermissionService>(), sender.Object);

        (await controller.AssignTestingLabRole(Guid.NewGuid(), new AssignTestingLabRoleRequest
        {
            RoleName = "Missing"
        })).Should().BeOfType<NotFoundObjectResult>();
    }

    private static TestingLabPermissionController Controller(
        ActorContext actor,
        ITestingLabPermissionService service,
        ISender sender)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(value => value.ActorContext).Returns(actor);
        return new TestingLabPermissionController(
            service, accessor.Object, sender, NullLogger<TestingLabPermissionController>.Instance);
    }

    private static ActorContext Actor(Guid tenantId) =>
        ActorContextBuilder.ForUser(Guid.NewGuid()).WithTenantId(tenantId).Build();

    private static List<PermissionTemplate> EveryPermissionCombination()
    {
        var result = new List<PermissionTemplate>
        {
            new() { Action = "other", ResourceType = "other" }
        };
        result.AddRange(TestingLabActions.All.Select(action =>
            new PermissionTemplate { Action = action, ResourceType = "other" }));
        result.AddRange(TestingLabActions.All.SelectMany(action => TestingLabResourceTypes.All.Select(resource =>
            new PermissionTemplate { Action = action, ResourceType = resource })));
        return result;
    }

    private static TestingLabPermissionsDto Permissions(bool value) => new()
    {
        CanCreateSessions = value,
        CanEditSessions = value,
        CanDeleteSessions = value,
        CanViewSessions = value,
        CanCreateLocations = value,
        CanEditLocations = value,
        CanDeleteLocations = value,
        CanViewLocations = value,
        CanCreateFeedback = value,
        CanEditFeedback = value,
        CanDeleteFeedback = value,
        CanViewFeedback = value,
        CanModerateFeedback = value,
        CanCreateRequests = value,
        CanEditRequests = value,
        CanDeleteRequests = value,
        CanViewRequests = value,
        CanApproveRequests = value,
        CanManageParticipants = value,
        CanViewParticipants = value,
        CanCreateEvents = value,
        CanEditEvents = value,
        CanDeleteEvents = value,
        CanViewEvents = value,
        CanViewApplications = value,
        CanApproveApplications = value,
        CanManageApplications = value,
        CanViewAnalytics = value
    };

    private static void AssertAllFlags(TestingLabPermissionsDto actual, bool expected)
    {
        actual.GetType().GetProperties()
            .Where(property => property.PropertyType == typeof(bool))
            .Select(property => (bool)property.GetValue(actual)!)
            .Should().OnlyContain(value => value == expected);
    }
}
