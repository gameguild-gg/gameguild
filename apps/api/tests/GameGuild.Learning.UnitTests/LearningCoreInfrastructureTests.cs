using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Learning.UnitTests;

public sealed class LearningControllerBaseTests
{
    [Fact]
    public void Constructor_RejectsMissingActorAccessor()
    {
        var action = () => new TestController(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("actorContextAccessor");
    }

    [Fact]
    public void UserAndTenantHelpers_ReturnAuthenticatedActorValues()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var actor = CreateActor(userId, tenantId);
        var controller = new TestController(Accessor(actor));

        controller.RequiredUserId().Should().Be(userId);
        controller.OptionalUserId().Should().Be(userId);
        controller.RequiredActor().Should().BeSameAs(actor);
        controller.CurrentTenantId().Should().Be(tenantId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public void RequiredUserHelpers_RejectActorsWithoutGuidSubjects(string? subjectId)
    {
        var controller = new TestController(Accessor(CreateActor(subjectId)));

        controller.Invoking(candidate => candidate.RequiredUserId())
            .Should().Throw<UnauthorizedAccessException>();
        controller.Invoking(candidate => candidate.RequiredActor())
            .Should().Throw<UnauthorizedAccessException>();
        controller.OptionalUserId().Should().BeNull();
    }

    [Fact]
    public void RequiredActor_RejectsMissingActorContext()
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(candidate => candidate.ActorContext).Returns((ActorContext)null!);
        var controller = new TestController(accessor.Object);

        controller.Invoking(candidate => candidate.RequiredActor())
            .Should().Throw<UnauthorizedAccessException>();
        controller.OptionalUserId().Should().BeNull();
        controller.CurrentTenantId().Should().BeNull();
    }

    [Fact]
    public void OkOrNotFound_MapsMissingAndPresentResources()
    {
        var controller = new TestController(Accessor(CreateActor(Guid.NewGuid())));
        var entity = new TestResource("present");

        controller.OkResource(entity).Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(entity);
        controller.OkResource(null).Result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Resource not found");
        controller.OkResource(null, "Missing lesson").Result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Missing lesson");
    }

    [Fact]
    public void MapOrNotFound_MapsMissingAndPresentResources()
    {
        var controller = new TestController(Accessor(CreateActor(Guid.NewGuid())));

        controller.MapResource(new TestResource("lesson")).Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be("LESSON");
        controller.MapResource(null).Result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Resource not found");
        controller.MapResource(null, "Missing content").Result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Missing content");
    }

    [Theory]
    [InlineData("Member", false)]
    [InlineData("SystemAdmin", true)]
    [InlineData("TenantAdmin", true)]
    public void CanAccessUserResource_UsesOwnershipAndAdministrativeRoles(string role, bool canAccessOtherUser)
    {
        var userId = Guid.NewGuid();
        var controller = new TestController(Accessor(CreateActor(userId, roles: new HashSet<string> { role })));

        controller.CanAccess(userId).Should().BeTrue();
        controller.CanAccess(Guid.NewGuid()).Should().Be(canAccessOtherUser);
    }

    [Fact]
    public void CanAccessUserResource_DeniesAnonymousActor()
    {
        var controller = new TestController(Accessor(ActorContext.Anonymous));

        controller.CanAccess(Guid.NewGuid()).Should().BeFalse();
    }

    private static IActorContextAccessor Accessor(ActorContext actor)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(candidate => candidate.ActorContext).Returns(actor);
        return accessor.Object;
    }

    private static ActorContext CreateActor(Guid userId, Guid? tenantId = null, IReadOnlySet<string>? roles = null) =>
        CreateActor(userId.ToString(), tenantId, roles);

