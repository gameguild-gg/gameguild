using GameGuild.Identity.Context.Actors;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests;

/// <summary>
///     Unit tests demonstrating the ActorContext model and its usage.
/// </summary>
public class ActorContextTests
{
    [Fact]
    public void ActorContext_Anonymous_IsNotAuthenticated()
    {
        // Arrange & Act
        var context = ActorContext.Anonymous;

        // Assert
        Assert.False(context.IsAuthenticated);
        Assert.Equal(ActorKind.Anonymous, context.ActorKind);
        Assert.Null(context.SubjectId);
        Assert.Null(context.TenantId);
        Assert.Empty(context.Roles);
        Assert.Empty(context.Permissions);
    }

    [Fact]
    public void ActorContextBuilder_ForUser_CreatesValidContext()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var context = ActorContextBuilder.ForUser(userId)
            .WithTenantId(tenantId)
            .WithRole("Member")
            .WithRole("ProjectLead")
            .WithPermission("projects:read")
            .WithPermission("projects:write")
            .WithAttribute("email", "test@example.com")
            .WithAuthScheme("Bearer")
            .Build();

        // Assert
        Assert.True(context.IsAuthenticated);
        Assert.Equal(ActorKind.User, context.ActorKind);
        Assert.Equal(userId.ToString(), context.SubjectId);
        Assert.Equal(userId, context.SubjectIdAsGuid);
        Assert.Equal(tenantId, context.TenantId);
        Assert.Contains("Member", context.Roles);
        Assert.Contains("ProjectLead", context.Roles);
        Assert.True(context.HasPermission("projects:read"));
        Assert.True(context.HasPermission("projects:write"));
        Assert.False(context.HasPermission("admin:*"));
        Assert.Equal("test@example.com", context.GetAttribute("email"));
        Assert.Equal("Bearer", context.AuthScheme);
    }

    [Fact]
    public void ActorContextBuilder_ForService_CreatesValidContext()
    {
        // Arrange
        var serviceId = "my-service-client-id";
        var serviceName = "MyBackgroundService";

        // Act
        var context = ActorContextBuilder.ForService(serviceId, serviceName)
            .WithPermission("jobs:execute")
            .Build();

        // Assert
        Assert.True(context.IsAuthenticated);
        Assert.Equal(ActorKind.Service, context.ActorKind);
        Assert.Equal(serviceId, context.SubjectId);
        Assert.Equal(serviceName, context.GetAttribute("service_name"));
        Assert.True(context.HasPermission("jobs:execute"));
    }

    [Fact]
    public void ActorContextBuilder_ForSystem_HasAdminPermissions()
    {
        // Arrange & Act
        var context = ActorContextBuilder.ForSystem("BackgroundJobProcessor")
            .WithTenantId(Guid.NewGuid())
            .Build();

        // Assert
        Assert.True(context.IsAuthenticated);
        Assert.Equal(ActorKind.System, context.ActorKind);
        Assert.Equal(SystemActor.SystemSubjectId, context.SubjectId);
        Assert.True(context.IsSystemAdmin);
        Assert.True(context.HasPermission("anything:goes"));  // System admins have all permissions
    }

    [Fact]
    public void ActorContext_HasPermission_ReturnsFalseWhenNotPresent()
    {
        // Arrange
        var context = ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithPermission("projects:read")
            .Build();

        // Act & Assert
        Assert.True(context.HasPermission("projects:read"));
        Assert.False(context.HasPermission("admin:delete"));
    }

    [Fact]
    public void ActorContext_HasAnyPermission_WorksCorrectly()
    {
        // Arrange
        var context = ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithPermission("projects:read")
            .Build();

        // Act & Assert
        Assert.True(context.HasAnyPermission("admin:*", "projects:read"));
        Assert.False(context.HasAnyPermission("admin:*", "users:delete"));
    }

    [Fact]
    public void ActorContext_HasAllPermissions_WorksCorrectly()
    {
        // Arrange
        var context = ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithPermission("projects:read")
            .WithPermission("projects:write")
            .Build();

        // Act & Assert
        Assert.True(context.HasAllPermissions("projects:read", "projects:write"));
        Assert.False(context.HasAllPermissions("projects:read", "admin:*"));
    }

    [Fact]
    public void ActorContext_IsInRole_WorksCorrectly()
    {
        // Arrange
        var context = ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithRole("Member")
            .WithRole("ProjectLead")
            .Build();

        // Act & Assert
        Assert.True(context.IsInRole("Member"));
        Assert.True(context.IsInRole("ProjectLead"));
        Assert.False(context.IsInRole("Admin"));
    }

    [Fact]
    public void ActorContext_AdminRole_IsTenantScoped_NotSystemAdmin()
    {
        // Arrange
        var context = ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithRole("Admin")
            .Build();

        // Act & Assert
        Assert.False(context.IsSystemAdmin);
        Assert.True(context.IsTenantAdmin);
    }

    [Fact]
    public void ActorContext_IsSystemAdmin_TrueForSystemAdminRole()
    {
        // Arrange
        var context = ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithRole("SystemAdmin")
            .Build();

        // Act & Assert
        Assert.True(context.IsSystemAdmin);
    }

    [Fact]
    public void ActorContext_IsTenantAdmin_TrueForTenantAdminRole()
    {
        // Arrange
        var context = ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithRole("TenantAdmin")
            .Build();

        // Act & Assert
        Assert.True(context.IsTenantAdmin);
        Assert.False(context.IsSystemAdmin);
    }

    [Fact]
    public void ActorContext_MfaVerified_ReturnsTrueWhenSet()
    {
        // Arrange
        var context = ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithMfaVerified(true)
            .Build();

        // Assert
        Assert.True(context.IsMfaVerified);
    }

    [Fact]
    public void ActorContext_MfaVerified_ReturnsFalseWhenNotSet()
    {
        // Arrange
        var context = ActorContextBuilder.ForUser(Guid.NewGuid())
            .Build();

        // Assert
        Assert.False(context.IsMfaVerified);
    }

    [Fact]
    public void ActorContextAccessor_SetAndGetContext_WorksCorrectly()
    {
        // Arrange
        var accessor = new ActorContextAccessor();
        var userId = Guid.NewGuid();
        var context = ActorContextBuilder.ForUser(userId).Build();

        // Act
        accessor.SetActorContext(context);
        var retrieved = accessor.ActorContext;

        // Assert
        Assert.Equal(context, retrieved);
        Assert.Equal(userId.ToString(), retrieved.SubjectId);
    }

    [Fact]
    public void ActorContextAccessor_ClearContext_ReturnsAnonymous()
    {
        // Arrange
        var accessor = new ActorContextAccessor();
        var context = ActorContextBuilder.ForUser(Guid.NewGuid()).Build();
        accessor.SetActorContext(context);

        // Act
        accessor.ClearActorContext();
        var retrieved = accessor.ActorContext;

        // Assert
        Assert.Equal(ActorContext.Anonymous, retrieved);
        Assert.False(retrieved.IsAuthenticated);
    }

    [Fact]
    public void ActorContextAccessor_DefaultContext_IsAnonymous()
    {
        // Arrange
        var accessor = new ActorContextAccessor();

        // Act
        var context = accessor.ActorContext;

        // Assert
        Assert.Equal(ActorContext.Anonymous, context);
    }
}
