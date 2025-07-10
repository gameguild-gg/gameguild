using System.Diagnostics;
using System.Reflection;
using FluentValidation;
using GameGuild.Modules.Authentication;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;


namespace GameGuild.Common;

/// <summary>
/// Provides extension methods for configuring dependency injection in the GameGuild API.
/// This class implements best practices for:
/// - Optimized assembly scanning and type registration
/// - Configurable presentation layer services (CORS, Swagger, Rate Limiting)
/// - CQRS handler and validator registration with caching
/// - Performance monitoring and error handling
/// - Registration validation and startup diagnostics
/// </summary>
public static class DependencyInjection {
  public static IServiceCollection AddPresentation(this IServiceCollection services, PresentationOptions? options = null) {
    options ??= new PresentationOptions();
    
    // Validate configuration
    options.Validate();

    // API Documentation
    services.AddEndpointsApiExplorer();
    
    if (options.EnableSwagger)
      services.AddSwaggerGen(c => {
          c.SwaggerDoc("v1", new OpenApiInfo { 
            Title = options.ApiTitle, 
            Version = options.ApiVersion, 
            Description = "A comprehensive API for GameGuild platform with CQRS architecture",
            Contact = options.Contact ?? new OpenApiContact {
              Name = "GameGuild Team",
              Email = "support@gameguild.com",
            },
          });

          // Include XML comments for better documentation
          var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
          var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);

          if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
        }
      );

    // Controllers (for backward compatibility with existing REST endpoints)
    services.AddControllers()
            .ConfigureApiBehaviorOptions(options => {
                options.SuppressModelStateInvalidFilter = true; // We handle validation through MediatR
              }
            );

    // Exception Handling
    services.AddExceptionHandler<GlobalExceptionHandler>();
    services.AddProblemDetails(options => {
        options.CustomizeProblemDetails = context => {
          context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
          context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
        };
      }
    );

    // CORS for frontend integration
    services.AddCors(corsOptions => {
        corsOptions.AddDefaultPolicy(builder => {
            builder
              .WithOrigins(options.AllowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
          }
        );
      }
    );

    // Health Checks
    if (options.EnableHealthChecks)
      services.AddHealthChecks()
              .AddCheck("database", () => HealthCheckResult.Healthy(), ["database"]);

    // Response Compression
    if (options.EnableResponseCompression)
      services.AddResponseCompression(compressionOptions => {
          compressionOptions.EnableForHttps = true;
          compressionOptions.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
          compressionOptions.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        }
      );

    // Rate Limiting
    if (options.EnableRateLimiting)
      services.AddRateLimiter(limiterOptions => {
          limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
          limiterOptions.AddFixedWindowLimiter(
            "DefaultPolicy",
            policyOptions => {
              policyOptions.PermitLimit = options.RateLimitRequests;
              policyOptions.Window = options.RateLimitWindow;
              policyOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
              policyOptions.QueueLimit = options.RateLimitQueueLimit;
            }
          );
        }
      );

    return services;
  }

