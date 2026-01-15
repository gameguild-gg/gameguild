using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameGuild.Localization;

/// <summary>
///     Dependency injection extensions for the Localization module.
/// </summary>
public static class LocalizationModuleExtensions
{
    /// <summary>
    ///     Registers localization services with the dependency injection container.
    /// </summary>
    public static IServiceCollection AddLocalizationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Repository
        services.AddScoped<ILanguageRepository, LanguageRepository>();

        // Localization context (request-scoped for HTTP header/user preference access)
        services.AddScoped<ILocalizationContext, LocalizationContext>();

        // Content sanitizer for XSS prevention (singleton - stateless)
        services.AddSingleton<IContentSanitizer, ContentSanitizer>();

        // Error message localization service
        services.AddScoped<ILocalizedErrorService, LocalizedErrorService>();

        // Translation services
        services.AddScoped<TranslationWorkflowService>();
        services.AddScoped<TranslationMemoryService>();

        return services;
    }

    /// <summary>
    ///     Adds caching support for localization services.
    ///     Call this after AddLocalizationServices to wrap ILocalizationService with caching.
    /// </summary>
    public static IServiceCollection AddLocalizationCaching(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add memory cache if not already registered
        services.AddMemoryCache();

        // Note: If ILocalizationService is registered elsewhere, this decorates it.
        // The CachedLocalizationService wraps the underlying implementation.
        services.TryAddScoped<CachedLocalizationService>();

        return services;
    }
}
