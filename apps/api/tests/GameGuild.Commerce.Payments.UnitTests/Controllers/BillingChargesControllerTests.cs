using FluentAssertions;
using GameGuild.Commerce;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Controllers;

public sealed class BillingChargesControllerTests
{
    [Fact]
    public async Task GetCharges_ShouldDispatchPaymentQueryUsingBillingRouteContract()
    {
        var tenantId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(
                It.Is<GetAllPaymentsQuery>(query =>
                    query.TenantId == tenantId &&
                    query.Status == "failed" &&
                    query.Page == 1 &&
                    query.PageSize == 100),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PaymentResult>());

        var controller = CreateController(sender.Object, ActorContext.Anonymous);

        var result = await controller.GetCharges(tenantId, "failed", null, null, page: 0, pageSize: 500, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task GetCharges_ShouldDefaultPageSizeWhenLessThanOne()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(
                It.Is<GetAllPaymentsQuery>(query => query.Page == 2 && query.PageSize == 20),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PaymentResult>());

        var controller = CreateController(sender.Object, ActorContext.Anonymous);

        var result = await controller.GetCharges(null, null, null, null, page: 2, pageSize: 0, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task CreateCharge_ShouldValidateTenantAndDispatchProcessPaymentCommand()
    {
        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(
                It.Is<ProcessPaymentCommand>(command =>
                    command.TenantId == tenantId &&
                    command.SubscriptionId == subscriptionId &&
                    command.Amount == 49.95m &&
                    command.PaymentMethodId == "pm_card_visa"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult
            {
                Success = true,
                PaymentId = Guid.NewGuid().ToString(),
                Status = PaymentStatus.Succeeded,
                Amount = new Money(49.95m, "USD")
            });

        var controller = CreateController(sender.Object, CreateAuthenticatedActorContext(tenantId));

        var result = await controller.CreateCharge(
            new BillingChargesController.CreateBillingChargeRequest(tenantId, subscriptionId, 49.95m, "pm_card_visa"),
            CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task CreateCharge_ShouldForbidCrossTenantRequests()
    {
        var controller = CreateController(Mock.Of<ISender>(), CreateAuthenticatedActorContext(Guid.NewGuid()));

        var result = await controller.CreateCharge(
            new BillingChargesController.CreateBillingChargeRequest(Guid.NewGuid(), Guid.NewGuid(), 49.95m, "pm_card_visa"),
            CancellationToken.None);

        var forbidden = result.Should().BeOfType<ObjectResult>().Subject;
        forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    private static BillingChargesController CreateController(ISender sender, ActorContext actorContext)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(value => value.ActorContext).Returns(actorContext);

        return new BillingChargesController(sender, accessor.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static ActorContext CreateAuthenticatedActorContext(Guid? tenantId)
    {
        return new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Test",
            IsAuthenticated = true
        };
    }
}
