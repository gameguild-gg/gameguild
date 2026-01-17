using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Xunit;

namespace GameGuild.Identity.Context.UnitTests.Actors;

public class ActorContextTests
{
    [Fact]
    public void HasPermission_Should_Return_True_For_SystemAdmin()
    {
        var context = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = "user",
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string> { "SystemAdmin" },
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };

        context.HasPermission("any:permission").Should().BeTrue();
    }

    [Fact]
    public void HasPermission_Should_Return_True_For_Admin_Wildcard()
    {
        var context = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = "user",
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string> { "admin:*" },
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };

        context.HasPermission("users:read").Should().BeTrue();
    }

    [Fact]
    public void HasAnyPermission_Should_Return_True_When_Any_Match()
    {
        var context = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = "user",
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string> { "projects:read" },
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };

        context.HasAnyPermission("projects:read", "users:read").Should().BeTrue();
        context.HasAnyPermission("users:read").Should().BeFalse();
    }

    [Fact]
    public void SubjectIdAsGuid_Should_Return_Guid_When_Parsable()
    {
        var id = Guid.NewGuid();
        var context = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = id.ToString(),
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };

        context.SubjectIdAsGuid.Should().Be(id);
    }

    [Fact]
    public void IsTenantAdmin_Should_Return_True_For_TenantAdmin()
    {
        var context = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = "user",
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string> { "TenantAdmin" },
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };

        context.IsTenantAdmin.Should().BeTrue();
    }

    private sealed record TestPermission(string Key)
    {
        public override string ToString() => Key;
    }

    [Fact]
    public void HasPermission_Object_Should_Use_ToString()
    {
        var context = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = "user",
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string> { "users:read" },
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };

        context.HasPermission(new TestPermission("users:read")).Should().BeTrue();
    }
}
