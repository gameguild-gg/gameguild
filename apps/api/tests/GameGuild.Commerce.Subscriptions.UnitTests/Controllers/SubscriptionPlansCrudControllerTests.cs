using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Controllers;

public class SubscriptionPlansCrudControllerTests
{
    [Fact]
    public void GetSubscriptionPlans_ShouldAllowAnonymous()
    {
        var method = typeof(SubscriptionPlansCrudController)
            .GetMethod(nameof(SubscriptionPlansCrudController.GetSubscriptionPlans), BindingFlags.Instance | BindingFlags.Public);

        method.Should().NotBeNull();
        method!.GetCustomAttribute<AllowAnonymousAttribute>().Should().NotBeNull();
    }

    [Fact]
    public async Task GetSubscriptionPlans_ShouldReturnNotFound_ForInactiveSlug_WhenAnonymous()
    {
        var sender = new Mock<ISender>();
        var inactivePlan = new SubscriptionPlan("Legacy", "legacy", 999);
        inactivePlan.Deactivate();

        sender
            .Setup(service => service.Send(It.Is<GetSubscriptionPlanBySlugQuery>(query => query.Slug == "legacy"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactivePlan);

        var controller = CreateController(sender.Object, authenticated: false);

        var result = await controller.GetSubscriptionPlans(slug: "legacy");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSubscriptionPlans_ShouldForceActiveFilter_ForAnonymousPagedRequests()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(
                It.Is<GetPagedSubscriptionPlansQuery>(query =>
                    query.Page == 2 &&
                    query.PageSize == 5 &&
                    query.IsActive == true &&
                    query.IsFeatured == false &&
                    query.SearchTerm == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SubscriptionPlan>());

        var controller = CreateController(sender.Object, authenticated: false);

        var result = await controller.GetSubscriptionPlans(page: 2, pageSize: 5, isActive: false, isFeatured: false);

        result.Should().BeOfType<OkObjectResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task GetSubscriptionPlans_ShouldFilterInactiveSearchResults_WhenAnonymous()
    {
        var sender = new Mock<ISender>();
        var activePlan = new SubscriptionPlan("Active", "active", 1999);
        var inactivePlan = new SubscriptionPlan("Inactive", "inactive", 999);
        inactivePlan.Deactivate();

        sender
            .Setup(service => service.Send(It.Is<SearchSubscriptionPlansQuery>(query => query.SearchTerm == "plan"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { activePlan, inactivePlan });

        var controller = CreateController(sender.Object, authenticated: false);

        var result = await controller.GetSubscriptionPlans(q: "plan");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var plans = ok.Value.Should().BeAssignableTo<IEnumerable<SubscriptionPlan>>().Subject.ToList();
        plans.Should().ContainSingle(plan => plan.IsActive);
        plans.Should().OnlyContain(plan => plan.IsActive);
    }

    private static SubscriptionPlansCrudController CreateController(ISender sender, bool authenticated)
    {
        var identity = authenticated
            ? new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Test")
            : new ClaimsIdentity();

        return new SubscriptionPlansCrudController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }
}
