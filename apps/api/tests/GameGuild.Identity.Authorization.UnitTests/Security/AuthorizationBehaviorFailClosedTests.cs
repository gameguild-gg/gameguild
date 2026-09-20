using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Moq;
using Xunit;
// The platform attribute is aliased because this file also declares a decoy attribute
// with the same simple name to prove the behavior ignores name-only matches.
using PlatformAuthorizeRequest = GameGuild.Identity.Authorization.AuthorizeRequestAttribute;

namespace GameGuild.Identity.Authorization.UnitTests.Security;

/// <summary>
///     Fail-closed guarantees for <see cref="AuthorizationBehavior{TRequest,TResponse}"/>:
///     typed attribute resolution (no name-based matching) and denial of unknown
///     permission patterns during permission-to-access-level mapping.
/// </summary>
public sealed class AuthorizationBehaviorFailClosedTests
{
    // ─── Typed attribute resolution ───

    [Fact]
    public async Task AuthorizeRequestAttribute_ByConcreteType_IsHonored()
    {
        var (accessor, acl) = SetupActor(permissions: ["tenant:allowed"]);

        var behavior = new AuthorizationBehavior<TypedTenantRequest, string>(accessor.Object, acl.Object);

        var result = await behavior.Handle(
            new TypedTenantRequest(),
            () => Task.FromResult("ok"),
            CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Attribute_OnlyMatchingByName_InOtherNamespace_IsIgnored()
    {
        // Regression: the behavior used to match attributes by the NAME
        // "AuthorizeRequestAttribute" via reflection, so any attribute with that
        // name — from any namespace — triggered authorization with default values.
        // It must only honor the platform's own typed attribute.
        var (accessor, acl) = SetupActor(permissions: Array.Empty<string>());

        var behavior = new AuthorizationBehavior<DecoyNamedAttributeRequest, string>(accessor.Object, acl.Object);

        // The decoy attribute carries Permission="tenant:denied-permission"; the actor has no
        // permissions. If the decoy were honored the pipeline would throw. Being ignored,
        // the request proceeds without an authorization decision.
        var result = await behavior.Handle(
            new DecoyNamedAttributeRequest(),
            () => Task.FromResult("passed-through"),
            CancellationToken.None);

        result.Should().Be("passed-through");
        acl.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DerivedAuthorizeRequestAttribute_IsStillHonored()
    {
        var (accessor, acl) = SetupActor(permissions: Array.Empty<string>());

        var behavior = new AuthorizationBehavior<DerivedAttributeHolderRequest, string>(accessor.Object, acl.Object);

        var act = () => behavior.Handle(
            new DerivedAttributeHolderRequest(),
            () => Task.FromResult("ok"),
            CancellationToken.None);

        // Derived attribute carries a permission the actor lacks -> denied.
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ─── Fail-closed permission mapping ───

    [Theory]
    [InlineData("read")]
    [InlineData("view")]
    [InlineData("get")]
    [InlineData("list")]
    public void MapPermissionToAccessLevel_KnownReadPatterns_MapToRead(string permission)
    {
        MapPermission(permission)
            .Should().Be(AccessLevel.Read);
    }

    [Theory]
    [InlineData("write")]
    [InlineData("edit")]
    [InlineData("update")]
    [InlineData("create")]
    public void MapPermissionToAccessLevel_KnownWritePatterns_MapToWrite(string permission)
    {
        MapPermission(permission)
            .Should().Be(AccessLevel.Write);
    }

    [Theory]
    [InlineData("manage")]
    [InlineData("admin")]
    [InlineData("delete")]
    [InlineData("remove")]
    public void MapPermissionToAccessLevel_KnownAdminPatterns_MapToAdmin(string permission)
    {
        MapPermission(permission)
            .Should().Be(AccessLevel.Admin);
    }

    [Theory]
    [InlineData("custom")]
    [InlineData("surprise")]
    [InlineData("documents:whatever")]
    public void MapPermissionToAccessLevel_UnknownPermission_FailsClosedWithExplicitError(string permission)
    {
        // Regression: unknown permissions previously defaulted to AccessLevel.Write,
        // granting write access the caller never declared.
        var act = () => MapPermission(permission);

        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage($"*{permission}*");
    }

    [Fact]
    public async Task ResourceLevelCheck_WithUnknownPermission_DeniesInsteadOfDefaultingToWrite()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var actor = new ActorContext
        {
            IsAuthenticated = true,
            ActorKind = ActorKind.User,
            SubjectId = userId.ToString(),
            TenantId = tenantId,
            Permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            Roles = new HashSet<string>()
        };
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(a => a.ActorContext).Returns(actor);
        var acl = new Mock<IAccessControlListService>();

        var behavior = new AuthorizationBehavior<ResourceLevelUnknownPermissionRequest, string>(accessor.Object, acl.Object);

        var act = () => behavior.Handle(
            new ResourceLevelUnknownPermissionRequest(Guid.NewGuid()),
            () => Task.FromResult("ok"),
            CancellationToken.None);

        // Denial must happen before any ACL lookup — the access level cannot be
        // determined, so no evaluation may proceed.
        var assertion = await act.Should().ThrowAsync<UnauthorizedAccessException>();
        assertion.WithMessage("*unknown permission*");
        acl.VerifyNoOtherCalls();
    }

    // ─── Setup ───

    private static AccessLevel MapPermission(string permission)
    {
        var method = typeof(AuthorizationBehavior<MapProbeRequest, string>)
            .GetMethod("MapPermissionToAccessLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull();
        try
        {
            return (AccessLevel)method!.Invoke(null, [permission])!;
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Surface the private method's own exception (the fail-closed denial)
            // instead of the reflection wrapper.
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // unreachable
        }
    }

    private static (Mock<IActorContextAccessor> Accessor, Mock<IAccessControlListService> Acl) SetupActor(
        string[] permissions)
    {
        var actor = new ActorContext
        {
            IsAuthenticated = true,
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = Guid.NewGuid(),
            Permissions = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase),
            Roles = new HashSet<string>()
        };
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(a => a.ActorContext).Returns(actor);
        return (accessor, new Mock<IAccessControlListService>());
    }

    // ─── Request fixtures ───

    [PlatformAuthorizeRequest("tenant:allowed")]
    public sealed record TypedTenantRequest : IRequestBase;

    /// <summary>
    ///     Attribute that only matches the platform attribute by NAME (different type,
    ///     different namespace). The behavior must ignore it.
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    private sealed class AuthorizeRequestAttribute(string permission) : Attribute
    {
        public string Permission { get; } = permission;
    }

    // Decorated with the DECOY attribute (the nested type that only shares the platform
    // attribute's simple name) — deliberately NOT with the platform attribute itself.
    [AuthorizeRequest("tenant:denied-permission")]
    private sealed record DecoyNamedAttributeRequest : IRequestBase;

    private sealed class ManageEverythingAttribute : PlatformAuthorizeRequest
    {
        public ManageEverythingAttribute() : base("tenant:not-granted") { }
    }

    [ManageEverythingAttribute]
    private sealed record DerivedAttributeHolderRequest : IRequestBase;

    [PlatformAuthorizeRequest("surprise", ResourceType = "document", ResourceIdProperty = "ResourceId")]
    public sealed record ResourceLevelUnknownPermissionRequest(Guid ResourceId) : IRequestBase;

    public sealed record MapProbeRequest : IRequestBase;
}
