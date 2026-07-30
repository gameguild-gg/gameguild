using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Configuration;

/// <summary>
///     Common configuration extension methods that can be used across all modules
/// </summary>
public static class SharedConfigurationExtensions
{
    /// <summary>
    ///     Configures an options type using the provided builder pattern
    /// </summary>
    /// <typeparam name="TOptions">The options type to configure</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <param name="defaultFactory">Factory to create default options</param>
    /// <param name="validator">Optional validation action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection ConfigureOptionsFromSection<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName,
        Func<TOptions> defaultFactory,
        Action<TOptions>? validator = null
    ) where TOptions : class
    {
        var options =
            OptionBuilderUtilities.CreateBindAndValidate(configuration, sectionName, defaultFactory, validator);

        services.AddSingleton(options);

        return services;
    }

    /// <summary>
    ///     Adds configuration with automatic section name detection based on type name
    /// </summary>
    /// <typeparam name="TOptions">The options type to configure</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="defaultFactory">Factory to create default options</param>
    /// <param name="validator">Optional validation action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection ConfigureOptions<TOptions>(this IServiceCollection services,
        IConfiguration configuration, Func<TOptions> defaultFactory, Action<TOptions>? validator = null)
        where TOptions : class
    {
        var sectionName = typeof(TOptions).Name.Replace("Options", "");

        return services.ConfigureOptionsFromSection(configuration, sectionName, defaultFactory, validator);
    }
}