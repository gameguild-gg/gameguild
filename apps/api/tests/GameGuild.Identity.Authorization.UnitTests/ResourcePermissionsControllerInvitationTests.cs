using FluentAssertions;
using GameGuild.CQRS;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests;

public class ResourcePermissionsControllerInvitationTests
{
    [Fact]
    public async Task GetInvitation_ShouldReturnOk_WhenSenderReturnsInvitation()
    {
        var invitationId = Guid.NewGuid();
        var response = new GetResourceInvitationResponse
        {
            Invitation = CreateInvitationDto(invitationId)
        };

        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.Is<GetResourceInvitationQuery>(query => query.InvitationId == invitationId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = CreateController(sender);

        var result = await controller.GetInvitation(invitationId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task GetPendingInvitations_ShouldReturnOk_WhenSenderReturnsPendingInvitations()
    {
        var response = new GetPendingResourceInvitationsResponse
        {
            Invitations = [CreateInvitationDto(Guid.NewGuid())]
        };

        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<GetPendingResourceInvitationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = CreateController(sender);

        var result = await controller.GetPendingInvitations(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task AcceptInvitation_ShouldReturnOk_WhenSenderAcceptsInvitation()
    {
        var invitationId = Guid.NewGuid();
        var response = new InvitationActionResult
        {
            Success = true,
            InvitationId = invitationId,
            Status = InvitationStatus.Accepted.ToString()
        };

        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.Is<AcceptResourceInvitationCommand>(command => command.InvitationId == invitationId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = CreateController(sender);

        var result = await controller.AcceptInvitation(invitationId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task DeclineInvitation_ShouldReturnOk_WhenSenderDeclinesInvitation()
    {
        var invitationId = Guid.NewGuid();
        var request = new DeclineInvitationRequest("No thanks");
        var response = new InvitationActionResult
        {
            Success = true,
            InvitationId = invitationId,
            Status = InvitationStatus.Declined.ToString()
        };

        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(
                It.Is<DeclineResourceInvitationCommand>(command => command.InvitationId == invitationId && command.Reason == request.Reason),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = CreateController(sender);

        var result = await controller.DeclineInvitation(invitationId, request, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task RevokeInvitation_ShouldReturnOk_WhenSenderRevokesInvitation()
    {
        var invitationId = Guid.NewGuid();
        var response = new InvitationActionResult
        {
            Success = true,
            InvitationId = invitationId,
            Status = InvitationStatus.Revoked.ToString()
        };

        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.Is<RevokeResourceInvitationCommand>(command => command.InvitationId == invitationId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = CreateController(sender);

        var result = await controller.RevokeInvitation(invitationId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    private static ResourcePermissionsController CreateController(Mock<ISender> sender)
    {
        return new ResourcePermissionsController(sender.Object, NullLogger<ResourcePermissionsController>.Instance);
    }

    private static ResourceInvitationDto CreateInvitationDto(Guid invitationId)
    {
        return new ResourceInvitationDto(
            invitationId,
            Guid.NewGuid(),
            "invitee@example.com",
            "course",
            Guid.NewGuid().ToString(),
            ["read"],
            "Join this resource",
            "Admin",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7),
            InvitationStatus.Pending.ToString());
    }
}
