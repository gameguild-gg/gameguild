using Microsoft.AspNetCore.Routing;
using GameGuild.Projects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.LaunchPad;

public sealed class LaunchPadModule : ModuleBase
{
    public override string Name => "LaunchPad";

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IProjectLifecycleParticipant, LaunchPadProjectLifecycleParticipant>();
        services.AddScoped<ILaunchPadAuthorizationService, LaunchPadAuthorizationService>();
        return services;
    }

    public override IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints;
}

public static class LaunchPadModuleExtensions
{
    public static IServiceCollection AddLaunchPadModule(this IServiceCollection services)
        => services.AddModule<LaunchPadModule>(new ConfigurationBuilder().Build());
}