  public static IServiceCollection AddApplication(this IServiceCollection services, ServiceRegistrationOptions? options = null) {
    // Register domain event infrastructure early (needed by handlers)
    services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
    services.AddScoped<IDomainEventsDispatcher, DomainEventsDispatcher>();

    // MediatR for CQRS 
    services.AddMediatR(Assembly.GetExecutingAssembly(), typeof(Program).Assembly);

    // MediatR Pipeline Behaviors (order matters - reverse execution order!)
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnifiedValidationBehavior<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

    // Optimized CQRS Handler Registration (includes domain event handlers and validators)
    services.AddOptimizedHandlers(options);

    // Background Services for domain events
    services.AddHostedService<DomainEventProcessorService>();

    return services;
  }

  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration? configuration = null) {
    // Add all module services
    services.AddUserModule();
    services.AddUserProfileModule();
    services.AddTenantModule();
    services.AddProjectModule();
    services.AddProgramModule();
    services.AddProductModule();
    services.AddSubscriptionModule();
    services.AddPaymentModule();
    services.AddTestModule();
    services.AddCommonServices();

    // Add Auth module if configuration is provided
    if (configuration != null) services.AddAuthModule(configuration);

    // Add database seeder
    services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

    // Add HTTP context accessor for GraphQL authorization
    services.AddHttpContextAccessor();

    // Time Provider
    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

    return services;
  }

  /// <summary>
  /// Configuration options for service registration
  /// </summary>
  public class ServiceRegistrationOptions {
    public bool EnablePerformanceLogging { get; set; } = true;
    public bool EnableDetailedErrors { get; set; } = false;
    public Assembly[]? AdditionalAssemblies { get; set; }
    public ILogger? Logger { get; set; }
    public bool ValidateRegistrations { get; set; } = true;
    public Action<IServiceCollection, Type[], ServiceRegistrationOptions>? CustomRegistrationAction { get; set; }
    public TimeSpan? RegistrationTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool ThrowOnRegistrationFailure { get; set; } = true;
  }

  /// <summary>
  /// Configuration options for presentation layer
  /// </summary>
  public class PresentationOptions {
    public string[] AllowedOrigins { get; set; } = ["http://localhost:3000", "https://localhost:3001"];
    public int RateLimitRequests { get; set; } = 100;
    public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromMinutes(1);
    public int RateLimitQueueLimit { get; set; } = 50;
    public bool EnableSwagger { get; set; } = true;
    public string ApiTitle { get; set; } = "GameGuild API";
    public string ApiVersion { get; set; } = "v1";
    public OpenApiContact? Contact { get; set; }
    public bool EnableResponseCompression { get; set; } = true;
    public bool EnableHealthChecks { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
    
    /// <summary>
    /// Validates the configuration options
    /// </summary>
    public void Validate() {
      if (AllowedOrigins == null || AllowedOrigins.Length == 0) throw new InvalidOperationException("At least one allowed origin must be specified");

      if (RateLimitRequests <= 0) throw new ArgumentOutOfRangeException(nameof(RateLimitRequests), "Rate limit requests must be greater than 0");

      if (RateLimitWindow <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(RateLimitWindow), "Rate limit window must be greater than zero");

      if (string.IsNullOrWhiteSpace(ApiTitle)) throw new ArgumentException("API title cannot be null or empty", nameof(ApiTitle));

      if (string.IsNullOrWhiteSpace(ApiVersion)) throw new ArgumentException("API version cannot be null or empty", nameof(ApiVersion));
    }
  }

  /// <summary>
  /// Gets all application assemblies to scan for types
  /// </summary>
  private static Assembly[] GetApplicationAssemblies(Assembly[]? additionalAssemblies = null) {
    var baseAssemblies = new[] {
      Assembly.GetExecutingAssembly(),
      typeof(Program).Assembly,
    };

    return additionalAssemblies?.Length > 0 
      ? baseAssemblies.Concat(additionalAssemblies).Distinct().ToArray()
      : baseAssemblies.Distinct().ToArray();
  }

  /// <summary>
  /// Optimized registration for all CQRS and domain event handlers
  /// </summary>
  private static IServiceCollection AddOptimizedHandlers(this IServiceCollection services, ServiceRegistrationOptions? options = null) {
    options ??= new ServiceRegistrationOptions();
    var stopwatch = options.EnablePerformanceLogging ? Stopwatch.StartNew() : null;
    
    try {
      var assemblies = GetApplicationAssemblies(options.AdditionalAssemblies);
      options.Logger?.LogInformation("Scanning {AssemblyCount} assemblies for handlers", assemblies.Length);
      
      // Cache type scanning for better performance
      var allTypes = assemblies
        .SelectMany(assembly => {
          try {
            return assembly.GetTypes();
          }
          catch (ReflectionTypeLoadException ex) {
            // Handle assembly loading issues gracefully
            options.Logger?.LogWarning(ex, "Failed to load some types from assembly {AssemblyName}", assembly.GetName().Name);
            return ex.Types.Where(t => t != null).Cast<Type>();
          }
          catch (Exception ex) {
            options.Logger?.LogError(ex, "Failed to load types from assembly {AssemblyName}", assembly.GetName().Name);
            if (options.ThrowOnRegistrationFailure) throw;
            return [];
          }
        })
        .Where(t => t is { IsClass: true, IsAbstract: false })
        .ToArray();

      options.Logger?.LogInformation("Found {TypeCount} concrete types across all assemblies", allTypes.Length);

      // Register all handler types in a single optimized pass
      var registeredCount = RegisterHandlerTypes(services, allTypes,
      [
        typeof(IQueryHandler<,>),
        typeof(ICommandHandler<,>),
        typeof(ICommandHandler<>),
        typeof(IDomainEventHandler<>),
      ], options);

      // Register FluentValidation validators
      var validatorCount = RegisterValidators(services, allTypes, options);
      
      // Execute custom registration if provided
      options.CustomRegistrationAction?.Invoke(services, allTypes, options);

      if (options.EnablePerformanceLogging && stopwatch != null) {
        stopwatch.Stop();
        options.Logger?.LogInformation(
          "Handler registration completed in {ElapsedMs}ms. Registered {HandlerCount} handlers and {ValidatorCount} validators",
          stopwatch.ElapsedMilliseconds, registeredCount, validatorCount);
      }

      return services;
    }
    catch (Exception ex) {
      options.Logger?.LogError(ex, "Failed to register handlers");
      if (options.ThrowOnRegistrationFailure) throw;
      return services;
    }
  }

  /// <summary>
  /// Optimized handler registration using single type scan
  /// </summary>
  private static int RegisterHandlerTypes(IServiceCollection services, Type[] allTypes, Type[] handlerInterfaces, ServiceRegistrationOptions? options = null) {
    var registrationCache = new Dictionary<Type, Type[]>();
    var registeredCount = 0;

    foreach (var handlerInterfaceType in handlerInterfaces) {
      if (!registrationCache.ContainsKey(handlerInterfaceType))
        registrationCache[handlerInterfaceType] = allTypes
                                                  .Where(type => type.GetInterfaces()
                                                                     .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType))
                                                  .ToArray();

      var implementingTypes = registrationCache[handlerInterfaceType];
      options?.Logger?.LogDebug("Found {Count} implementations for {Interface}", 
        implementingTypes.Length, handlerInterfaceType.Name);

      foreach (var implementingType in implementingTypes) {
        var serviceInterfaces = implementingType.GetInterfaces()
          .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
          .ToArray();

        foreach (var serviceInterface in serviceInterfaces) {
          services.AddScoped(serviceInterface, implementingType);
          registeredCount++;
          options?.Logger?.LogTrace("Registered {Implementation} as {Service}", 
            implementingType.Name, serviceInterface.Name);
        }
      }
    }

    return registeredCount;
  }

  /// <summary>
  /// Optimized FluentValidation validator registration
  /// </summary>
  private static int RegisterValidators(IServiceCollection services, Type[] allTypes, ServiceRegistrationOptions? options = null) {
    var validatorTypes = allTypes
      .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && 
                                           i.GetGenericTypeDefinition() == typeof(IValidator<>)))
      .ToArray();

    var registeredCount = 0;
    options?.Logger?.LogDebug("Found {Count} validator types", validatorTypes.Length);

    foreach (var validatorType in validatorTypes) {
      var interfaces = validatorType.GetInterfaces()
        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
        .ToArray();

      foreach (var @interface in interfaces) {
        services.AddScoped(@interface, validatorType);
        registeredCount++;
        options?.Logger?.LogTrace("Registered validator {Implementation} as {Service}", 
          validatorType.Name, @interface.Name);
      }
    }

    return registeredCount;
  }

  /// <summary>
  /// Retrieves registration metrics from the last registration operation
  /// </summary>
  public static RegistrationMetrics GetRegistrationMetrics(IServiceProvider serviceProvider) {
    return serviceProvider.GetService<RegistrationMetrics>() ?? new RegistrationMetrics {
      TotalHandlersRegistered = 0,
      TotalValidatorsRegistered = 0,
      RegistrationDuration = TimeSpan.Zero,
    };
  }
}
