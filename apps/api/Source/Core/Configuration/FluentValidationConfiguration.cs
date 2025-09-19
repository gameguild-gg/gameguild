using FluentValidation;
using GameGuild;
using GameGuild.CQRS;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Configuration for FluentValidation pipeline behaviors
/// </summary>
public static class FluentValidationConfiguration {
    /// <summary>
    /// Configures FluentValidation with CQRS pipeline behavior
    /// </summary>
    public static IServiceCollection SetupFluentValidation(this IServiceCollection services, IConfiguration configuration, FluentValidationOptions? options = null) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        options ??= new FluentValidationOptions();
        options.Validate();

        // Register validation behavior if enabled
        if (options.EnableValidationBehavior) {
            // Use existing ValidationBehavior which integrates with FluentValidation
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        }

        return services;
    }
}
