using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using GameGuild.Abstractions;
using GameGuild.API.Authorization;
using GameGuild.API.Data;
using GameGuild.Audit;
using GameGuild.Authentication.Controllers;
using GameGuild.Authentication.Entities;
using GameGuild.Authentication.Extensions;
using GameGuild.Billing.Controllers;
using GameGuild.Payments;
using GameGuild.Payments.Controllers;
using GameGuild.Permissions.Infrastructure.Extensions;
using GameGuild.Resources.Controllers;
using GameGuild.Resources.Extensions;
using GameGuild.SharedKernel.Configuration;
using GameGuild.Subscriptions.Controllers;
using GameGuild.Subscriptions.Extensions;
using GameGuild.Tenants;
using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Extensions;
using GameGuild.Tenants.Repositories;
using GameGuild.Users;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Core;

public static class DependencyInjection
{
    /// <summary>
    ///     Adds the presentation layer services with custom options.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <param name="options">Custom presentation layer options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPresentationLayer(this IServiceCollection services, IConfiguration configuration, PresentationLayerOptions? options = null)
    {
        options ??= PresentationLayerOptionsBuilder.CreateWithValidation(configuration);

        // Validate the options to ensure they are correctly configured
        options.Validate();

        // Register the presentation layer services

        // 1. HTTP Logging (capture everything)
        if (options.EnableHttpLogging) services.SetupHttpLogging(configuration, options.HttpLogging);

        // 2. Exception Handling/Problem Details (early error handling)
        if (options.EnableProblemDetails) services.SetupProblemDetails(configuration, options.ProblemDetails);

        // 3. Localization (early for error messages)
        if (options.EnableLocalization) services.SetupLocalization(configuration, options.Localization);

        // 4. Memory Caching (foundation for other services)
        if (options.EnableMemoryCaching) services.SetupMemoryCaching(configuration, options.MemoryCaching);

        // 5. Response Caching (after memory caching)
        if (options.EnableResponseCaching) services.SetupResponseCaching(configuration, options.ResponseCaching);

        // 6. Response Compression
        if (options.EnableResponseCompression) services.SetupResponseCompression(configuration, options.ResponseCompression);

        // 7. CORS (Cross-Origin Resource Sharing)
        if (options.EnableCors) services.SetupCors(configuration, options.Cors);

        // 8. Authentication (identify user, and tenant)
        if (options.EnableAuthentication) services.SetupAuthentication(configuration, options.Authentication);

        // 9. Request Context (after authentication, unified request context handling)
        // Handles user, tenant, location, and feature information in one cohesive service
        if (options.EnableRequestContext) services.SetupRequestContext(configuration, options.RequestContext);

        // 10. Authorization (after context is established and feature flags are checked)
        if (options.EnableAuthorization) services.SetupAuthorization(configuration, options.Authorization);

        // 11. Rate Limiting
        if (options.EnableRateLimiting) services.SetupRateLimiting(configuration, options.RateLimiting);

        // 12. Model Validation
        if (options.EnableModelValidation) services.SetupModelValidation(configuration, options.ModelValidation);

        // 13. Feature Flags (OpenFeature)
        if (options.EnableFeatureFlags) services.SetupFeatureFlags(configuration, options.FeatureFlags);

        // 14. API Versioning
        if (options.EnableApiVersioning) services.SetupApiVersioning(configuration, options.ApiVersioning);

        // 15. Health Checks
        if (options.EnableHealthChecks) services.SetupHealthChecks(configuration, options.HealthChecks);

        // 16. SignalR (real-time communication)
        if (options.EnableSignalR) services.SetupSignalR(configuration, options.SignalR);

        // 17. Controllers/Endpoints
        services.AddControllers(options =>
                {
                    options.Conventions.Add(new RouteTokenTransformerConvention(new KebabCaseParameterTransformer()));
                    // Add permission authorization filter globally to all controllers
                    options.Filters.Add<PermissionAuthorizationFilter>();
                }
            )
            .AddApplicationPart(typeof(DependencyInjection).Assembly) // Main API assembly
            .AddApplicationPart(typeof(MfaController).Assembly) // Authentication module
            .AddApplicationPart(typeof(UsersController).Assembly) // Users module
            .AddApplicationPart(typeof(TenantsController).Assembly) // Tenants module
            .AddApplicationPart(typeof(BillingWebhooksController).Assembly) // Billing module
            .AddApplicationPart(typeof(PaymentsController).Assembly) // Payments module
            .AddApplicationPart(typeof(SubscriptionsController).Assembly) // Subscriptions module
            .AddApplicationPart(typeof(ResourcesController).Assembly) // Resources module
            .AddJsonOptions(jsonOptions =>
                {
                    jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    jsonOptions.JsonSerializerOptions.WriteIndented = true;
                }
            );

        // Register minimal API endpoints (IEndpoint implementations)
        services.AddEndpoints(typeof(DependencyInjection).Assembly);

        // 18. API Explorer - MUST be called AFTER controllers and application parts are registered
        if (options.EnableApiExplorer) services.SetupApiExplorer(configuration, options.ApiVersioning);

        // 19. GraphQL (handled by the application layer)

        // 20. gRPC (handled by the application layer)

        // 21. OpenAPI/Swagger
        if (options.EnableOpenApi) services.SetupOpenApi(configuration, options.OpenApi);

        return services;
    }

