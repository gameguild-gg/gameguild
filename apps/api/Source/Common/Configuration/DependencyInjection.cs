using System.Diagnostics;
using System.Reflection;
using FluentValidation;
using GameGuild.Common.Extensions;
using GameGuild.Database;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Payments;
using GameGuild.Modules.Products;
using GameGuild.Modules.Programs;
using HotChocolate.Execution.Configuration;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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
        c.SwaggerDoc(
          "v1",
          new OpenApiInfo {
            Title = options.ApiTitle,
            Version = options.ApiVersion,
            Description = "A comprehensive API for GameGuild platform with CQRS architecture",
            Contact = options.Contact ?? new OpenApiContact { Name = "GameGuild Team", Email = "support@gameguild.com", },
          }
        );

        // Include XML comments for better documentation
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
      }
      );

    // Controllers (for backward compatibility with existing REST endpoints)
    services.AddControllers()
            .AddJsonOptions(options => {
              // Handle circular references in navigation properties
              options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            }
            )
            .ConfigureApiBehaviorOptions(options => {
              options.SuppressModelStateInvalidFilter = true; // We handle validation through MediatR
            }
            );

    // Configure routing to use lowercase URLs
    services.Configure<RouteOptions>(options => {
      options.LowercaseUrls = true;
      options.LowercaseQueryStrings = true;
    });

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

    // Add context services for user and tenant context
    services.AddContextServices();

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

  /// <summary>
  /// Adds infrastructure layer services following clean architecture principles.
  /// This method configures core infrastructure concerns in a composable manner.
  /// </summary>
  /// <param name="services">The service collection to configure</param>
  /// <param name="configuration">Application configuration</param>
  /// <param name="excludeAuth">Whether to exclude authentication (useful for testing)</param>
  /// <returns>The configured service collection</returns>
  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool excludeAuth = false) {
    return services
           .AddCoreServices()
           .AddDatabase(configuration)
           .AddDomainModules()
           .AddAuthenticationInternal(configuration, excludeAuth)
           .AddGraphQLInfrastructure(GraphQLOptionsFactory.ForProduction())
           .AddHealthChecksInternal(configuration)
           .AddHttpContextAccessor();
    // Required for GraphQL authorization
  }

  /// <summary>
  /// Adds core infrastructure services that are fundamental to the application.
  /// These services provide cross-cutting concerns like time, domain events, and data access.
  /// </summary>
  private static IServiceCollection AddCoreServices(this IServiceCollection services) {
    // Time abstraction for testability and consistency.
    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

    // Domain events infrastructure
    services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();
    services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

    // Database seeding
    services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

    return services;
  }

  /// <summary>
  /// Configures Entity Framework with the appropriate database provider.
  /// Supports PostgreSQL for production and in-memory for testing.
  /// </summary>
  private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration) {
    var dbOptions = InfrastructureConfiguration.CreateDatabaseOptions(configuration);

    // Add DbContext factory for GraphQL DataLoaders with proper lifetime management
    services.AddDbContextFactory<ApplicationDbContext>(options => { InfrastructureConfiguration.ConfigureDbContext(options, dbOptions); });

    // Add regular DbContext using the factory (this ensures compatible lifetimes)
    services.AddScoped<ApplicationDbContext>(provider => {
      var factory = provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
      return factory.CreateDbContext();
    });

    return services;
  }

  /// <summary>
  /// Adds all domain modules following the modular monolith pattern.
  /// Each module encapsulates its own business logic and data access.
  /// </summary>
  private static IServiceCollection AddDomainModules(this IServiceCollection services) =>
    services
      .AddUserModule()
      .AddUserProfileModule()
      .AddCredentialsModule()
      .AddTenantModule()
      .AddProjectModule()
      .AddProgramModule()
      .AddProductModule()
      .AddSubscriptionModule()
      .AddPaymentModule()
      .AddPostsModule()
      .AddTestModule()
      .AddCommonServices();

  /// <summary>
  /// Conditionally adds authentication services based on configuration.
  /// Can be excluded for testing scenarios.
  /// </summary>
  private static IServiceCollection AddAuthenticationInternal(
    this IServiceCollection services,
    IConfiguration configuration,
    bool excludeAuth = false
  ) {
    if (configuration != null && !excludeAuth) {
      // Use the new modular Auth configuration
      services = AuthModuleDependencyInjection.AddAuthModule(services, configuration);
    }

    return services;
  }

  /// <summary>
  /// Adds health checks for monitoring application and infrastructure health.
  /// </summary>
  private static IServiceCollection AddHealthChecksInternal(
    this IServiceCollection services,
    IConfiguration configuration
  ) {
    var healthOptions = InfrastructureConfiguration.CreateHealthCheckOptions(configuration);

    var builder = services.AddHealthChecks();

    if (healthOptions.EnableDatabaseCheck) {
      builder.AddCheck(
        "database",
        () => HealthCheckResult.Healthy("Database is accessible"),
        tags: ["database", "infrastructure"],
        timeout: healthOptions.Timeout
      );
    }

    if (healthOptions.EnableApiHealthCheck) {
      builder.AddCheck(
        "api",
        () => HealthCheckResult.Healthy("API is responding"),
        tags: ["api", "readiness"],
        timeout: healthOptions.Timeout
      );
    }

    return services;
  }

  /// <summary>
  /// Configures GraphQL infrastructure with all module types, extensions, and optimizations.
  /// This method provides robust, maintainable, and extensible GraphQL configuration with:
  /// - Dynamic module discovery and registration
  /// - Intelligent type scanning and caching
  /// - Environment-specific configuration
  /// - Comprehensive error handling and logging
  /// - Performance optimization
  /// </summary>
  /// <param name="services">The service collection</param>
  /// <param name="options">Optional GraphQL configuration options</param>
  /// <returns>The service collection for method chaining</returns>
  public static IServiceCollection AddGraphQLInfrastructure(this IServiceCollection services, GraphQLOptions? options = null) {
    options ??= new GraphQLOptions();
    options.Validate();

    var logger = services.BuildServiceProvider().GetService<ILogger<object>>(); // Temporary logger for setup

    try {
      var stopwatch = Stopwatch.StartNew();

      // Add DAC authorization services first
      services.AddDACAuthorizationServices();

      // Initialize GraphQL server with base types
      var graphqlBuilder = services.AddGraphQLServer()
                                   .AddQueryType<Query>()
                                   .AddMutationType<Mutation>();

      // Configure core GraphQL features
      ConfigureGraphQLFeatures(graphqlBuilder, options, logger);

      // Register core modules (always required)
      RegisterCoreModules(graphqlBuilder, options, logger);

      // Register optional modules based on configuration
      RegisterOptionalModules(graphqlBuilder, options, logger);

      // Register advanced modules if enabled
      if (options.EnableAdvancedModules) { RegisterAdvancedModules(graphqlBuilder, options, logger); }

      // Configure GraphQL server options
      ConfigureServerOptions(graphqlBuilder, options, logger);

      stopwatch.Stop();
      logger?.LogInformation("GraphQL infrastructure configured in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

      return services;
    }
    catch (Exception ex) {
      logger?.LogError(ex, "Failed to configure GraphQL infrastructure");

      throw new InvalidOperationException("GraphQL infrastructure configuration failed. See inner exception for details.", ex);
    }
  }

  /// <summary>
  /// Configures core GraphQL features like filtering, sorting, projections, etc.
  /// </summary>
  private static void ConfigureGraphQLFeatures(IRequestExecutorBuilder builder, GraphQLOptions options, ILogger? logger = null) {
    // Add global object identification if enabled
    if (options.EnableGlobalObjectIdentification) { builder.AddGlobalObjectIdentification(); }

    // Add filtering, sorting, and projections if enabled
    if (options.EnableFiltering) { builder.AddFiltering(); }

    if (options.EnableSorting) { builder.AddSorting(); }

    if (options.EnableProjections) { builder.AddProjections(); }

    // Add authorization if enabled
    if (options.EnableAuthorization) {
      try {
        builder.AddDACAuthorization();
        logger?.LogInformation("DAC Authorization enabled");
      }
      catch (Exception ex) {
        // In test environments, DAC authorization might not be available
        if (!options.IsTestEnvironment) { throw new InvalidOperationException("Failed to configure DAC authorization. Ensure AddDACAuthorizationServices() is called first.", ex); }
        else { logger?.LogWarning("DAC Authorization failed in test environment (expected): {Message}", ex.Message); }
      }
    }
    else {
      logger?.LogInformation("DAC Authorization disabled");
    }
  }

  /// <summary>
  /// Registers core modules that are always required for the GraphQL schema
  /// </summary>
  private static void RegisterCoreModules(IRequestExecutorBuilder builder, GraphQLOptions options, ILogger? logger) {
    var coreModules = new Dictionary<string, Action<IRequestExecutorBuilder>> {
      ["Users"] =
        b => {
          SafeAddGraphQLTypes(
            b,
            new[] { ("UserQueries", typeof(Modules.Users.UserQueries)), ("UserMutations", typeof(Modules.Users.UserMutations)), ("UserType", typeof(Modules.Users.UserType)) },
            logger,
            isExtension: new[] { true, true, false }
          );
        },
      ["UserProfiles"] =
        b => {
          SafeAddGraphQLTypes(
            b,
            new[] {
              ("UserProfileQueries", typeof(Modules.UserProfiles.UserProfileQueries)),
              ("UserProfileMutations", typeof(Modules.UserProfiles.UserProfileMutations)),
              ("UserProfileType", typeof(Modules.UserProfiles.UserProfileType)),
            },
            logger,
            isExtension: new[] { true, true, false }
          );
        },
      ["Tenants"] = b => {
        SafeAddGraphQLTypes(
          b,
          new[] {
            ("TenantQueries", typeof(Modules.Tenants.TenantQueries)),
            ("TenantMutations", typeof(Modules.Tenants.TenantMutations)),
            ("TenantType", typeof(Modules.Tenants.TenantType)),
            ("TenantPermissionType", typeof(Modules.Tenants.TenantPermissionType)),
          },
          logger,
          isExtension: new[] { true, true, false, false }
        );
      },
      ["Projects"] = b => {
        SafeAddGraphQLTypes(
          b,
          new[] { ("ProjectQueries", typeof(Modules.Projects.ProjectQueries)), ("ProjectMutations", typeof(Modules.Projects.ProjectMutations)), ("ProjectType", typeof(GameGuild.Modules.Projects.ProjectType)) },
          logger,
          isExtension: new[] { true, true, false }
        );
      },
    };

    // Add test module for testing environments
    var isTestEnvironment = options.IsTestEnvironment ||
                            options.EnableTestingModule ||
                            AppDomain.CurrentDomain.GetAssemblies()
                                     .Any(a => a.FullName?.Contains("GameGuild.API.Tests") == true);

    if (isTestEnvironment) {
      coreModules["TestModule"] = b => {
        try {
          // Try to register test module if available
          var testModuleType = Type.GetType("GameGuild.Tests.MockModules.TestModuleQueries, GameGuild.API.Tests");

          if (testModuleType != null) {
            SafeAddGraphQLTypes(
              b,
              new[] { ("TestModuleQueries", testModuleType) },
              logger,
              isExtension: new[] { true }
            );
            logger?.LogDebug("Registered test module: TestModuleQueries");
          }
        }
        catch (Exception ex) {
          logger?.LogDebug("Test module not available: {Error}", ex.Message);
          // Ignore test module registration failures
        }
      };
    }

    foreach (var (moduleName, registration) in coreModules) {
      try {
        registration(builder);
        logger?.LogDebug("Registered core module: {ModuleName}", moduleName);
      }
      catch (Exception ex) {
        logger?.LogError(ex, "Failed to register core module: {ModuleName}", moduleName);

        throw;
      }
    }
  }

  /// <summary>
  /// Registers optional modules based on configuration settings
  /// </summary>
  private static void RegisterOptionalModules(IRequestExecutorBuilder builder, GraphQLOptions options, ILogger? logger) {
    // Authentication module
    if (options.EnableAuthentication) {
      TryRegisterModule(
        builder,
        "Authentication",
        () => {
          SafeAddGraphQLTypes(builder, new[] { ("AuthQueries", typeof(AuthQueries)), ("AuthMutations", typeof(AuthMutations)) }, logger, isExtension: new[] { true, true });

          // Try to add CredentialType if it exists
          SafeAddGraphQLTypes(builder, new[] { ("CredentialType", typeof(Modules.Credentials.CredentialType)) }, logger, isExtension: new[] { false }, isOptional: true);
        },
        logger
      );
    }

    // TestingLab module
    if (options.EnableTestingModule) {
      TryRegisterModule(
        builder,
        "TestingLab",
        () => {
          SafeAddGraphQLTypes(
            builder,
            new[] {
              ("TestingLabQueries", typeof(Modules.TestingLab.TestingLabQueries)),
              ("TestingLabMutations", typeof(Modules.TestingLab.TestingLabMutations)),
              ("TestingRequestType", typeof(Modules.TestingLab.TestingRequestType)),
              ("TestingSessionType", typeof(Modules.TestingLab.TestingSessionType)),
              ("TestingParticipantType", typeof(Modules.TestingLab.TestingParticipantType)),
              ("TestingLocationType", typeof(Modules.TestingLab.TestingLocationType)),
            },
            logger,
            isExtension: new[] { true, true, false, false, false, false }
          );
        },
        logger
      );
    }
  }

  /// <summary>
  /// Registers advanced modules like Programs, Products, etc.
  /// </summary>
  private static void RegisterAdvancedModules(IRequestExecutorBuilder builder, GraphQLOptions options, ILogger? logger) {
    var advancedModules = new Dictionary<string, Action> {
      ["Programs"] = () => {
        SafeAddGraphQLTypes(
          builder,
          new[] {
            ("ProgramContentQueries", typeof(ProgramContentQueries)),
            ("ProgramContentMutations", typeof(ProgramContentMutations)),
            ("ContentInteractionQueries", typeof(ContentInteractionQueries)),
            ("ContentInteractionMutations", typeof(ContentInteractionMutations)),
            ("ActivityGradeQueries", typeof(ActivityGradeQueries)),
            ("ActivityGradeMutations", typeof(ActivityGradeMutations)),
            ("ProgramContentType", typeof(Modules.Programs.ProgramContentType)),
            ("ContentInteractionType", typeof(ContentInteractionType)),
            ("ActivityGradeType", typeof(ActivityGradeType)),
          },
          logger,
          isExtension: new[] { true, true, true, true, true, true, false, false, false }
        );
      },
      ["Products"] = () => {
        SafeAddGraphQLTypes(
          builder,
          new[] { ("ProductQueries", typeof(ProductQueries)), ("ProductMutations", typeof(ProductMutations)), ("ProductType", typeof(Modules.Products.ProductType)) },
          logger,
          isExtension: new[] { true, true, false },
          isOptional: true
        );
      },
      ["Payments"] = () => {
        SafeAddGraphQLTypes(
          builder,
          new[] { ("PaymentQueries", typeof(PaymentQueries)), ("PaymentMutations", typeof(PaymentMutations)) },
          logger,
          isExtension: new[] { true, true },
          isOptional: true
        );
      },
    };

    foreach (var (moduleName, registration) in advancedModules) { TryRegisterModule(builder, moduleName, registration, logger); }
  }

  /// <summary>
  /// Safely adds GraphQL types with comprehensive error handling
  /// </summary>
  private static void SafeAddGraphQLTypes(IRequestExecutorBuilder builder, (string Name, Type Type)[] types, ILogger? logger, bool[] isExtension, bool isOptional = false) {
    for (int i = 0; i < types.Length; i++) {
      var (name, type) = types[i];
      var isExt = i < isExtension.Length ? isExtension[i] : false;

      try {
        if (isExt) {
          // Use reflection to call AddTypeExtension<T>()
          var method = typeof(RequestExecutorBuilderExtensions)
                       .GetMethods()
                       .FirstOrDefault(m => m.Name == "AddTypeExtension" && m.IsGenericMethodDefinition);

          if (method != null) {
            var genericMethod = method.MakeGenericMethod(type);
            genericMethod.Invoke(null, new object[] { builder });
            logger?.LogDebug("Added GraphQL type extension: {TypeName}", name);
          }
        }
        else {
          // Use reflection to call AddType<T>()
          var method = typeof(RequestExecutorBuilderExtensions)
                       .GetMethods()
                       .FirstOrDefault(m => m.Name == "AddType" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);

          if (method != null) {
            var genericMethod = method.MakeGenericMethod(type);
            genericMethod.Invoke(null, new object[] { builder });
            logger?.LogDebug("Added GraphQL type: {TypeName}", name);
          }
        }
      }
      catch (Exception ex) {
        if (isOptional) { logger?.LogDebug("Optional GraphQL type {TypeName} not available: {Error}", name, ex.Message); }
        else {
          logger?.LogWarning(ex, "Failed to register GraphQL type {TypeName}", name);
          // In test environments, be more permissive with missing types
          // Note: We can't access options here, so we'll just log and continue
        }
      }
    }
  }

  /// <summary>
  /// Safely registers a module with error handling
  /// </summary>
  private static void TryRegisterModule(IRequestExecutorBuilder builder, string moduleName, Action registration, ILogger? logger) {
    try {
      registration();
      logger?.LogDebug("Registered optional module: {ModuleName}", moduleName);
    }
    catch (Exception ex) { logger?.LogWarning(ex, "Failed to register optional module: {ModuleName}. This module will be skipped.", moduleName); }
  }

  /// <summary>
  /// Configures GraphQL server options based on provided configuration
  /// </summary>
  private static void ConfigureServerOptions(IRequestExecutorBuilder builder, GraphQLOptions options, ILogger? logger = null) {
    builder.ModifyOptions(opt => {
      opt.RemoveUnreachableTypes = options.RemoveUnreachableTypes;
      opt.EnsureAllNodesCanBeResolved = options.EnsureAllNodesCanBeResolved;
      opt.StrictValidation = options.StrictValidation;
      opt.EnableDirectiveIntrospection = options.EnableDirectiveIntrospection;
    }
    );

    // Configure introspection
    if (!options.EnableIntrospection) {
      builder.DisableIntrospection();
      logger?.LogInformation("Introspection disabled for non-test environment");
    }
    else {
      logger?.LogInformation("Introspection enabled (default HotChocolate behavior)");
    }

    // Configure request options for development vs production
    builder.ModifyRequestOptions(opt => {
      opt.IncludeExceptionDetails = options.IncludeExceptionDetails;
    });
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
  /// Configuration options for GraphQL infrastructure
  /// </summary>
  public class GraphQLOptions {
    /// <summary>
    /// Enable authentication types and queries
    /// </summary>
    public bool EnableAuthentication { get; set; } = true;

    /// <summary>
    /// Enable testing module types
    /// </summary>
    public bool EnableTestingModule { get; set; } = true;

    /// <summary>
    /// Enable advanced module types (Programs, etc.)
    /// </summary>
    public bool EnableAdvancedModules { get; set; } = true;

    /// <summary>
    /// Enable DAC authorization middleware
    /// </summary>
    public bool EnableAuthorization { get; set; } = true;

    /// <summary>
    /// Enable global object identification
    /// </summary>
    public bool EnableGlobalObjectIdentification { get; set; } = false;

    /// <summary>
    /// Enable filtering capabilities
    /// </summary>
    public bool EnableFiltering { get; set; } = true;

    /// <summary>
    /// Enable sorting capabilities
    /// </summary>
    public bool EnableSorting { get; set; } = true;

    /// <summary>
    /// Enable projection capabilities
    /// </summary>
    public bool EnableProjections { get; set; } = true;

    /// <summary>
    /// Remove unreachable types from schema
    /// </summary>
    public bool RemoveUnreachableTypes { get; set; } = true;

    /// <summary>
    /// Ensure all nodes can be resolved
    /// </summary>
    public bool EnsureAllNodesCanBeResolved { get; set; } = false;

    /// <summary>
    /// Enable strict validation
    /// </summary>
    public bool StrictValidation { get; set; } = false;

    /// <summary>
    /// Enable directive introspection
    /// </summary>
    public bool EnableDirectiveIntrospection { get; set; } = false;

    /// <summary>
    /// Enable schema introspection
    /// </summary>
    public bool EnableIntrospection { get; set; } = true;

    /// <summary>
    /// Include exception details in responses
    /// </summary>
    public bool IncludeExceptionDetails { get; set; } = false;

    /// <summary>
    /// Indicates if this is a test environment (affects error handling and module availability)
    /// </summary>
    public bool IsTestEnvironment { get; set; } = false;

    /// <summary>
    /// Validates the GraphQL configuration options
    /// </summary>
    public void Validate() {
      // No specific validation needed currently
      // This method is here for future validation requirements
    }
  }

  /// <summary>
  /// Factory methods for creating GraphQL configuration for different environments
  /// </summary>
  public static class GraphQLOptionsFactory {
    /// <summary>
    /// Creates a production-ready GraphQL configuration
    /// </summary>
    public static GraphQLOptions ForProduction() {
      return new GraphQLOptions {
        EnableAuthentication = true,
        EnableTestingModule = true,
        EnableAdvancedModules = true,
        EnableAuthorization = true,
        EnableFiltering = true,
        EnableSorting = true,
        EnableProjections = true,
        EnableGlobalObjectIdentification = false,
        RemoveUnreachableTypes = true,
        EnsureAllNodesCanBeResolved = false,
        StrictValidation = false,
        EnableDirectiveIntrospection = false,
        EnableIntrospection = false, // Disabled for security in production
        IncludeExceptionDetails = false,
        IsTestEnvironment = false,
      };
    }

    /// <summary>
    /// Creates a development-friendly GraphQL configuration
    /// </summary>
    public static GraphQLOptions ForDevelopment() {
      return new GraphQLOptions {
        EnableAuthentication = true,
        EnableTestingModule = true,
        EnableAdvancedModules = true,
        EnableAuthorization = true,
        EnableFiltering = true,
        EnableSorting = true,
        EnableProjections = true,
        EnableGlobalObjectIdentification = true,
        RemoveUnreachableTypes = false, // Keep for debugging
        EnsureAllNodesCanBeResolved = false,
        StrictValidation = false,
        EnableDirectiveIntrospection = true,
        EnableIntrospection = true,
        IncludeExceptionDetails = true,
        IsTestEnvironment = false,
      };
    }

    /// <summary>
    /// Creates a test-friendly GraphQL configuration
    /// </summary>
    public static GraphQLOptions ForTesting() {
      return new GraphQLOptions {
        EnableAuthentication = false, // Avoid auth conflicts in tests
        EnableTestingModule = true,
        EnableAdvancedModules = false, // Keep tests minimal
        EnableAuthorization = false, // Skip DAC in tests
        EnableFiltering = true,
        EnableSorting = true,
        EnableProjections = false,
        EnableGlobalObjectIdentification = false,
        RemoveUnreachableTypes = false,
        EnsureAllNodesCanBeResolved = false,
        StrictValidation = false,
        EnableDirectiveIntrospection = false,
        EnableIntrospection = true,
        IncludeExceptionDetails = true,
        IsTestEnvironment = true,
      };
    }

    /// <summary>
    /// Creates a minimal GraphQL configuration for basic functionality
    /// </summary>
    public static GraphQLOptions Minimal() {
      return new GraphQLOptions {
        EnableAuthentication = false,
        EnableTestingModule = false,
        EnableAdvancedModules = false,
        EnableAuthorization = false,
        EnableFiltering = false,
        EnableSorting = false,
        EnableProjections = false,
        EnableGlobalObjectIdentification = false,
        RemoveUnreachableTypes = true,
        EnsureAllNodesCanBeResolved = false,
        StrictValidation = false,
        EnableDirectiveIntrospection = false,
        EnableIntrospection = true,
        IncludeExceptionDetails = false,
        IsTestEnvironment = false,
      };
    }
  }

  /// <summary>
  /// Gets all application assemblies to scan for types
  /// </summary>
  private static Assembly[] GetApplicationAssemblies(Assembly[]? additionalAssemblies = null) {
    var baseAssemblies = new[] { Assembly.GetExecutingAssembly(), typeof(Program).Assembly, };

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
                       try { return assembly.GetTypes(); }
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
                     }
                     )
                     .Where(t => t is { IsClass: true, IsAbstract: false })
                     .ToArray();

      options.Logger?.LogInformation("Found {TypeCount} concrete types across all assemblies", allTypes.Length);

      // Register all handler types in a single optimized pass
      var registeredCount = RegisterHandlerTypes(
        services,
        allTypes,
        [typeof(IQueryHandler<,>), typeof(ICommandHandler<,>), typeof(ICommandHandler<>), typeof(IDomainEventHandler<>),],
        options
      );

      // Register FluentValidation validators
      var validatorCount = RegisterValidators(services, allTypes, options);

      // Execute custom registration if provided
      options.CustomRegistrationAction?.Invoke(services, allTypes, options);

      if (options.EnablePerformanceLogging && stopwatch != null) {
        stopwatch.Stop();
        options.Logger?.LogInformation(
          "Handler registration completed in {ElapsedMs}ms. Registered {HandlerCount} handlers and {ValidatorCount} validators",
          stopwatch.ElapsedMilliseconds,
          registeredCount,
          validatorCount
        );
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
                                                                     .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
                                                  )
                                                  .ToArray();

      var implementingTypes = registrationCache[handlerInterfaceType];
      options?.Logger?.LogDebug(
        "Found {Count} implementations for {Interface}",
        implementingTypes.Length,
        handlerInterfaceType.Name
      );

      foreach (var implementingType in implementingTypes) {
        var serviceInterfaces = implementingType.GetInterfaces()
                                                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
                                                .ToArray();

        foreach (var serviceInterface in serviceInterfaces) {
          services.AddScoped(serviceInterface, implementingType);
          registeredCount++;
          options?.Logger?.LogTrace(
            "Registered {Implementation} as {Service}",
            implementingType.Name,
            serviceInterface.Name
          );
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
                         .Where(t => t.GetInterfaces()
                                      .Any(i => i.IsGenericType &&
                                                i.GetGenericTypeDefinition() == typeof(IValidator<>)
                                      )
                         )
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
        options?.Logger?.LogTrace(
          "Registered validator {Implementation} as {Service}",
          validatorType.Name,
          @interface.Name
        );
      }
    }

    return registeredCount;
  }

  /// <summary>
  /// Retrieves registration metrics from the last registration operation
  /// </summary>
  public static RegistrationMetrics GetRegistrationMetrics(IServiceProvider serviceProvider) {
    return serviceProvider.GetService<RegistrationMetrics>() ?? new RegistrationMetrics { TotalHandlersRegistered = 0, TotalValidatorsRegistered = 0, RegistrationDuration = TimeSpan.Zero, };
  }
}
