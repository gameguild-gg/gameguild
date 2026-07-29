using System.Diagnostics;
using System.Reflection;
using GameGuild.CQRS;

namespace GameGuild.API.Setup;

/// <summary>
///     Extension methods for configuring the application layer services.
///     Includes CQRS handler registration and module discovery.
/// </summary>
public static class ApplicationLayerExtensions
{
    #region WebApplicationBuilder Extensions

    /// <summary>
    ///     Adds the application layer services with default options.
    ///     Includes CQRS handlers, domain services, validators, and business logic components.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance</param>
    /// <returns>The WebApplicationBuilder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when the builder is null</exception>
    public static WebApplicationBuilder AddApplicationLayer(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var logger = StartupLogger.Create();
        builder.Services.AddApplicationLayer(logger);

        return builder;
    }

    /// <summary>
    ///     Adds the application layer services with custom options.
    ///     Includes CQRS handlers, domain services, validators, and business logic components.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance</param>
    /// <param name="configureOptions">Action to configure application layer options</param>
    /// <returns>The WebApplicationBuilder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when the builder or configureOptions is null</exception>
    public static WebApplicationBuilder AddApplicationLayer(this WebApplicationBuilder builder,
        Action<ApplicationLayerSetupOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var logger = StartupLogger.Create();
        builder.Services.AddApplicationLayer(logger, configureOptions);

        return builder;
    }

    #endregion

    #region IServiceCollection Extensions

    /// <summary>
    ///     Adds application layer services to the service collection with default options.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="logger">Logger for diagnostics</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationLayer(
        this IServiceCollection services,
        ILogger logger)
    {
        return services.AddApplicationLayer(logger, _ => { });
    }

    /// <summary>
    ///     Adds application layer services to the service collection with custom options.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="logger">Logger for diagnostics</param>
    /// <param name="configureOptions">Action to configure application layer options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationLayer(
        this IServiceCollection services,
        ILogger logger,
        Action<ApplicationLayerSetupOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = new ApplicationLayerSetupOptions();
        configureOptions(options);

        var totalStopwatch = Stopwatch.StartNew();
        logger.LogInformation("Starting application layer setup...");

        var moduleAssemblies = DiscoverModuleAssemblies(options.ModuleConfiguration, logger);
        RegisterCqrsHandlers(services, moduleAssemblies, logger);

        if (options.LogHandlerStatistics)
        {
            LogHandlerStatistics(moduleAssemblies, options.ModuleConfiguration, logger);
        }

        totalStopwatch.Stop();
        logger.LogInformation("Completed application layer setup in {ElapsedMs}ms", totalStopwatch.ElapsedMilliseconds);

        return services;
    }

    #endregion

    /// <summary>
    ///     Discovers assemblies matching the enabled modules configuration.
    /// </summary>
    private static Assembly[] DiscoverModuleAssemblies(ModuleConfiguration config, ILogger logger)
    {
        var stepStopwatch = Stopwatch.StartNew();

        var allAssemblies = DependencyInjection.GetAssembliesByPattern(config.AssemblyPrefix)
            .Where(a => !config.ExcludeTestAssemblies ||
                        !ModuleConfiguration.IsTestAssembly(a.GetName().Name))
            .ToArray();

        var moduleAssemblies = allAssemblies
            .Where(a => config.IsEnabledAssembly(a.GetName().Name))
            .ToArray();

        logger.LogInformation(
            "Discovered {TotalCount} assemblies, {EnabledCount} enabled modules ({Modules}) in {ElapsedMs}ms",
            allAssemblies.Length,
            moduleAssemblies.Length,
            string.Join(", ", config.EnabledModules),
            stepStopwatch.ElapsedMilliseconds);

        return moduleAssemblies;
    }

    /// <summary>
    ///     Registers CQRS handlers from the discovered assemblies.
    /// </summary>
    private static void RegisterCqrsHandlers(IServiceCollection services, Assembly[] assemblies, ILogger logger)
    {
        var stepStopwatch = Stopwatch.StartNew();

        services.AddCqrs(assemblies);

        logger.LogInformation("CQRS registration completed in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    ///     Logs handler and validator statistics per module.
    /// </summary>
    private static void LogHandlerStatistics(Assembly[] assemblies, ModuleConfiguration config, ILogger logger)
    {
        var totalHandlerCount = 0;
        var totalValidatorCount = 0;

        foreach (var assembly in assemblies)
        {
            var stepStopwatch = Stopwatch.StartNew();
            var moduleName = assembly.GetName().Name?.Replace(config.AssemblyPrefix, "") ?? "Unknown";

            var (handlerCount, validatorCount) = CountHandlersAndValidators(assembly);
            totalHandlerCount += handlerCount;
            totalValidatorCount += validatorCount;

            logger.LogInformation(
                "Registered {Module}: {HandlerCount} handlers, {ValidatorCount} validators in {ElapsedMs}ms",
                moduleName, handlerCount, validatorCount, stepStopwatch.ElapsedMilliseconds);
        }

        logger.LogInformation("Completed handler setup: {HandlerCount} handlers, {ValidatorCount} validators",
            totalHandlerCount, totalValidatorCount);
    }

    /// <summary>
    ///     Counts handlers and validators in an assembly.
    /// </summary>
    private static (int handlers, int validators) CountHandlersAndValidators(Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var handlers = types.Count(t => t.GetInterfaces().Any(i =>
            i.IsGenericType && ModuleConfiguration.HandlerTypeNames.Any(ht =>
                i.GetGenericTypeDefinition().Name.Contains(ht))));

        var validators = types.Count(t => t.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition().Name.Contains("IValidator")));

        return (handlers, validators);
    }
}
