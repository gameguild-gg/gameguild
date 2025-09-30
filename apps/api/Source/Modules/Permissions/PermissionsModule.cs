using GameGuild.Core.Infrastructure.Permissions;
using GameGuild.Core.Modules;

namespace GameGuild.Modules.Permissions;

/// <summary>
/// Permissions module implementing the standardized IModule interface.
/// Provides comprehensive permission management services following Clean Architecture and DAC patterns.
/// </summary>
[StandardizedModule("Comprehensive permission management services following Clean Architecture and DAC patterns")]
[ModuleVersion("1.0.0")]
public class PermissionsModule : ModuleBase
{
    public override string ModuleName => "Permissions";

    public override string ModuleVersion => "1.0.0";

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        base.ConfigureServices(services, configuration);

        // Register Permission services
        services.AddScoped<IPermissionService, PermissionService>();

        // CQRS handlers are automatically registered by assembly scanning

        return services;
    }

    public override WebApplication MapEndpoints(WebApplication app)
    {
        base.MapEndpoints(app);

        // Permissions module endpoints can be configured here

        return app;
    }
}