    private static ActorContext CreateActor(string? subjectId, Guid? tenantId = null, IReadOnlySet<string>? roles = null) => new()
    {
        ActorKind = ActorKind.User,
        SubjectId = subjectId,
        TenantId = tenantId,
        Roles = roles ?? new HashSet<string> { "Member" },
        Permissions = new HashSet<string>(),
        TypedAttributes = ActorAttributes.Empty,
        AuthScheme = "Test",
        IsAuthenticated = subjectId is not null
    };

    private sealed record TestResource(string Name);

    private sealed class TestController(IActorContextAccessor actorContextAccessor)
        : LearningControllerBase(actorContextAccessor)
    {
        public Guid RequiredUserId() => GetRequiredUserId();
        public Guid? OptionalUserId() => GetOptionalUserId();
        public ActorContext RequiredActor() => GetRequiredActorContext();
        public Guid? CurrentTenantId() => GetCurrentTenantId();
        public ActionResult<TestResource> OkResource(TestResource? resource, string? message = null) =>
            OkOrNotFound(resource, message);
        public ActionResult<string> MapResource(TestResource? resource, string? message = null) =>
            MapOrNotFound(resource, candidate => candidate.Name.ToUpperInvariant(), message);
        public bool CanAccess(Guid userId) => CanAccessUserResource(userId);
    }
}

public sealed class LearningServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLearningCore_ReturnsTheOriginalCollection()
    {
        var services = new ServiceCollection();

        services.AddLearningCore().Should().BeSameAs(services);
    }

    [Theory]
    [InlineData(nameof(LearningServiceCollectionExtensions.AddCourseInfoProvider), typeof(ICourseInfoProvider))]
    [InlineData(nameof(LearningServiceCollectionExtensions.AddEnrollmentInfoProvider), typeof(IEnrollmentInfoProvider))]
    [InlineData(nameof(LearningServiceCollectionExtensions.AddProgressInfoProvider), typeof(IProgressInfoProvider))]
    [InlineData(nameof(LearningServiceCollectionExtensions.AddLearnerProfileProvider), typeof(ILearnerProfileProvider))]
    [InlineData(nameof(LearningServiceCollectionExtensions.AddLearningEventPublisher), typeof(ILearningEventPublisher))]
    [InlineData(nameof(LearningServiceCollectionExtensions.AddLearningCapabilityService), typeof(ILearningCapabilityService))]
    public void ProviderExtensions_RegisterScopedImplementations(string methodName, Type serviceType)
    {
        var services = new ServiceCollection();
        var implementationType = CreateProxyType(serviceType);
        var method = typeof(LearningServiceCollectionExtensions).GetMethod(methodName)!;

        var result = method.MakeGenericMethod(implementationType).Invoke(null, [services]);

        result.Should().BeSameAs(services);
        var descriptor = services.Should().ContainSingle().Subject;
        descriptor.ServiceType.Should().Be(serviceType);
        descriptor.ImplementationType.Should().Be(implementationType);
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    private static Type CreateProxyType(Type serviceType)
    {
        var mockType = typeof(Mock<>).MakeGenericType(serviceType);
        var mock = Activator.CreateInstance(mockType)!;
        var objectProperty = mockType.GetProperties()
            .Single(property => property.Name == nameof(Mock<object>.Object) && property.PropertyType == serviceType);
        return objectProperty.GetValue(mock)!.GetType();
    }
}

public sealed class LearningInformationRecordTests
{
    [Fact]
    public void ProgressInfo_ComputesStartedAndCompletedState()
    {
        var untouched = new ProgressInfo();
        var complete = new ProgressInfo { StartedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow };

        untouched.IsStarted.Should().BeFalse();
        untouched.IsCompleted.Should().BeFalse();
        complete.IsStarted.Should().BeTrue();
        complete.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void EnrollmentInfo_ComputesCompletionState()
    {
        new EnrollmentInfo().IsCompleted.Should().BeFalse();
        new EnrollmentInfo { CompletedAt = DateTime.UtcNow }.IsCompleted.Should().BeTrue();
    }
}
