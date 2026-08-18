using Microsoft.AspNetCore.Routing;
using GameGuild.Projects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.TestingLab;

/// <summary>
/// TestingLab module implementing the standardized IModule interface.
/// Provides comprehensive testing lab services following Clean Architecture.
/// </summary>
public class TestingLabModule : ModuleBase
{
    public override string Name => "TestingLab";

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register TestingLab repositories
        services.AddScoped<ITestingRequestRepository, TestingRequestRepository>();
        services.AddHostedService<Services.TestingEventReminderService>();
        services.AddScoped<ITestingLocationRepository, TestingLocationRepository>();

        // Register TestingLab services (pre-existing focused services used by CQRS handlers)
        services.AddScoped<ITestingRequestService, TestingRequestService>();
        services.AddScoped<ITestingSessionService, TestingSessionService>();

        // Register focused operation services (extracted from monolithic TestService)
        services.AddScoped<ITestingRequestOperations, TestingRequestOperationsService>();
        services.AddScoped<ITestingSessionOperations, TestingSessionOperationsService>();
        services.AddScoped<ITestingParticipantOperations, TestingParticipantOperationsService>();
        services.AddScoped<ITestingFeedbackOperations, TestingFeedbackOperationsService>();
        services.AddScoped<ITestingLocationOperations, TestingLocationOperationsService>();
        services.AddScoped<ITestingLabPermissionService, TestingLabPermissionService>();
        services.AddScoped<IProjectLifecycleParticipant, TestingLabProjectLifecycleParticipant>();

        // Register composite ITestService for backward compatibility (GraphQL resolvers)
        services.AddScoped<ITestService, TestService>();

        // CQRS handlers are automatically registered by assembly scanning

        return services;
    }

    public override IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // TestingLab module doesn't have specific middleware currently
        // This can be extended when needed for testing-specific routes or middleware

        return endpoints;
    }
}

/// <summary>
/// Extension methods for the TestingLab module providing the standardized pattern.
/// </summary>
public static class TestingLabModuleExtensions
{
    /// <summary>
    /// Registers the TestingLab module using the IModule pattern.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTestingLabModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddModule<TestingLabModule>(configuration);
    }

    /// <summary>
    /// Maps TestingLab module endpoints using the IModule pattern.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder</param>
    /// <returns>The endpoint route builder for chaining</returns>
    public static IEndpointRouteBuilder UseTestingLabModule(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.UseModule<TestingLabModule>();
    }
}
