using GameGuild.Core.Infrastructure.Permissions;
using GameGuild.Core.Modules;
using GameGuild.Modules.Permissions.Contexts;

namespace GameGuild.Source.Modules.Authorization;

/// <summary>
/// Authorization module implementing the standardized IModule interface.
/// Provides comprehensive authorization services following Clean Architecture and DAC patterns.
/// </summary>
[StandardizedModule("Comprehensive authorization services following Clean Architecture and DAC patterns")]
[ModuleVersion("1.0.0")]
public class AuthorizationModule : ModuleBase
{
    public override string ModuleName => "Authorization";

    public override string ModuleVersion => "1.0.0";

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        base.ConfigureServices(services, configuration);

        // Register Authorization services
        services.AddScoped<IPermissionService, PermissionService>();

        // Register unified permission context
        services.AddScoped<PermissionsContext>();

        // CQRS handlers are automatically registered by assembly scanning

        return services;
    }

    public override WebApplication MapEndpoints(WebApplication app)
    {
        base.MapEndpoints(app);

        // Authorization module middleware should be configured here
        // This can include DAC authorization middleware and context middleware

        return app;
    }
}
