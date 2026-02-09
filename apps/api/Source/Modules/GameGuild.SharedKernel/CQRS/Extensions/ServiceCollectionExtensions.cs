using System.Reflection;
using GameGuild.CQRS.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GameGuild.CQRS;

/// <summary>
///     Extension methods for adding CQRS to the service collection.
/// </summary>
/// <remarks>
///     <b>Lifetime design decisions:</b>
///     <list type="bullet">
///         <item><description>Handlers: <c>Transient</c> — stateless, one instance per resolution (standard mediator/CQRS pattern)</description></item>
///         <item><description>Validators: <c>Scoped</c> — may depend on scoped services (DbContext, HttpContext)</description></item>
///         <item><description>Mediator/ISender/IPublisher: <c>Scoped</c> — tied to request scope for proper DI resolution</description></item>
///         <item><description>NotificationPublisher: <c>Singleton</c> — stateless strategy, safe to share</description></item>
///     </list>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Cached handler interface type definitions for faster lookup - O(1) performance
    /// </summary>
    private static readonly HashSet<Type> RequestHandlerInterfaceTypes =
    [
        typeof(IRequestHandler<,>), typeof(IRequestHandler<>), typeof(IStreamRequestHandler<,>), typeof(ICommandHandler<,>), typeof(ICommandHandler<>), typeof(IQueryHandler<,>)
    ];

    /// <summary>
    ///     Adds CQRS services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assemblies">Assemblies to scan for handlers</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddCqrs(this IServiceCollection services, params Assembly[ ] assemblies) { return services.AddCqrs(_ => { }, assemblies); }

    /// <summary>
    ///     Adds CQRS services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration action</param>
    /// <param name="assemblies">Assemblies to scan for handlers</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddCqrs(this IServiceCollection services, Action<CqrsConfiguration>? configuration, params Assembly[ ] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        if (assemblies.Length == 0) { assemblies = [Assembly.GetCallingAssembly()]; }

        var config = new CqrsConfiguration();
        configuration?.Invoke(config);

        // Register service factory - Use scoped registration to ensure proper service resolution
        services.TryAddScoped<ServiceFactory>(provider => serviceType => provider.GetService(serviceType));
        services.TryAddScoped<IMediator, Mediator>();
        services.TryAddScoped<ISender>(provider => provider.GetRequiredService<IMediator>());
        services.TryAddScoped<IPublisher>(provider => provider.GetRequiredService<IMediator>());

        // Register notification publisher
        services.TryAddSingleton(config.NotificationPublisher);

        // Register handlers and validators
        foreach (var assembly in assemblies)
        {
            services.AddRequestHandlers(assembly);
            services.AddNotificationHandlers(assembly);
            services.AddFluentValidators(assembly);
        }

        return services;
    }

    /// <summary>
    ///     Scans an assembly for implementations of an open-generic interface and registers them.
    ///     This is the single generic scanner used by all CQRS registration methods (DRY).
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assembly">Assembly to scan</param>
    /// <param name="openGenericInterfaceType">The open generic interface type to scan for (e.g., <c>typeof(INotificationHandler&lt;&gt;)</c>)</param>
    /// <param name="lifetime">Service lifetime for registrations</param>
    /// <param name="useTryAdd">If true, uses TryAdd to prevent duplicate registrations</param>
    private static void ScanAndRegister(
        this IServiceCollection services,
        Assembly assembly,
        Type openGenericInterfaceType,
        ServiceLifetime lifetime = ServiceLifetime.Transient,
        bool useTryAdd = false)
    {
        var types = assembly.GetTypes();
        var registrations = new List<(Type serviceType, Type implementationType)>();

        foreach (var type in types)
        {
            if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition) continue;

            var interfaces = type.GetInterfaces();
            for (var i = 0; i < interfaces.Length; i++)
            {
                var @interface = interfaces[i];
                if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == openGenericInterfaceType)
                {
                    registrations.Add((@interface, type));
                }
            }
        }

        foreach (var (serviceType, implementationType) in registrations)
        {
            if (useTryAdd)
            {
                services.TryAdd(new ServiceDescriptor(serviceType, implementationType, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
            }
        }
    }

    /// <summary>
    ///     Scans an assembly for implementations matching any of the given open-generic interfaces and registers them.
    ///     Used for request handlers where multiple interface types (IRequestHandler, ICommandHandler, etc.) are valid.
    /// </summary>
    private static void ScanAndRegisterMultiple(
        this IServiceCollection services,
        Assembly assembly,
        HashSet<Type> openGenericInterfaceTypes,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var types = assembly.GetTypes();
        var registrations = new List<(Type serviceType, Type implementationType)>();

        foreach (var type in types)
        {
            if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition) continue;

            var interfaces = type.GetInterfaces();
            for (var i = 0; i < interfaces.Length; i++)
            {
                var @interface = interfaces[i];
                if (@interface.IsGenericType && openGenericInterfaceTypes.Contains(@interface.GetGenericTypeDefinition()))
                {
                    registrations.Add((@interface, type));
                }
            }
        }

        foreach (var (serviceType, implementationType) in registrations)
        {
            services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
        }
    }

    /// <summary>
    ///     Adds FluentValidation validators from the assembly (Scoped, TryAdd to prevent duplicates).
    ///     These are the validators consumed by <see cref="ValidationBehavior{TRequest,TResponse}"/>.
    /// </summary>
    private static void AddFluentValidators(this IServiceCollection services, Assembly assembly)
    {
        services.ScanAndRegister(assembly, typeof(FluentValidation.IValidator<>), ServiceLifetime.Scoped, useTryAdd: true);
    }

    /// <summary>
    ///     Adds request handlers from the assembly (Transient)
    /// </summary>
    private static void AddRequestHandlers(this IServiceCollection services, Assembly assembly)
    {
        services.ScanAndRegisterMultiple(assembly, RequestHandlerInterfaceTypes);
    }

    /// <summary>
    ///     Adds notification handlers from the assembly (Transient)
    /// </summary>
    private static void AddNotificationHandlers(this IServiceCollection services, Assembly assembly)
    {
        services.ScanAndRegister(assembly, typeof(INotificationHandler<>));
    }

    /// <summary>
    ///     Adds a custom pipeline behavior
    /// </summary>
    /// <typeparam name="TBehavior">Behavior type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddPipelineBehavior<TBehavior>(this IServiceCollection services) where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TBehavior));

        return services;
    }

    /// <summary>
    ///     Adds a custom pipeline behavior with lifetime
    /// </summary>
    /// <typeparam name="TBehavior">Behavior type</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="lifetime">Service lifetime</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddPipelineBehavior<TBehavior>(this IServiceCollection services, ServiceLifetime lifetime) where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(services);
        services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(TBehavior), lifetime));

        return services;
    }

    /// <summary>
    ///     Adds request pre-processors from assembly (Transient)
    /// </summary>
    public static IServiceCollection AddRequestPreProcessors(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);
        services.ScanAndRegister(assembly, typeof(IRequestPreProcessor<>));
        return services;
    }

    /// <summary>
    ///     Adds request post-processors from assembly (Transient)
    /// </summary>
    public static IServiceCollection AddRequestPostProcessors(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);
        services.ScanAndRegister(assembly, typeof(IRequestPostProcessor<,>));
        return services;
    }

    /// <summary>
    ///     Adds exception handlers from assembly (Transient)
    /// </summary>
    public static IServiceCollection AddExceptionHandlers(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);
        services.ScanAndRegister(assembly, typeof(IRequestExceptionHandler<,,>));
        return services;
    }

    /// <summary>
    ///     Adds advanced pipeline behaviors (pre/post processors, exception handling, observability)
    /// </summary>
    public static IServiceCollection AddAdvancedPipelineBehaviors(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ObservabilityBehavior<,>));

        // Register default ObservabilityOptions if not already configured
        services.TryAddSingleton(Microsoft.Extensions.Options.Options.Create(new ObservabilityOptions()));

        return services;
    }

    /// <summary>
    ///     Adds caching pipeline behavior
    /// </summary>
    public static IServiceCollection AddCachingBehavior(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        return services;
    }

    /// <summary>
    ///     Configures assembly scanning with more options
    /// </summary>
    public static IServiceCollection AddCqrsFromAssemblies(this IServiceCollection services, IEnumerable<Assembly> assemblies, Action<CqrsAssemblyConfiguration>? configurator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        var config = new CqrsAssemblyConfiguration();
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
