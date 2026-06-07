using System.Security.Claims;
using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Features;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning;
using GameGuild.Learning.Abstractions;
using GameGuild.Learning.Attributes;
using GameGuild.Learning.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.UnitTests;

public class LearningServiceCollectionExtensionCoverageTests
{
    [Fact]
    public void RegistrationExtensions_ShouldRegisterProviderImplementations()
    {
        var services = new ServiceCollection();

        services.AddLearningCore()
            .AddCourseInfoProvider<FakeCourseInfoProvider>()
            .AddEnrollmentInfoProvider<FakeEnrollmentInfoProvider>()
            .AddProgressInfoProvider<FakeProgressInfoProvider>()
            .AddLearnerProfileProvider<FakeLearnerProfileProvider>()
            .AddLearningEventPublisher<FakeLearningEventPublisher>()
            .AddLearningCapabilityService<FakeLearningCapabilityService>();

        AssertScoped<ICourseInfoProvider, FakeCourseInfoProvider>(services);
        AssertScoped<IEnrollmentInfoProvider, FakeEnrollmentInfoProvider>(services);
        AssertScoped<IProgressInfoProvider, FakeProgressInfoProvider>(services);
        AssertScoped<ILearnerProfileProvider, FakeLearnerProfileProvider>(services);
        AssertScoped<ILearningEventPublisher, FakeLearningEventPublisher>(services);
        AssertScoped<ILearningCapabilityService, FakeLearningCapabilityService>(services);
    }

    [Fact]
    public void InfoRecords_ShouldExposeComputedProperties()
    {
        new EnrollmentInfo { CompletedAt = SystemClock.UtcNow }.IsCompleted.Should().BeTrue();
        new EnrollmentInfo { CompletedAt = null }.IsCompleted.Should().BeFalse();
        var progress = new ProgressInfo { StartedAt = SystemClock.UtcNow, CompletedAt = SystemClock.UtcNow };
        progress.IsStarted.Should().BeTrue();
        progress.IsCompleted.Should().BeTrue();
        new ProgressInfo().IsStarted.Should().BeFalse();
    }

