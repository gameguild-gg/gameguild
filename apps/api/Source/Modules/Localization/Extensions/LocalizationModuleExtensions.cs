namespace GameGuild.Modules.Localization;

/// <summary>
/// Dependency injection extensions for the Localization module.
/// </summary>
public static class LocalizationModuleExtensions
{
    /// <summary>
    /// Registers localization services with the dependency injection container.
    /// </summary>
    public static IServiceCollection AddLocalizationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ILanguageRepository, LanguageRepository>();

        return services;
    }
}
