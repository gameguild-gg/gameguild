using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Compliance.Consent;

public static class DependencyInjection
{
    public static IServiceCollection AddConsentModule(this IServiceCollection services)
    {
        services.AddScoped<IConsentPolicyRepository, ConsentPolicyRepository>();
        services.AddScoped<IPolicyVersionRepository, PolicyVersionRepository>();
        services.AddScoped<IUserConsentRepository, UserConsentRepository>();
        services.AddScoped<IDataSubjectRequestRepository, DataSubjectRequestRepository>();
        services.AddScoped<IConsentService, ConsentService>();
        return services;
    }
}
