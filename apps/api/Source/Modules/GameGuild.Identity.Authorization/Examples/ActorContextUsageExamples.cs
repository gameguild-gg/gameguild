// =============================================================================
// ACTOR CONTEXT - USAGE EXAMPLES
// =============================================================================
// This file demonstrates how to use the new Actor context model for authorization.
// 
// IMPORTANT: This file is for documentation purposes only. It shows patterns
// for using ActorContext in various scenarios.
// =============================================================================

using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Identity.Authorization.Examples;

/// <summary>
///     Example demonstrating how to configure Actor context in the application.
/// </summary>
public static class ActorContextConfigurationExample
{
    /// <summary>
    ///     Example Startup.ConfigureServices() setup.
    /// </summary>
    public static void ConfigureServices(IServiceCollection services)
    {
        // Option 1: Full actor-based contexts (recommended for new projects)
        // This replaces IUserContext/ITenantContext with actor-based adapters
        services.AddActorContextIntegration(useActorBasedContexts: true);

        // Option 2: Just add actor context without replacing existing contexts
        // Useful for gradual migration
        // services.AddActorContextIntegration(useActorBasedContexts: false);
    }

    /// <summary>
    ///     Example Startup.Configure() middleware setup.
    /// </summary>
    public static void Configure(IApplicationBuilder app)
    {
        // Add ActorContext middleware AFTER authentication, BEFORE authorization
        app.UseAuthentication();
        app.UseActorContext();  // <-- Populates ActorContext from claims
        app.UseAuthorization();
    }
}

/// <summary>
///     Example CQRS command handler using ActorContext for authorization.
/// </summary>
public class UpdateProjectCommandHandler
{
    private readonly IActorContextAccessor _actorContextAccessor;

    public UpdateProjectCommandHandler(IActorContextAccessor actorContextAccessor)
    {
        _actorContextAccessor = actorContextAccessor;
    }

    public async Task<Result> HandleAsync(UpdateProjectCommand command, CancellationToken ct)
    {
        var actor = _actorContextAccessor.ActorContext;

        // Check authentication
        if (!actor.IsAuthenticated)
        {
            return Result.Unauthorized("User must be authenticated");
        }

        // Check tenant context
        if (!actor.TenantId.HasValue)
        {
            return Result.BadRequest("Tenant context required");
        }

        // Check permission using pre-evaluated permissions
        if (!actor.HasPermission(Permissions.ProjectWrite))
        {
            return Result.Forbidden($"Missing permission: {Permissions.ProjectWrite}");
        }

        // Check if user is project owner
        var isOwner = actor.SubjectIdAsGuid == command.ProjectOwnerId;
        var canEditAny = actor.HasPermission(Permissions.ProjectAdmin);

        if (!isOwner && !canEditAny)
        {
            return Result.Forbidden("You can only edit your own projects");
        }

        // Proceed with update...
        await Task.CompletedTask;
        return Result.Success();
    }
}

/// <summary>
///     Example authorization service that uses ActorContext.
/// </summary>
public class ProjectAuthorizationService
{
    private readonly IActorContextAccessor _actorContextAccessor;
    private readonly IPermissionService _permissionService;

    public ProjectAuthorizationService(
        IActorContextAccessor actorContextAccessor,
        IPermissionService permissionService)
    {
        _actorContextAccessor = actorContextAccessor;
        _permissionService = permissionService;
    }

    /// <summary>
    ///     Example: Check if actor can perform an action on a resource.
    /// </summary>
    public async Task<bool> AuthorizeAsync(
        string permission,
        Guid? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        var actor = _actorContextAccessor.ActorContext;

        // Anonymous users have no permissions
        if (!actor.IsAuthenticated)
            return false;

        // System admins bypass all checks
        if (actor.IsSystemAdmin)
            return true;

        // For simple permissions, use pre-evaluated permissions from ActorContext
        if (!resourceId.HasValue)
        {
            return actor.HasPermission(permission);
        }

        // For resource-level permissions, check against the database
        var userId = actor.SubjectIdAsGuid;
        var tenantId = actor.TenantId;

        if (!userId.HasValue || !tenantId.HasValue)
            return false;

        // Check resource-specific permission
        var resourcePermission = $"project:{resourceId}:{permission}";
        return await _permissionService.HasTenantPermissionAsync(
            userId.Value,
            tenantId.Value,
            resourcePermission,
            cancellationToken);
    }
}

/// <summary>
///     Example: Creating ActorContext in tests.
/// </summary>
public class ActorContextTestExamples
{
    /// <summary>
    ///     Example: Create a test context for a regular user.
    /// </summary>
    public ActorContext CreateUserContext()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        return ActorContextBuilder.ForUser(userId)
            .WithTenantId(tenantId)
            .WithRole("Member")
            .WithPermission("projects:read")
            .WithPermission("projects:write")
            .WithAttribute("email", "test@example.com")
            .Build();
    }

    /// <summary>
    ///     Example: Create a test context for an admin user.
    /// </summary>
    public ActorContext CreateAdminContext()
    {
        return ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithTenantId(Guid.NewGuid())
            .WithRole("Admin")
            .WithPermission("admin:*")
            .Build();
    }

    /// <summary>
    ///     Example: Create a test context for a service actor.
    /// </summary>
    public ActorContext CreateServiceContext()
    {
        return ActorContextBuilder.ForService("background-job-service", "BackgroundJobService")
            .WithPermission("jobs:execute")
            .WithPermission("projects:read")
            .Build();
    }

    /// <summary>
    ///     Example: Create a test context for a system actor.
    /// </summary>
    public ActorContext CreateSystemContext()
    {
        return ActorContextBuilder.ForSystem("DataMigration")
            .WithTenantId(Guid.NewGuid())
            .Build();
    }

    /// <summary>
    ///     Example: Using the test context in a unit test.
    /// </summary>
    public void ExampleTest()
    {
        // Arrange
        var context = CreateUserContext();
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(context);

        // Act
        var actor = accessor.ActorContext;

        // Assert
        // actor.IsAuthenticated should be true
        // actor.HasPermission("projects:read") should be true
        // actor.HasPermission("admin:*") should be false
    }
}

/// <summary>
///     Example: Background job with system actor context.
/// </summary>
public class DataCleanupJob
{
    private readonly IActorContextAccessor _actorContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public DataCleanupJob(
        IActorContextAccessor actorContextAccessor,
        IServiceProvider serviceProvider)
    {
        _actorContextAccessor = actorContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Set up system actor context for the background job
        var systemContext = ActorContextBuilder.ForSystem("DataCleanupJob")
            .Build();

        _actorContextAccessor.SetActorContext(systemContext);

        try
        {
            // Now all services that inject IActorContextAccessor
            // will see the system actor context
            await PerformCleanupAsync(cancellationToken);
        }
        finally
        {
            // Clear context when done
            _actorContextAccessor.ClearActorContext();
        }
    }

    private Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        // Cleanup logic here...
        return Task.CompletedTask;
    }
}

// Helper types for examples
public record UpdateProjectCommand(Guid ProjectId, string Name, Guid ProjectOwnerId);
public record Result
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    
    public static Result Success() => new() { IsSuccess = true };
    public static Result Unauthorized(string message) => new() { IsSuccess = false, Error = message };
    public static Result BadRequest(string message) => new() { IsSuccess = false, Error = message };
    public static Result Forbidden(string message) => new() { IsSuccess = false, Error = message };
}
