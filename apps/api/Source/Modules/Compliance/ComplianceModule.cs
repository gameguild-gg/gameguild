using Microsoft.Extensions.DependencyInjection;
using GameGuild.Modules.Compliance.Services;
using GameGuild.Modules.Compliance.Repositories;

namespace GameGuild.Modules.Compliance;

public static class ComplianceModule
{
    public static IServiceCollection AddComplianceModule(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<IComplianceService, ComplianceService>();
        services.AddScoped<IConsentService, ConsentService>();

        // Register repositories
        services.AddScoped<IConsentPolicyRepository, ConsentPolicyRepository>();
        services.AddScoped<IPolicyVersionRepository, PolicyVersionRepository>();
        services.AddScoped<IUserConsentRepository, UserConsentRepository>();
        services.AddScoped<IComplianceAuditRepository, ComplianceAuditRepository>();

        return services;
    }
}
