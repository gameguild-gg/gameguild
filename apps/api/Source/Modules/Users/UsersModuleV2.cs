using GameGuild.Core.Modules;

namespace GameGuild.Modules.Users;

/// <summary>
/// Users module implementing the standardized IModule interface.
/// Manages user entities and related CQRS operations.
/// </summary>
public class UsersModuleV2 : ModuleBase {
    public override string ModuleName => "Users";
    public override string ModuleVersion => "2.0.0";

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration) {
        base.ConfigureServices(services, configuration);

        // Register Users services
        services.AddScoped<IUserService, UserService>();

        // CQRS handlers are automatically registered by assembly scanning
        // TODO: Add explicit handler registration when needed

        return services;
    }

    public override WebApplication MapEndpoints(WebApplication app) {
        base.MapEndpoints(app);

        // Users module doesn't have specific middleware or endpoint mapping currently
        // This can be extended when needed for user-specific routes

        return app;
    }
}

/// <summary>
/// Extension methods for the Users module providing the new standardized pattern.
/// </summary>
public static class UsersModuleV2Extensions {
    /// <summary>
    /// Registers the Users module using the new IModule pattern.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddUsersModuleV2(this IServiceCollection services, IConfiguration configuration) {
        return services.AddModule<UsersModuleV2>(configuration);
    }

    /// <summary>
    /// Maps Users module endpoints using the new IModule pattern.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseUsersModuleV2(this WebApplication app) {
        return app.UseModule<UsersModuleV2>();
    }
}
