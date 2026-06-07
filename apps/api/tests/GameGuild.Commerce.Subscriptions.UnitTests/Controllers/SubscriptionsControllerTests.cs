using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Controllers;

public class SubscriptionsControllerTests
{
    [Fact]
    public async Task GetSubscriptions_ShouldNormalizePaging_ForAnonymousRequests()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(
                It.Is<GetPagedSubscriptionsQuery>(query =>
                    query.Page == 1 &&
                    query.PageSize == 100 &&
                    query.Status == SubscriptionStatus.Active &&
                    query.TenantId == null &&
                    query.PlanId == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<Subscription>.Empty(100));

        var controller = CreateController(sender.Object, ActorContext.Anonymous);

        var result = await controller.GetSubscriptions(page: 0, pageSize: 500, status: SubscriptionStatus.Active);

        result.Should().BeOfType<OkObjectResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task GetSubscriptions_ShouldReturnBadRequest_WhenAuthenticatedWithoutTenantContext()
    {
        var controller = CreateController(Mock.Of<ISender>(), CreateAuthenticatedActorContext(null));

        var result = await controller.GetSubscriptions();

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetSubscriptions_ShouldAllowSystemAdminWithoutTenantContext()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(
                It.Is<GetPagedSubscriptionsQuery>(query =>
                    query.Page == 1 &&
                    query.PageSize == 20 &&
                    query.TenantId == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<Subscription>.Empty(20));

        var controller = CreateController(sender.Object, CreateAuthenticatedActorContext(null, "SystemAdmin"));

        var result = await controller.GetSubscriptions();

        result.Should().BeOfType<OkObjectResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task GetSubscriptions_ShouldReturnForbid_WhenRequestedTenantDoesNotMatchActorTenant()
    {
        var controller = CreateController(Mock.Of<ISender>(), CreateAuthenticatedActorContext(Guid.NewGuid()));

        var result = await controller.GetSubscriptions(tenantId: Guid.NewGuid());

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetSubscriptions_ShouldAllowSystemAdminRequestedTenantFilter()
    {
        var requestedTenantId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(
                It.Is<GetPagedSubscriptionsQuery>(query => query.TenantId == requestedTenantId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<Subscription>.Empty(20));

        var controller = CreateController(sender.Object, CreateAuthenticatedActorContext(null, "SystemAdmin"));

        var result = await controller.GetSubscriptions(tenantId: requestedTenantId);

        result.Should().BeOfType<OkObjectResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task GetSubscriptions_ShouldDefaultTenantFilter_WhenAuthenticatedAndTenantMatches()
    {
        var tenantId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(
                It.Is<GetPagedSubscriptionsQuery>(query =>
                    query.Page == 1 &&
                    query.PageSize == 20 &&
                    query.TenantId == tenantId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<Subscription>.Empty(20));

        var controller = CreateController(sender.Object, CreateAuthenticatedActorContext(tenantId));

        var result = await controller.GetSubscriptions();

        result.Should().BeOfType<OkObjectResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task GetSubscriptions_ShouldDispatchExpiringQuery_WhenRequested()
    {
        var tenantId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(
                It.Is<GetExpiringSubscriptionsQuery>(query => query.Days == 14),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Subscription>());

        var controller = CreateController(sender.Object, CreateAuthenticatedActorContext(tenantId));

        var result = await controller.GetSubscriptions(expiring: true, expiringDays: 14);

        result.Should().BeOfType<OkObjectResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task CheckSubscriptionExistsById_ShouldReturnNotFound_WhenSubscriptionDoesNotExist()
    {
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(It.IsAny<GetSubscriptionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var controller = CreateController(sender.Object, ActorContext.Anonymous);

        var result = await controller.CheckSubscriptionExistsById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CheckSubscriptionExistsById_ShouldReturnBadRequest_WhenAuthenticatedWithoutTenantContext()
    {
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(It.IsAny<GetSubscriptionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSubscription(Guid.NewGuid()));

        var controller = CreateController(sender.Object, CreateAuthenticatedActorContext(null));

        var result = await controller.CheckSubscriptionExistsById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CheckSubscriptionExistsById_ShouldReturnNotFound_WhenSubscriptionBelongsToAnotherTenant()
    {
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(It.IsAny<GetSubscriptionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSubscription(Guid.NewGuid()));

        var controller = CreateController(sender.Object, CreateAuthenticatedActorContext(Guid.NewGuid()));

        var result = await controller.CheckSubscriptionExistsById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CheckSubscriptionExistsById_ShouldReturnOk_WhenSubscriptionIsAccessible()
    {
        var tenantId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(It.IsAny<GetSubscriptionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSubscription(tenantId));

        var controller = CreateController(sender.Object, CreateAuthenticatedActorContext(tenantId));

        var result = await controller.CheckSubscriptionExistsById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task GetSubscriptionById_ShouldReturnNotFound_WhenSubscriptionDoesNotExist()
    {
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(It.IsAny<GetSubscriptionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var controller = CreateController(sender.Object, ActorContext.Anonymous);

        var result = await controller.GetSubscriptionById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSubscriptionById_ShouldReturnBadRequest_WhenAuthenticatedWithoutTenantContext()
    {
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(It.IsAny<GetSubscriptionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSubscription(Guid.NewGuid()));

        var controller = CreateController(sender.Object, CreateAuthenticatedActorContext(null));

        var result = await controller.GetSubscriptionById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetSubscriptionById_ShouldReturnNotFound_WhenSubscriptionBelongsToAnotherTenant()
    {
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(It.IsAny<GetSubscriptionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSubscription(Guid.NewGuid()));

        var controller = CreateController(sender.Object, CreateAuthenticatedActorContext(Guid.NewGuid()));

        var result = await controller.GetSubscriptionById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSubscriptionById_ShouldReturnOk_WhenSubscriptionIsAccessible()
    {
        var tenantId = Guid.NewGuid();
        var subscription = CreateSubscription(tenantId);
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(It.IsAny<GetSubscriptionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var controller = CreateController(sender.Object, CreateAuthenticatedActorContext(tenantId));

        var result = await controller.GetSubscriptionById(Guid.NewGuid(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(subscription);
    }

    private static SubscriptionsController CreateController(ISender sender, ActorContext actorContext)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(value => value.ActorContext).Returns(actorContext);

        return new SubscriptionsController(sender, accessor.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static ActorContext CreateAuthenticatedActorContext(Guid? tenantId, params string[] roles)
    {
        return new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(roles),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Test",
            IsAuthenticated = true
        };
    }

    private static Subscription CreateSubscription(Guid tenantId)
    {
        var subscription = new Subscription(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            BillingCycle.Monthly,
            new Money(29.99m, "USD"),
            DateTime.UtcNow,
            null);

        typeof(Subscription).GetProperty(nameof(Subscription.Id))!.SetValue(subscription, Guid.NewGuid());
        return subscription;
    }
}