    private static void AssertScoped<TService, TImplementation>(IServiceCollection services)
    {
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(TService) &&
            descriptor.ImplementationType == typeof(TImplementation) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}

public class LxpCapabilityFilterCoverageTests
{
    [Fact]
    public async Task OnActionExecutionAsync_WithNoAttributes_ShouldProceed()
    {
        var filter = new LxpCapabilityFilter(NullLogger<LxpCapabilityFilter>.Instance);
        var context = CreateContext();
        var called = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new object()));
        });

        called.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithMissingTenant_ShouldReturnBadRequest()
    {
        var filter = new LxpCapabilityFilter(NullLogger<LxpCapabilityFilter>.Instance);
        var context = CreateContext(new LxpCapabilityAttribute(LxpCapabilities.Discovery));

        await filter.OnActionExecutionAsync(context, NotCalled);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithMissingCapabilityService_ShouldReturnUnavailable()
    {
        var filter = new LxpCapabilityFilter(NullLogger<LxpCapabilityFilter>.Instance);
        var tenantId = Guid.NewGuid();
        var context = CreateContext(new LxpCapabilityAttribute(LxpCapabilities.Discovery));
        context.HttpContext.Request.RouteValues["tenantId"] = tenantId;

        await filter.OnActionExecutionAsync(context, NotCalled);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenCapabilityDisabled_ShouldReturnForbidden()
    {
        var capability = new Mock<ICapabilityService>();
        capability.Setup(s => s.IsCapabilityEnabledAsync(It.IsAny<Guid>(), LxpCapabilities.Discovery, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var filter = new LxpCapabilityFilter(NullLogger<LxpCapabilityFilter>.Instance);
        var context = CreateContext(new LxpCapabilityAttribute(LxpCapabilities.Discovery) { ErrorMessage = "Upgrade required" }, capability.Object);
        context.HttpContext.Request.Headers["X-Tenant-Id"] = Guid.NewGuid().ToString();

        await filter.OnActionExecutionAsync(context, NotCalled);

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        result.Value.Should().BeOfType<ProblemDetails>().Which.Detail.Should().Be("Upgrade required");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenCapabilityThrows_ShouldReturnUnavailable()
    {
        var capability = new Mock<ICapabilityService>();
        capability.Setup(s => s.IsCapabilityEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var filter = new LxpCapabilityFilter(NullLogger<LxpCapabilityFilter>.Instance);
        var context = CreateContext(new LxpCapabilityAttribute(LxpCapabilities.Discovery), capability.Object);
        context.HttpContext.Request.QueryString = QueryString.Create("tenantId", Guid.NewGuid().ToString());

        await filter.OnActionExecutionAsync(context, NotCalled);

        context.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithEnabledCapabilities_ShouldProceedForAllTenantSources()
    {
        var capability = new Mock<ICapabilityService>();
        capability.Setup(s => s.IsCapabilityEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await AssertProceedWithTenant(context => context.HttpContext.Request.RouteValues["tenantId"] = Guid.NewGuid().ToString(), capability.Object);
        await AssertProceedWithTenant(context =>
        {
            context.HttpContext.Request.RouteValues["tenantId"] = "bad-route";
            context.HttpContext.Request.Headers["X-Tenant-Id"] = Guid.NewGuid().ToString();
        }, capability.Object);
        await AssertProceedWithTenant(context =>
        {
            context.HttpContext.Request.RouteValues["tenantId"] = null;
            context.HttpContext.Request.Headers["X-Tenant-Id"] = Guid.NewGuid().ToString();
        }, capability.Object);
        await AssertProceedWithTenant(context =>
        {
            context.HttpContext.Request.RouteValues["tenantId"] = "bad-route";
            context.HttpContext.Request.Headers["X-Tenant-Id"] = "bad-header";
            context.HttpContext.Request.QueryString = QueryString.Create("tenantId", Guid.NewGuid().ToString());
        }, capability.Object);
        await AssertProceedWithTenant(context =>
        {
            context.HttpContext.Request.RouteValues["tenantId"] = "bad-route";
            context.HttpContext.Request.Headers["X-Tenant-Id"] = "bad-header";
            context.HttpContext.Request.QueryString = QueryString.Create("tenantId", "bad-query");
            context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenantId", Guid.NewGuid().ToString())]));
        }, capability.Object);
        await AssertProceedWithTenant(context =>
        {
            context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", Guid.NewGuid().ToString())]));
        }, capability.Object);
    }

    [Fact]
    public void LxpCapabilityFilterAttribute_ShouldCreateFilter()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        var attribute = new LxpCapabilityFilterAttribute();

        attribute.IsReusable.Should().BeTrue();
        attribute.CreateInstance(services).Should().BeOfType<LxpCapabilityFilter>();
    }

    private static async Task AssertProceedWithTenant(Action<ActionExecutingContext> configure, ICapabilityService capability)
    {
        var filter = new LxpCapabilityFilter(NullLogger<LxpCapabilityFilter>.Instance);
        var context = CreateContext(new LxpCapabilityAttribute(LxpCapabilities.Discovery), capability);
        configure(context);
        var called = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new object()));
        });

        called.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    private static ActionExecutingContext CreateContext(LxpCapabilityAttribute? attribute = null, ICapabilityService? capabilityService = null)
    {
        var services = new ServiceCollection();
        if (capabilityService != null)
        {
            services.AddSingleton(capabilityService);
        }

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        var actionDescriptor = new ControllerActionDescriptor();
        if (attribute != null)
        {
            actionDescriptor.EndpointMetadata = new List<object> { attribute };
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        return new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), new object());
    }

    private static Task<ActionExecutedContext> NotCalled() => throw new InvalidOperationException("next should not be called");
}

public class LearningControllerBaseCoverageTests
{
    [Fact]
    public void Constructor_WithNullAccessor_ShouldThrow()
    {
        var act = () => new TestLearningController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ActorHelpers_ShouldReturnValuesOrThrow()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var controller = new TestLearningController(Accessor(Actor(userId, tenantId)));

        controller.RequiredUserId().Should().Be(userId);
        controller.OptionalUserId().Should().Be(userId);
        controller.RequiredActorContext().TenantId.Should().Be(tenantId);
        controller.CurrentTenantId().Should().Be(tenantId);

        new TestLearningController(Accessor(ActorContext.Anonymous)).Invoking(c => c.RequiredUserId())
            .Should().Throw<UnauthorizedAccessException>();
        new TestLearningController(Accessor(ActorContext.Anonymous)).Invoking(c => c.RequiredActorContext())
            .Should().Throw<UnauthorizedAccessException>();
        new TestLearningController(Accessor(null)).Invoking(c => c.RequiredActorContext())
            .Should().Throw<UnauthorizedAccessException>();
        new TestLearningController(Accessor(null)).OptionalUserId().Should().BeNull();
        new TestLearningController(Accessor(null)).CurrentTenantId().Should().BeNull();
    }

    [Fact]
    public void ResultHelpers_ShouldMapNullAndNonNullValues()
    {
        var controller = new TestLearningController(Accessor(Actor(Guid.NewGuid())));

        controller.OkOrNotFoundPublic("value").Result.Should().BeOfType<OkObjectResult>();
        controller.OkOrNotFoundPublic(null).Result.Should().BeOfType<NotFoundObjectResult>();
        controller.OkOrNotFoundPublic(null, "missing").Result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("missing");

        controller.MapOrNotFoundPublic("value").Result.Should().BeOfType<OkObjectResult>();
        controller.MapOrNotFoundPublic(null).Result.Should().BeOfType<NotFoundObjectResult>();
        controller.MapOrNotFoundPublic(null, "missing").Result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("missing");
    }

    [Fact]
    public void CanAccessUserResource_ShouldCoverSelfAdminTenantAdminAndDeniedBranches()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        new TestLearningController(Accessor(null)).CanAccess(otherUserId).Should().BeFalse();
        new TestLearningController(Accessor(Actor(userId))).CanAccess(userId).Should().BeTrue();
        new TestLearningController(Accessor(Actor(userId))).CanAccess(otherUserId).Should().BeFalse();
        new TestLearningController(Accessor(Actor(userId, roles: new HashSet<string> { "Admin" }))).CanAccess(otherUserId).Should().BeTrue();
        new TestLearningController(Accessor(Actor(userId, roles: new HashSet<string> { "TenantAdmin" }))).CanAccess(otherUserId).Should().BeTrue();
    }

    private static IActorContextAccessor Accessor(ActorContext? actor)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(a => a.ActorContext).Returns(actor!);
        return accessor.Object;
    }

    private static ActorContext Actor(Guid userId, Guid? tenantId = null, IReadOnlySet<string>? roles = null) => new()
    {
        ActorKind = ActorKind.User,
        SubjectId = userId.ToString(),
        TenantId = tenantId,
        Roles = roles ?? new HashSet<string>(),
        Permissions = new HashSet<string>(),
        IsAuthenticated = true
    };

    private sealed class TestLearningController(IActorContextAccessor actorContextAccessor) : LearningControllerBase(actorContextAccessor)
    {
        public Guid RequiredUserId() => GetRequiredUserId();
        public Guid? OptionalUserId() => GetOptionalUserId();
        public ActorContext RequiredActorContext() => GetRequiredActorContext();
        public Guid? CurrentTenantId() => GetCurrentTenantId();
        public ActionResult<string> OkOrNotFoundPublic(string? value, string? message = null) => OkOrNotFound(value, message);
        public ActionResult<string> MapOrNotFoundPublic(string? value, string? message = null) => MapOrNotFound(value, v => v.ToUpperInvariant(), message);
        public bool CanAccess(Guid resourceUserId) => CanAccessUserResource(resourceUserId);
    }
}

