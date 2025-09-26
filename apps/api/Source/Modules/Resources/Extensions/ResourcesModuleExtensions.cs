namespace GameGuild.Modules.Resources;

public static class ResourcesModuleExtensions
{
    public static IServiceCollection AddResourcesModule(this IServiceCollection services)
    {
        services.AddScoped<IResourceQuotaRepository, ResourceQuotaRepository>();
        services.AddScoped<IResourceQuotaService, ResourceQuotaService>();

        return services;
    }
}
