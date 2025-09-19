using System.Reflection;
using GameGuild.Authorization.Identity;
using GameGuild.Core.Configuration;
using GameGuild.Core.Domain.Identity;
using GameGuild.CQRS;
using GameGuild.Database;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Billing;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Posts;
using GameGuild.Modules.Products;
using GameGuild.Modules.Programs;
using GameGuild.Modules.Projects;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Subscriptions;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.TestingLab;
using GameGuild.Modules.UserAchievements;
using GameGuild.Modules.Users;
using GameGuild.Source.Core.Services;
using GameGuild.Source.Modules.Authorization;
using GameGuild.Source.Modules.Billing;
using GameGuild.Source.Modules.Payments;
using GameGuild.Source.Modules.Posts;
using GameGuild.Source.Modules.TestingLab;
using static GameGuild.Source.Core.Services.RoleTemplateService;
using static GameGuild.Source.Core.Services.TenantIsolationService;
using static GameGuild.Source.Core.Services.UsernameNormalizationService;
using static GameGuild.Source.Core.Services.UserPrivacyService;


namespace GameGuild;

public static class DependencyInjection {
  /// <summary> Adds the presentation layer services with custom options. </summary>
  /// <param name="services"> The service collection </param>
  /// <param name="configuration"> The application configuration </param>
  /// <param name="options"> Custom presentation layer options </param>
  /// <returns> The service collection for chaining </returns>
  public static IServiceCollection AddPresentationLayer(this IServiceCollection services, IConfiguration configuration, PresentationLayerOptions? options = null) {
    options ??= PresentationLayerOptionsBuilder.Create(configuration);

    // Validate the options to ensure they are correctly configured
    options.Validate();

    // Register the presentation layer services

    // 0. Structured Logging (must be first for proper logging throughout)
    services.AddStructuredLogging(configuration);

    // 1. HTTP Logging (capture everything)
    if (options.EnableHttpLogging) {
      services.SetupHttpLogging(configuration, options.HttpLogging);
    }

    // 2. Exception Handling/Problem Details (early error handling)
    if (options.EnableProblemDetails) {
      services.SetupProblemDetails(configuration, options.ProblemDetails);
    }

    // 3. Localization (early for proper culture context)
    if (options.EnableLocalization) {
      services.SetupLocalization(configuration, options.Localization);
    }

    // 4. Memory Caching (foundation for other services)
    if (options.EnableMemoryCaching) {
      services.SetupMemoryCaching(configuration, options.MemoryCaching);
    }

    // 5. Response Caching (early performance optimization)
    if (options.EnableResponseCaching) {
      services.SetupResponseCaching(configuration, options.ResponseCaching);
    }

    // 6. Response Compression (early performance optimization)
    if (options.EnableResponseCompression) {
      services.SetupResponseCompression(configuration, options.ResponseCompression);
    }    // 7. CORS (Cross-Origin Resource Sharing)
    // TODO: CF-Connecting-IP Cloudflare header support.
    if (options.EnableCors) {
      services.SetupCors(configuration, options.Cors);
    }

    // 8. Authentication (identify user, and tenant)
    if (options.EnableAuthentication) {
      services.SetupAuthentication(configuration, options.Authentication);
    }

    // 8.5. Cookie Security
    if (options.EnableCookieSecurity) {
      services.AddCookieSecurity(configuration, options.CookieSecurity);
    }

    // 9. Request Context (user/tenant context after authentication)
    if (options.EnableRequestContext) {
      services.SetupRequestContext(configuration, options.RequestContext);
    }

    // 10. Authorization (depends on authentication)
    if (options.EnableAuthorization) {
      services.SetupAuthorization(configuration, options.Authorization);
    }

    // 11. Rate Limiting (after authentication for user-based limits)
    if (options.EnableRateLimiting) {
      services.SetupRateLimiting(configuration, options.RateLimiting);
    }

    // 12. Model Validation
    if (options.EnableModelValidation) {
      services.SetupModelValidation(configuration, options.ModelValidation);
    }

    // 13. API Robustness (FluentValidation pipeline behaviors and enhanced error handling)
    if (options.EnableFluentValidation) {
      services.SetupFluentValidation(configuration, options.FluentValidation);
    }

    if (options.EnableErrorHandling) {
      services.SetupErrorHandling(configuration, options.ErrorHandling);
    }

    // 14. Feature Flags (OpenFeature)
    if (options.EnableFeatureFlags) {
      services.SetupFeatureFlags(configuration, options.FeatureFlags);
    }

    // 14. API Versioning
    if (options.EnableApiVersioning) {
      services.SetupApiVersioning(configuration, options.ApiVersioning);
    }

    // 14.5. REST Conventions (Enhanced versioning, status codes, ETags)
    services.SetupRestConventions(configuration);

    // 15. API Explorer
    if (options.EnableApiExplorer) {
      services.SetupApiExplorer(configuration, options.ApiVersioning);
    }

    // 16. Health Checks
    if (options.EnableHealthChecks) {
      services.SetupHealthChecks(configuration, options.HealthChecks);
    }

    // 17. SignalR (real-time communication)
    if (options.EnableSignalR) {
      services.SetupSignalR(configuration, options.SignalR);
    }

    // 18. Controllers/Endpoints
    // services.AddControllers(options => { options.Conventions.Add(new RouteTokenTransformerConvention(new GameGuild.ToKebabParameterTransformer())); })
    //         .AddApplicationPart(typeof(PresentationLayerOptions).Assembly)
    //         .AddApplicationPart(typeof(GameGuild.Users.UsersController).Assembly) // Users module
    //         .AddApplicationPart(typeof(GameGuild.Tenants.Presentation.Controllers.TenantsController).Assembly) // Tenants module
    //         .AddApplicationPart(typeof(GameGuild.Billing.Presentation.Controllers.BillingWebhooksController).Assembly) // Billing module
    //         .AddApplicationPart(typeof(GameGuild.Payments.Presentation.Controllers.PaymentsController).Assembly) // Payments module
    //         .AddApplicationPart(typeof(GameGuild.Subscriptions.Presentation.Controllers.SubscriptionsController).Assembly) // Subscriptions module
    //         .AddJsonOptions(jsonOptions => {
    //             jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    //             jsonOptions.JsonSerializerOptions.WriteIndented = true;
    //           }
    //         );

    // 19. GraphQL server configuration
    if (options.EnableGraphQl) {
      services.SetupGraphQl(configuration, options.GraphQl);
    }

    // 20. gRPC (handled by the application layer)

    // 21. OpenAPI/Swagger
    if (options.EnableOpenApi) {
      services.SetupOpenApi(configuration, options.OpenApi);
    }

    return services;
  }