internal sealed class FakeCourseInfoProvider : ICourseInfoProvider
{
    public Task<CourseBasicInfo?> GetCourseBasicInfoAsync(Guid courseId, CancellationToken cancellationToken = default) => Task.FromResult<CourseBasicInfo?>(null);
    public Task<IReadOnlyDictionary<Guid, CourseBasicInfo>> GetCourseBasicInfoBatchAsync(IEnumerable<Guid> courseIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<Guid, CourseBasicInfo>>(new Dictionary<Guid, CourseBasicInfo>());
    public Task<bool> IsCourseAvailableAsync(Guid courseId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<IReadOnlyList<Guid>> FindCourseIdsAsync(CourseSearchCriteria criteria, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
}

internal sealed class FakeEnrollmentInfoProvider : IEnrollmentInfoProvider
{
    public Task<bool> IsUserEnrolledAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<IReadOnlyList<Guid>> GetUserEnrolledCourseIdsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
    public Task<EnrollmentInfo?> GetEnrollmentInfoAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default) => Task.FromResult<EnrollmentInfo?>(null);
    public Task<IReadOnlyDictionary<Guid, EnrollmentInfo>> GetEnrollmentInfoBatchAsync(Guid userId, IEnumerable<Guid> courseIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<Guid, EnrollmentInfo>>(new Dictionary<Guid, EnrollmentInfo>());
    public Task<int> GetEnrollmentCountAsync(Guid courseId, CancellationToken cancellationToken = default) => Task.FromResult(0);
}

internal sealed class FakeProgressInfoProvider : IProgressInfoProvider
{
    public Task<ProgressInfo?> GetCourseProgressAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default) => Task.FromResult<ProgressInfo?>(null);
    public Task<IReadOnlyDictionary<Guid, ProgressInfo>> GetCourseProgressBatchAsync(Guid userId, IEnumerable<Guid> courseIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<Guid, ProgressInfo>>(new Dictionary<Guid, ProgressInfo>());
    public Task<ProgressInfo?> GetLearningPathProgressAsync(Guid userId, Guid learningPathId, CancellationToken cancellationToken = default) => Task.FromResult<ProgressInfo?>(null);
    public Task<IReadOnlyList<Guid>> GetCompletedCourseIdsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
    public Task<IReadOnlyList<Guid>> GetInProgressCourseIdsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
    public Task<LearningStatistics> GetUserLearningStatisticsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(new LearningStatistics { UserId = userId });
}

internal sealed class FakeLearnerProfileProvider : ILearnerProfileProvider
{
    public Task<IReadOnlyList<SkillInterest>> GetUserSkillInterestsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SkillInterest>>(Array.Empty<SkillInterest>());
    public Task<LearningPreferences?> GetUserLearningPreferencesAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<LearningPreferences?>(null);
    public Task<IReadOnlyList<Guid>> GetUsersWithSimilarInterestsAsync(Guid userId, int maxResults = 10, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
    public Task<IReadOnlyList<AcquiredSkill>> GetUserAcquiredSkillsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AcquiredSkill>>(Array.Empty<AcquiredSkill>());
    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
}

internal sealed class FakeLearningEventPublisher : ILearningEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : DomainEvent => Task.CompletedTask;
    public Task PublishManyAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class FakeLearningCapabilityService : ILearningCapabilityService
{
    public Task<bool> IsDiscoveryEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> IsLearningPathsEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> IsRecommendationsEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> IsAiRecommendationsEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> IsSkillsEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> IsAssessmentsEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> IsCertificatesEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<LearningCapabilities> GetLearningCapabilitiesAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult(LearningCapabilities.Free);
}
