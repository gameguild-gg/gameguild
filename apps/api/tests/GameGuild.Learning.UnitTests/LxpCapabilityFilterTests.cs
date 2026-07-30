using System.Security.Claims;
using FluentAssertions;
using GameGuild.Features;
using GameGuild.Learning.Attributes;
using GameGuild.Learning.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.UnitTests;

public sealed class LxpCapabilityFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_AuthenticatedSystemAdmin_BypassesTenantCapability()
    {
        var capabilityService = new Mock<ICapabilityService>(MockBehavior.Strict);
        var context = CreateContext(
            CreateUser("SystemAdmin"),
            capabilityService.Object);
        var nextCalled = false;

        await CreateFilter().OnActionExecutionAsync(context, CreateNext(context, () => nextCalled = true));

        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
        capabilityService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnActionExecutionAsync_RegularUserWithoutCapability_RemainsForbidden()
    {
        var tenantId = Guid.NewGuid();
        var capabilityService = new Mock<ICapabilityService>();
        capabilityService
            .Setup(service => service.IsCapabilityEnabledAsync(tenantId, LxpCapabilities.Social, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var context = CreateContext(
            CreateUser("User", tenantId),
            capabilityService.Object);
        var nextCalled = false;

        await CreateFilter().OnActionExecutionAsync(context, CreateNext(context, () => nextCalled = true));

        nextCalled.Should().BeFalse();
        context.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ResolvedRequestTenant_UsesValidatedTenantContext()
    {
        var tenantId = Guid.NewGuid();
        var capabilityService = new Mock<ICapabilityService>();
        capabilityService
            .Setup(service => service.IsCapabilityEnabledAsync(tenantId, LxpCapabilities.Social, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var requestContext = new Mock<IRequestContextAccessor>();
        requestContext.SetupGet(accessor => accessor.CurrentTenantId).Returns(tenantId);
        var context = CreateContext(CreateUser("User"), capabilityService.Object, requestContext.Object);
        var nextCalled = false;

        await CreateFilter().OnActionExecutionAsync(context, CreateNext(context, () => nextCalled = true));

        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
    }
    private static LxpCapabilityFilter CreateFilter()
        => new(NullLogger<LxpCapabilityFilter>.Instance);

    private static ActionExecutingContext CreateContext(
        ClaimsPrincipal user,
        ICapabilityService capabilityService,
        IRequestContextAccessor? requestContextAccessor = null)
    {
        var serviceCollection = new ServiceCollection()
            .AddSingleton(capabilityService);
        if (requestContextAccessor is not null)
        {
            serviceCollection.AddSingleton(requestContextAccessor);
        }
        var services = serviceCollection.BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            User = user,
            RequestServices = services
        };
        var actionDescriptor = new ActionDescriptor
        {
            EndpointMetadata = [new LxpCapabilityAttribute(LxpCapabilities.Social)]
        };

        return new ActionExecutingContext(
            new ActionContext(httpContext, new RouteData(), actionDescriptor),
            [],
            new Dictionary<string, object?>(),
            controller: new object());
    }

    private static ClaimsPrincipal CreateUser(string role, Guid? tenantId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role)
        };
        if (tenantId.HasValue)
        {
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));
    }

    private static ActionExecutionDelegate CreateNext(ActionExecutingContext context, Action callback)
        => () =>
        {
            callback();
            return Task.FromResult(new ActionExecutedContext(
                context,
                context.Filters,
                context.Controller));
        };
}