    /// <summary>
    ///     Gets all application assemblies to scan for types with explicit entry assembly
    /// </summary>
    public static Assembly[ ] GetApplicationAssemblies(Assembly entryAssembly, params Assembly[ ] additionalAssemblies)
    {
        ArgumentNullException.ThrowIfNull(entryAssembly);
        ArgumentNullException.ThrowIfNull(additionalAssemblies);

        var baseAssemblies = new[ ]
        {
            Assembly.GetExecutingAssembly(), // Core assembly
            entryAssembly // Explicitly provided entry assembly (e.g., API assembly)
        };

        return additionalAssemblies.Length > 0 ? baseAssemblies.Concat(additionalAssemblies).Distinct().ToArray() : baseAssemblies.Distinct().ToArray();
    }

    /// <summary>
    ///     Gets assemblies from the current application domain that match the specified pattern
    /// </summary>
    public static Assembly[ ] GetAssembliesByPattern(string pattern = "GameGuild.*")
    {
        return AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.FullName?.StartsWith(pattern, StringComparison.OrdinalIgnoreCase) == true).ToArray();
    }

    /// <summary>
    ///     Retrieves registration metrics from the last registration operation
    /// </summary>
    public static RegistrationMetrics GetRegistrationMetrics(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<RegistrationMetrics>() ?? new RegistrationMetrics { TotalHandlersRegistered = 0, TotalValidatorsRegistered = 0, RegistrationDuration = TimeSpan.Zero };
    }

    /// <summary>
    ///     Adds the infrastructure layer services to the service collection.
    ///     Includes repositories, external service integrations, and data access components.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register unified ApplicationDbContext with PostgreSQL/InMemory
        services.AddInfrastructureData(configuration);

        // Register Authentication module (Infrastructure + Application layers)
        services.AddAuthenticationModule(configuration);

        // Register Permissions module (Infrastructure layer)
        services.AddPermissionsInfrastructure();

        // Register Resources module (Application layer)
        services.AddResourcesModule(configuration);

        // Register Audit module
        services.AddAuditServices();

        // Register repositories
        services.AddRepositories();

        // Register external services (email, payment, etc.)
        services.AddExternalServices(configuration);

        return services;
    }

    /// <summary>
    ///     Registers authentication services including Infrastructure and Application layers
    /// </summary>
    private static IServiceCollection AddAuthenticationModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Authentication Infrastructure (DbContext, repositories, etc.)
        services.AddAuthenticationData(configuration);

        // Register Authentication Application (handlers, validators, etc.)
        services.AddAuthenticationApplication();

        // Register password hasher for Authentication module
        services.AddScoped<IPasswordHasher<AuthUser>, PasswordHasher<AuthUser>>();

        return services;
    }

    /// <summary>
    ///     Registers Resources module application services
    /// </summary>
    private static IServiceCollection AddResourcesModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Resources Infrastructure (handlers, repositories, DbContext, etc.)
        // Note: DbContext is registered by AddInfrastructureData
        services.AddResourcesInfrastructure(configuration);

        return services;
    }

    /// <summary>
    ///     Registers repository implementations
    /// </summary>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // User repositories - using shared ApplicationDbContext
        services.AddScoped<IUserRepository>(provider => new UserRepository(provider.GetRequiredService<ApplicationDbContext>()));

        // Tenants module - temporarily use in-memory database
        // TODO: Configure proper TenantsDbContext when database setup is complete
        services.AddScoped<ITenantRepository>(provider =>
            {
                // Use the shared ApplicationDbContext registered in DI
                var dbContext = provider.GetRequiredService<IApplicationDbContext>();

                // TenantRepository expects an IApplicationDbContext
                return new TenantRepository(dbContext);
            }
        );

        // Subscriptions repositories - using shared ApplicationDbContext
        services.AddSubscriptionsModule();

        // Note: Some modules like Tenants may use their own DbContext
        // Those will need to be configured separately when their contexts are set up

        return services;
    }

    /// <summary>
    ///     Registers external service implementations
    /// </summary>
    private static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add modules using the new standardized IModule pattern where available

        // ===== CORE ESSENTIAL MODULES (Always enabled) =====
        services.AddAuthenticationModule(configuration); // Required for user authentication
        services.AddPermissionsInfrastructure(); // Required for permission system

        // ===== CORE INFRASTRUCTURE MODULES (Always enabled) =====
        services.AddResourcesModule(configuration); // Core resource management
        services.AddTenantsModule(); // Required for multi-tenancy

        // ===== FEATURE MODULES (Add as needed) =====
        // services.AddBillingModule(configuration); // Billing module - temporarily disabled due to DI issue
        services.AddPaymentsModule(configuration); // Payments module
        services.AddSubscriptionsModule(); // Subscriptions module
        // services.AddFeaturesModule(); // Feature flags module - temporarily disabled due to DI issue

        // External services will be added here as modules are implemented
        // Email service, payment providers, notification services, etc.

        return services;
    }

    /// <summary>
    ///     Registers shared infrastructure data services including ApplicationDbContext
    /// </summary>
    private static IServiceCollection AddInfrastructureData(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = configuration.GetValue<string>("Database:Provider") ?? "Postgres";

        if (databaseProvider == "InMemory")
        {
            var databaseName = configuration.GetValue<string>("Database:ConnectionString") ?? $"GameGuildInMemory_{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        }
        else
        {
            // Try multiple connection string sources for flexibility
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? configuration.GetValue<string>("DB_CONNECTION_STRING") ?? configuration.GetValue<string>("Database:ConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string not found in configuration. " + "Please add either 'ConnectionStrings:DefaultConnection' to appsettings.json, " + "or set the 'DB_CONNECTION_STRING' environment variable."
                );
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseNpgsql(
                        connectionString,
                        npgsqlOptions =>
                        {
                            npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                            npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                            npgsqlOptions.CommandTimeout(30);
                        }
                    );

                    // Enable sensitive data logging in development
                    if (configuration.GetValue<bool>("DetailedErrors"))
                    {
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                    }
                }
            );
        }

        // Register the interface for dependency injection
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        return services;
    }
}

/// <summary>
///     Transforms route parameters to kebab-case
/// </summary>
public class KebabCaseParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value == null) return null;

        var stringValue = value.ToString();

        return string.IsNullOrEmpty(stringValue) ? stringValue : Regex.Replace(stringValue, "([a-z0-9])([A-Z])", "$1-$2").ToLower();
    }
}
