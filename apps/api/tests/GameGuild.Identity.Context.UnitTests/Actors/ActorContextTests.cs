using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Xunit;

namespace GameGuild.Identity.Context.UnitTests.Actors;

public class ActorContextTests
{
    [Fact]
    public void Anonymous_Should_Expose_Expected_Defaults()
    {
        var context = ActorContext.Anonymous;

        context.ActorKind.Should().Be(ActorKind.Anonymous);
        context.SubjectId.Should().BeNull();
        context.TenantId.Should().BeNull();
        context.Roles.Should().BeEmpty();
        context.Permissions.Should().BeEmpty();
        context.TypedAttributes.Should().Be(ActorAttributes.Empty);
        context.AuthScheme.Should().BeNull();
        context.IsAuthenticated.Should().BeFalse();
        context.IsSystemAdmin.Should().BeFalse();
        context.IsTenantAdmin.Should().BeFalse();
        context.SubjectIdAsGuid.Should().BeNull();
        context.IsMfaVerified.Should().BeFalse();
    }

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
    public void HasAnyPermission_Should_Handle_Empty_SystemAdmin_And_Object_Inputs()
    {
        var regularContext = new ActorContext
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
        var systemAdminContext = regularContext with
        {
            Roles = new HashSet<string> { "SystemAdmin" }
        };
        var tenantAdminContext = regularContext with
        {
            Roles = new HashSet<string> { "Admin" }
        };

        regularContext.HasAnyPermission().Should().BeFalse();
        regularContext.HasAnyPermission(Array.Empty<object>()).Should().BeFalse();
        regularContext.HasAnyPermission(new TestPermission("projects:read"), new TestPermission("users:read")).Should().BeTrue();
        regularContext.HasAnyPermission(new TestPermission("users:read")).Should().BeFalse();
        tenantAdminContext.HasAnyPermission("anything:anything").Should().BeFalse();
        systemAdminContext.HasAnyPermission(new TestPermission("anything:anything")).Should().BeTrue();
    }

    [Fact]
    public void HasAllPermissions_Should_Handle_String_And_Object_Inputs()
    {
        var regularContext = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = "user",
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string> { "users:read", "users:write" },
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };
        var systemAdminContext = regularContext with
        {
            Roles = new HashSet<string> { "SystemAdmin" },
            Permissions = new HashSet<string>()
        };

        regularContext.HasAllPermissions().Should().BeTrue();
        regularContext.HasAllPermissions(Array.Empty<object>()).Should().BeTrue();
        regularContext.HasAllPermissions("users:read", "users:write").Should().BeTrue();
        regularContext.HasAllPermissions("users:read", "users:delete").Should().BeFalse();
        regularContext.HasAllPermissions(new TestPermission("users:read"), new TestPermission("users:write")).Should().BeTrue();
        regularContext.HasAllPermissions(new TestPermission("users:read"), new TestPermission("users:delete")).Should().BeFalse();
        systemAdminContext.HasAllPermissions("anything:anything").Should().BeTrue();
        systemAdminContext.HasAllPermissions(new TestPermission("anything:anything")).Should().BeTrue();
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

    [Fact]
    public void IsTenantAdmin_Should_Return_True_For_Owner_Without_System_Admin_Access()
    {
        var context = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = "user",
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string> { "Owner" },
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };

        context.IsTenantAdmin.Should().BeTrue();
        context.IsSystemAdmin.Should().BeFalse();
    }

    [Fact]
    public void Admin_Role_Should_Be_TenantAdmin_Without_SystemAdmin_Access()
    {
        var context = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = "user",
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string> { "Admin" },
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };

        context.IsSystemAdmin.Should().BeFalse();
        context.HasPermission("anything:anything").Should().BeFalse();
        context.IsTenantAdmin.Should().BeTrue();
    }

    [Fact]
    public void SubjectIdAsGuid_Should_Return_Null_When_Not_Parsable()
    {
        var context = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = "not-a-guid",
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };

        context.SubjectIdAsGuid.Should().BeNull();
    }

    [Fact]
    public void Attributes_And_GetAttribute_Should_Expose_Typed_And_Custom_Values()
    {
        var attributes = new ActorAttributes
        {
            Email = "user@example.com",
            Department = "Finance",
            MfaVerified = true,
            Custom = new Dictionary<string, string>
            {
                ["region"] = "BR"
            }
        };
        var context = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = "user",
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = attributes,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };

#pragma warning disable CS0618
        context.Attributes.Should().ContainKey("email").WhoseValue.Should().Be("user@example.com");
#pragma warning restore CS0618
        context.GetAttribute("region").Should().Be("BR");
        context.GetAttribute("email").Should().Be("user@example.com");
        context.GetAttribute("missing").Should().BeNull();
        context.IsMfaVerified.Should().BeTrue();
    }

    [Fact]
    public void IsInRole_Should_Validate_Argument_And_Match_Role()
    {
        var context = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = "user",
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string> { "Member" },
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };

        context.IsInRole("Member").Should().BeTrue();
        context.IsInRole("Admin").Should().BeFalse();
        var act = () => context.IsInRole(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("role");
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

    [Fact]
    public void HasPermission_Should_Validate_Null_Arguments()
    {
        var context = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = "user",
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        };

        var stringAct = () => context.HasPermission((string)null!);
        var objectAct = () => context.HasPermission((object)null!);

        stringAct.Should().Throw<ArgumentNullException>().WithParameterName("permission");
        objectAct.Should().Throw<ArgumentNullException>().WithParameterName("permission");
    }
}
