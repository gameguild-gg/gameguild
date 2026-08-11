using FluentAssertions;
using GameGuild.API.Controllers;
using GameGuild.CQRS;
using GameGuild.Economy.Commands;
using GameGuild.Economy.Funding;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GameGuild.API.UnitTests.Controllers;

public sealed class EconomyWalletControllerTests
{
    [Fact]
    public async Task ConvertMyHardToSoft_WhenActorHasNoSelfServiceContext_ReturnsForbid()
    {
        var sender = new Mock<ISender>(MockBehavior.Strict);
        var controller = new EconomyWalletController(sender.Object, new ActorContextAccessor());

        var result = await controller.ConvertMyHardToSoft(
            new ConvertMyHardToSoftRequest(100, 0, Guid.NewGuid(), "conversion-key"),
            CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        sender.Verify(value => value.Send(It.IsAny<ConvertMyHardToSoftCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConvertMyHardToSoft_WhenActorHasSelfServiceContext_UsesCqrsAndReturnsReceipt()
    {
        var actorId = Guid.Parse("93000000-0000-0000-0000-000000000001");
        var tenantId = Guid.Parse("93000000-0000-0000-0000-000000000002");
        var riskDecisionId = Guid.Parse("93000000-0000-0000-0000-000000000003");
        var request = new ConvertMyHardToSoftRequest(100, 3, riskDecisionId, "conversion-key");
        var receipt = new SelfServiceHardToSoftConversionReceipt(
            Guid.Parse("93000000-0000-0000-0000-000000000004"), null, 17, "journal-hash", false);
        var sender = new Mock<ISender>();
        sender
            .Setup(value => value.Send(
                It.Is<ConvertMyHardToSoftCommand>(command => command.Request == request),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = actorId.ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            IsAuthenticated = true,
        });
        var controller = new EconomyWalletController(sender.Object, accessor);

        var result = await controller.ConvertMyHardToSoft(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(receipt);
        sender.Verify(value => value.Send(
            It.Is<ConvertMyHardToSoftCommand>(command => command.Request == request),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
