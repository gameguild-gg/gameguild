using GameGuild.Modules.DeveloperPortal.Services;
using GameGuild.Modules.DeveloperPortal.Repositories;

namespace GameGuild.Modules.DeveloperPortal;

public static class DeveloperPortalModule
{
    public static IServiceCollection AddDeveloperPortalModule(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IDeveloperPortalService, DeveloperPortalService>();
        services.AddScoped<IOnboardingService, OnboardingService>();

        // Repositories
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IApiUsageLogRepository, ApiUsageLogRepository>();
        services.AddScoped<IDeveloperOnboardingRepository, DeveloperOnboardingRepository>();

        return services;
    }
}
