using FluentAssertions;

using Microsoft.AspNetCore.Mvc;

using Moq;

using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

using Xunit;

namespace GameGuild.Monitoring.SLA.UnitTests.Controllers;

public class SlaMonitoringControllerTenantContextTests
{
    [Fact]
    public async Task CreateSlo_WhenTenantMissing_ShouldUseActorTenant()
    {
        var sender = new Mock<ISender>();
        var tenantId = Guid.NewGuid();
        var controller = CreateController(sender, tenantId);
        var command = new CreateSloCommand(Guid.Empty, "API", null, "svc", 99.9, 30, 0.1, 50);
        sender.Setup(s => s.Send(It.Is<CreateSloCommand>(value => value.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SloDto { Id = Guid.NewGuid(), TenantId = tenantId, Name = "API" });

        var result = await controller.CreateSlo(command, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task GetSlos_WithExplicitTenant_ShouldPreferExplicitTenant()
    {
        var sender = new Mock<ISender>();
        var explicitTenant = Guid.NewGuid();
        var controller = CreateController(sender, Guid.NewGuid());
        sender.Setup(s => s.Send(It.Is<GetSlosQuery>(value => value.TenantId == explicitTenant), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await controller.GetSlos(explicitTenant, cancellationToken: CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetViolations_WithoutExplicitTenant_ShouldUseActorTenant()
    {
        var sender = new Mock<ISender>();
        var tenantId = Guid.NewGuid();
        var controller = CreateController(sender, tenantId);
        sender.Setup(s => s.Send(It.Is<GetSloViolationsQuery>(value => value.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await controller.GetViolations(cancellationToken: CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResolveViolation_WhenTenantMissing_ShouldUseActorTenant()
    {
        var sender = new Mock<ISender>();
        var tenantId = Guid.NewGuid();
        var controller = CreateController(sender, tenantId);
        var violationId = Guid.NewGuid();
        var command = new ConcreteResolveSloViolationCommand(violationId, Guid.Empty, "done");
        sender.Setup(s => s.Send(It.Is<ResolveSloViolationCommand>(value => value.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await controller.ResolveViolation(violationId, command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetSlo_WithoutTenantContext_ShouldThrowUnauthorizedAccessException()
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender, null);

        var action = () => controller.GetSlo(Guid.NewGuid(), CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static SlaMonitoringController CreateController(Mock<ISender> sender, Guid? tenantId)
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

    private sealed record ConcreteResolveSloViolationCommand(Guid ViolationId, Guid TenantId, string? ResolutionNotes = null)
        : ResolveSloViolationCommand(ViolationId, TenantId, ResolutionNotes);
}