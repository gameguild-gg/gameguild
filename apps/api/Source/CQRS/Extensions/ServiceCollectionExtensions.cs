using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameGuild.CQRS;

/// <summary>
/// Extension methods for adding CQRS to the service collection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Cached handler interface type definitions for faster lookup - O(1) performance
    /// </summary>
    private static readonly HashSet<Type> HandlerInterfaceTypes = new HashSet<Type>
    {
        typeof(IRequestHandler<,>),
        typeof(IRequestHandler<>),
        typeof(IStreamRequestHandler<,>),
        typeof(ICommandHandler<,>),
        typeof(ICommandHandler<>),
        typeof(IQueryHandler<,>)
    };

    /// <summary>
    /// Adds CQRS services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assemblies">Assemblies to scan for handlers</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddCQRS(this IServiceCollection services, params Assembly[] assemblies) { return services.AddCQRS(_ => { }, assemblies); }

    /// <summary>
    ///  Adds CQRS services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration action</param>
    /// <param name="assemblies">Assemblies to scan for handlers</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddCQRS(this IServiceCollection services, Action<CqrsConfiguration>? configuration, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        if (assemblies.Length == 0)
        {
            assemblies =
            [
                Assembly.GetCallingAssembly()
            ];
        }

        var config = new CqrsConfiguration();
        configuration?.Invoke(config);

        // Register service factory
        services.TryAddSingleton<ServiceFactory>(provider => serviceType => provider.GetService(serviceType));
        services.TryAddScoped<IMediator, Mediator>();
        services.TryAddScoped<ISender>(provider => provider.GetRequiredService<IMediator>());
        services.TryAddScoped<IPublisher>(provider => provider.GetRequiredService<IMediator>());

        // Register notification publisher
        services.TryAddSingleton(config.NotificationPublisher);

        // Register handlers
        foreach (var assembly in assemblies)
        {
            services.AddRequestHandlers(assembly);
            services.AddNotificationHandlers(assembly);
        }

        return services;
    }

    /// <summary>
    ///  Adds request handlers from the assembly with optimized O(n) scanning
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assembly">Assembly to scan</param>
    /// <returns>Service collection</returns>
    private static IServiceCollection AddRequestHandlers(this IServiceCollection services, Assembly assembly)
    {
        var types = assembly.GetTypes();
        var handlerRegistrations = new List<(Type serviceType, Type implementationType)>();

        // O(n) scan instead of O(n²) with optimized filtering
        foreach (var type in types)
        {
            // Quick filter - avoid expensive checks on obviously invalid types
            if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
            {
                continue;
            }

            // Get interfaces once and cache the array for reuse - more efficient than multiple LINQ calls
            var interfaces = type.GetInterfaces();

            // Use for loop instead of LINQ for better performance
            for (var i = 0; i < interfaces.Length; i++)
            {
                var @interface = interfaces[i];

                if (@interface.IsGenericType)
                {
                    var genericDefinition = @interface.GetGenericTypeDefinition();

                    // O(1) lookup instead of multiple equality checks
                    if (HandlerInterfaceTypes.Contains(genericDefinition))
                    {
                        handlerRegistrations.Add((@interface, type));
                    }
                }
            }
        }

        // Batch register all handlers - more efficient than individual registrations
        foreach ((var serviceType, var implementationType) in handlerRegistrations)
        {
            services.AddTransient(serviceType, implementationType);
        }

        return services;
    }

    /// <summary>
    ///  Adds notification handlers from the assembly with optimized O(n) scanning
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assembly">Assembly to scan</param>
    /// <returns>Service collection</returns>
    private static IServiceCollection AddNotificationHandlers(this IServiceCollection services, Assembly assembly)
    {
        var types = assembly.GetTypes();
        var handlerRegistrations = new List<(Type serviceType, Type implementationType)>();
        var notificationHandlerType = typeof(INotificationHandler<>);

        // O(n) scan with optimized filtering
        foreach (var type in types)
        {
            // Quick filter - avoid expensive checks on obviously invalid types
            if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
            {
                continue;
            }

            // Get interfaces once and cache for reuse
            var interfaces = type.GetInterfaces();

            // Use for loop for better performance than LINQ
            for (var i = 0; i < interfaces.Length; i++)
            {
                var @interface = interfaces[i];

                if (@interface.IsGenericType &&
                    @interface.GetGenericTypeDefinition() == notificationHandlerType)
                {
                    handlerRegistrations.Add((@interface, type));
                }
            }
        }

        // Batch register all handlers
        foreach ((var serviceType, var implementationType) in handlerRegistrations)
        {
            services.AddTransient(serviceType, implementationType);
        }

        return services;
    }

    /// <summary>
    ///  Adds a custom pipeline behavior
    /// </summary>
    /// <typeparam name="TBehavior">Behavior type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddPipelineBehavior<TBehavior>(this IServiceCollection services)
        where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TBehavior));

        return services;
    }

    /// <summary>
    ///  Adds a custom pipeline behavior with lifetime
    /// </summary>
    /// <typeparam name="TBehavior">Behavior type</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="lifetime">Service lifetime</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddPipelineBehavior<TBehavior>(this IServiceCollection services, ServiceLifetime lifetime)
        where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(services);
        services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(TBehavior), lifetime));

        return services;
    }

    /// <summary>
    ///  Adds request pre-processors from assembly with optimized O(n) scanning
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assembly">Assembly to scan</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddRequestPreProcessors(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var types = assembly.GetTypes();
        var handlerRegistrations = new List<(Type serviceType, Type implementationType)>();
        var preProcessorType = typeof(IRequestPreProcessor<>);

        // O(n) optimized scan
        foreach (var type in types)
        {
            if (!type.IsClass || type.IsAbstract) continue;

            var interfaces = type.GetInterfaces();
            for (var i = 0; i < interfaces.Length; i++)
            {
                var @interface = interfaces[i];
                if (@interface.IsGenericType &&
                    @interface.GetGenericTypeDefinition() == preProcessorType)
                {
                    handlerRegistrations.Add((@interface, type));
                }
            }
        }

        // Batch register
        foreach ((var serviceType, var implementationType) in handlerRegistrations)
        {
            services.AddTransient(serviceType, implementationType);
        }

        return services;
    }

    /// <summary>
    ///  Adds request post-processors from assembly with optimized O(n) scanning
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assembly">Assembly to scan</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddRequestPostProcessors(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var types = assembly.GetTypes();
        var handlerRegistrations = new List<(Type serviceType, Type implementationType)>();
        var postProcessorType = typeof(IRequestPostProcessor<,>);

        // O(n) optimized scan
        foreach (var type in types)
        {
            if (!type.IsClass || type.IsAbstract) continue;

            var interfaces = type.GetInterfaces();
            for (var i = 0; i < interfaces.Length; i++)
            {
                var @interface = interfaces[i];
                if (@interface.IsGenericType &&
                    @interface.GetGenericTypeDefinition() == postProcessorType)
                {
                    handlerRegistrations.Add((@interface, type));
                }
            }
        }

        // Batch register
        foreach ((var serviceType, var implementationType) in handlerRegistrations)
        {
            services.AddTransient(serviceType, implementationType);
        }

        return services;
    }

    /// <summary>
    ///  Adds exception handlers from assembly with optimized O(n) scanning
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assembly">Assembly to scan</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddExceptionHandlers(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var types = assembly.GetTypes();
        var handlerRegistrations = new List<(Type serviceType, Type implementationType)>();
        var exceptionHandlerType = typeof(IRequestExceptionHandler<,,>);

        // O(n) optimized scan
        foreach (var type in types)
        {
            if (!type.IsClass || type.IsAbstract) continue;

            var interfaces = type.GetInterfaces();
            for (var i = 0; i < interfaces.Length; i++)
            {
                var @interface = interfaces[i];
                if (@interface.IsGenericType &&
                    @interface.GetGenericTypeDefinition() == exceptionHandlerType)
                {
                    handlerRegistrations.Add((@interface, type));
                }
            }
        }

        // Batch register
        foreach ((var serviceType, var implementationType) in handlerRegistrations)
        {
            services.AddTransient(serviceType, implementationType);
        }

        return services;
    }

    /// <summary>
    ///  Adds advanced pipeline behaviors (pre/post processors, exception handling, caching)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddAdvancedPipelineBehaviors(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionBehavior<,>));

        return services;
    }

    /// <summary>
    ///  Adds caching pipeline behavior
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddCachingBehavior(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        return services;
    }

    /// <summary>
    ///  Configures assembly scanning with more options
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assemblies">Assemblies to scan</param>
    /// <param name="configurator">Configuration action</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddCQRSFromAssemblies(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        Action<CQRSAssemblyConfiguration>? configurator = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        var config = new CQRSAssemblyConfiguration();
        configurator?.Invoke(config);

        foreach (var assembly in assemblies)
        {
            if (config.IncludeRequestHandlers) services.AddRequestHandlers(assembly);

            if (config.IncludeNotificationHandlers) services.AddNotificationHandlers(assembly);

            if (config.IncludePreProcessors) services.AddRequestPreProcessors(assembly);

            if (config.IncludePostProcessors) services.AddRequestPostProcessors(assembly);

            if (config.IncludeExceptionHandlers) services.AddExceptionHandlers(assembly);
        }

        return services;
    }
}
