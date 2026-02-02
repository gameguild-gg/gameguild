using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Learning.Certificates;

/// <summary>
/// Module registration for certificates
/// </summary>
public static class CertificatesModule
{
    /// <summary>
    /// Registers all certificate module services
    /// </summary>
    public static IServiceCollection AddCertificatesModule(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<ICertificateService, CertificateService>();
        services.AddScoped<ICertificateTemplateService, CertificateTemplateService>();

        return services;
    }
}