  /// <summary> Adds the application layer services to the service collection. Backward compatibility method for tests that still use AddApplicationLayer() </summary>
  /// <param name="services"> The service collection </param>
  /// <returns> The service collection for chaining </returns>
  public static IServiceCollection AddApplicationLayer(this IServiceCollection services, IConfiguration configuration, ApplicationLayerOptions? options = null) {
    // Get all GameGuild assemblies automatically to scan for CQRS handlers
    var assemblies = GetAssembliesByPattern();

    // Add CQRS services (handlers, behaviors, etc.)
    services.AddCQRS(assemblies);

    // Add telemetry pipeline behavior for CQRS operations
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Core.Behaviors.TelemetryBehavior<,>));

    // Core infrastructure services are now registered in the Infrastructure Layer
    // This ensures they're available when modules are registered

    return services;
  }

  /// <summary> Adds the infrastructure layer services to the service collection. Includes repositories, external service integrations, and data access components. </summary>
  /// <param name="services"> The service collection </param>
  /// <param name="configuration"> The application configuration </param>
  /// <returns> The service collection for chaining </returns>
  public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration, InfrastructureLayerOptions? options = null) {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);

    // Register core infrastructure services FIRST (required by modules)
    services.AddCoreInfrastructure();

    // Register core business services
    services.AddCoreServices();

    // Register database context
    ServiceCollectionExtensions.AddDatabaseContext(services, configuration);

    // Add OpenTelemetry observability
    services.AddOpenTelemetryObservability(configuration);

    // Add health checks
    services.AddApplicationHealthChecks(configuration);

    // Add telemetry services
    services.AddScoped<Core.Telemetry.IPermissionTelemetryService, Core.Telemetry.PermissionTelemetryService>();

    // Register repositories
    // TODO: Enable when repository implementations are ready
    // services.AddRepositories();

    // Register external services (email, payment, etc.)
    services.AddExternalServices(configuration);

    return services;
  }

  /// <summary> Gets all application assemblies to scan for types with explicit entry assembly </summary>
  public static Assembly[] GetApplicationAssemblies(Assembly entryAssembly, params Assembly[] additionalAssemblies) {
    ArgumentNullException.ThrowIfNull(entryAssembly);
    ArgumentNullException.ThrowIfNull(additionalAssemblies);

    var baseAssemblies = new[] {
      Assembly.GetExecutingAssembly(), // Core assembly
      entryAssembly, // Explicitly provided entry assembly (e.g., API assembly)
    };

    return additionalAssemblies.Length > 0 ? baseAssemblies.Concat(additionalAssemblies).Distinct().ToArray() : baseAssemblies.Distinct().ToArray();
  }

  /// <summary> Gets assemblies from the current application domain that match the specified pattern </summary>
  public static Assembly[] GetAssembliesByPattern(string pattern = "GameGuild.*") {
    return AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.FullName?.StartsWith(pattern, StringComparison.OrdinalIgnoreCase) == true).ToArray();
  }

  /// <summary> Retrieves registration metrics from the last registration operation </summary>
  public static RegistrationMetrics GetRegistrationMetrics(IServiceProvider serviceProvider) {
    return serviceProvider.GetService<RegistrationMetrics>() ?? new RegistrationMetrics { TotalHandlersRegistered = 0, TotalValidatorsRegistered = 0, RegistrationDuration = TimeSpan.Zero };
  }

  /// <summary>
  ///    Registers repository implementations
  /// </summary>
  // private static IServiceCollection AddRepositories(this IServiceCollection services) {
  //   // User repositories - using shared ApplicationDbContext
  //   services.AddScoped<GameGuild.Users.Domain.Abstractions.IUserRepository>(provider => new GameGuild.Users.Infrastructure.Repositories.UserRepository(provider.GetRequiredService<ApplicationDbContext>()));
  // 
  //   // Tenants module - temporarily use in-memory database
  //   // TODO: Configure proper TenantsDbContext when database setup is complete
  //   services.AddScoped<GameGuild.Tenants.Domain.Abstractions.ITenantRepository>(provider => {
  //       // Create a TenantsDbContext-compatible wrapper using in-memory database
  //       // This is a temporary solution until proper TenantsDbContext is configured
  //       var optionsBuilder = new DbContextOptionsBuilder<GameGuild.Tenants.Infrastructure.Data.TenantsDbContext>();
  //       optionsBuilder.UseInMemoryDatabase("TenantsTemp"); // Temporary in-memory solution
  //       var tenantsContext = new GameGuild.Tenants.Infrastructure.Data.TenantsDbContext(optionsBuilder.Options);
  // 
  //       return new GameGuild.Tenants.Infrastructure.Repositories.TenantRepository(tenantsContext);
  //     }
  //   );
  // 
  //   // Subscriptions repositories - using shared ApplicationDbContext
  //   services.AddSubscriptionsInfrastructure<ApplicationDbContext>();
  // 
  //   // Note: Some modules like Tenants may use their own DbContext
  //   // Those will need to be configured separately when their contexts are set up
  // 
  //   return services;
  // }

  /// <summary> Registers external service implementations </summary>
  private static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration) {
    // Add modules using the new standardized IModule pattern where available

    // Core business modules with IModule implementations
    services.AddAuthenticationModule(configuration);
    services.AddProgramsModuleV2(configuration); // Keep V2 temporarily until updated
    services.AddBillingModule(configuration);
    services.AddPaymentsModule(configuration);
    services.AddTestingLabModule(configuration);
    services.AddPostsModule(configuration);
    services.AddAuthorizationModule(configuration);

    // Legacy modules still using old pattern (to be migrated)
    services.AddResourcesModule();
    services.AddTenantsModule();
    services.AddProjectsModule();
    services.AddSubscriptionsModule();
    services.AddCredentialsModule();
    services.AddUsersModule();
    services.AddUserAchievementsModule();
    services.AddProductsModule();

    // External services will be added here as modules are implemented
    // Email service, payment providers, notification services, etc.

    return services;
  }

  /// <summary> Registers core infrastructure services required by CQRS handlers </summary>
  private static IServiceCollection AddCoreInfrastructure(this IServiceCollection services) {
    // Add debug logging
    Console.WriteLine("🔧 AddCoreInfrastructure called");

    // Register domain event publisher
    services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
    Console.WriteLine("✅ IDomainEventPublisher registered");

    // Register context services
    services.AddScoped<IUserContext, UserContext>();
    Console.WriteLine("✅ IUserContext registered");

    services.AddScoped<ITenantContext, TenantContext>();
    Console.WriteLine("✅ ITenantContext registered");

    services.AddHttpContextAccessor(); // Required by UserContext and TenantContext
    Console.WriteLine("✅ HttpContextAccessor registered");

    Console.WriteLine("🔧 AddCoreInfrastructure completed successfully");
    return services;
  }

  /// <summary> Registers core business services for tenant isolation, privacy, and role management </summary>
  private static IServiceCollection AddCoreServices(this IServiceCollection services) {
    // Add debug logging
    Console.WriteLine("🔧 AddCoreServices called");

    // Tenant isolation and management services
    services.AddScoped<ITenantIsolationService, TenantIsolationService>();
    Console.WriteLine("✅ ITenantIsolationService registered");

    // Role template management services
    services.AddScoped<IRoleTemplateService, RoleTemplateService>();
    Console.WriteLine("✅ IRoleTemplateService registered");

    // Username normalization services
    services.AddScoped<IUsernameNormalizationService, UsernameNormalizationService>();
    Console.WriteLine("✅ IUsernameNormalizationService registered");

    // Privacy management services
    services.AddScoped<IUserPrivacyService, UserPrivacyService>();
    Console.WriteLine("✅ IUserPrivacyService registered");

    Console.WriteLine("🔧 AddCoreServices completed successfully");
    return services;
  }
}
