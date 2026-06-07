using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Compliance.FERPA;

public static class DependencyInjection
{
    public static IServiceCollection AddFerpaModule(this IServiceCollection services)
    {
        services.AddScoped<IFerpaEducationRecordRepository, FerpaEducationRecordRepository>();
        services.AddScoped<IFerpaDirectoryInformationPolicyRepository, FerpaDirectoryInformationPolicyRepository>();
        services.AddScoped<IFerpaDisclosureConsentRepository, FerpaDisclosureConsentRepository>();
        services.AddScoped<IFerpaDisclosureLogRepository, FerpaDisclosureLogRepository>();
        services.AddScoped<IFerpaInspectionRequestRepository, FerpaInspectionRequestRepository>();
        services.AddScoped<IFerpaService, FerpaService>();
        return services;
    }
}
