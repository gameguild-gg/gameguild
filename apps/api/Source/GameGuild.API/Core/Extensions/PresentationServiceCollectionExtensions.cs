using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using GameGuild.AI;
using GameGuild.Commerce.Billing;
using GameGuild.Commerce.Orders;
using GameGuild.Commerce.Payments;
using GameGuild.Commerce.Products;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Compliance.FERPA;
using GameGuild.Configuration;
using GameGuild.Configuration.PresentationLayer.Controllers;
using GameGuild.Configuration.PresentationLayer.Endpoints;
using GameGuild.Content.Pages;
using GameGuild.GameJams;
using GameGuild.LaunchPad;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using GameGuild.Learning.Enrollments;
using GameGuild.Social.Blog;
using GameGuild.Social.Feed;
using GameGuild.Social.Groups;
using GameGuild.Social.Profiles;
using GameGuild.Social.Reactions;
using GameGuild.Tags;
using GameGuild.Projects;
using GameGuild.TestingLab;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API;

/// <summary>
///     Extension methods for configuring presentation layer services
///     (Controllers, Endpoints, Middlewares).
/// </summary>
public static class PresentationServiceCollectionExtensions
{
    public static IServiceCollection SetupControllers(this IServiceCollection services, IConfiguration configuration,
        ControllersOptions? options)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("GameGuild.API");
        
        var totalStopwatch = Stopwatch.StartNew();
        logger.LogInformation("Setting up controllers...");
        
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Controllers",
            ControllersOptions.CreateDefault);
        options.Validate();

        var controllerStopwatch = Stopwatch.StartNew();
        services.AddControllers(mvcOptions =>
            {
                mvcOptions.Conventions.Add(new MinimumOrderRouteApplicationModelConvention());

                if (options.UseKebabCaseRoutes)
                {
                    mvcOptions.Conventions.Add(
                        new Microsoft.AspNetCore.Mvc.ApplicationModels.RouteTokenTransformerConvention(
                            new KebabCaseParameterTransformer()));
                }

                // Add permission authorization filter globally to all controllers
                // This provides defense-in-depth by requiring explicit [AllowAnonymous] to opt-out
                // Re-enabled 2026-01-15 per ASSETS_RESOURCES_DEEP_REVIEW.md security audit
                if (options.EnablePermissionAuthorizationFilter)
                    mvcOptions.Filters.Add<ResourcePermissionAuthorizationFilter>();
            }
        )
        .ConfigureApplicationPartManager(manager =>
        {
            // Remove all GameGuild module assemblies from auto-discovery
            var partsToRemove = manager.ApplicationParts
                .Where(part => part.Name.StartsWith("GameGuild.", StringComparison.OrdinalIgnoreCase)
                               && part.Name != "GameGuild.API")
                .ToList();

            foreach (var part in partsToRemove)
            {
                manager.ApplicationParts.Remove(part);
            }
            logger.LogInformation("Removed {Count} auto-discovered module assemblies", partsToRemove.Count);
        })
        .AddApplicationPart(typeof(DependencyInjection).Assembly); // Main API assembly only
        
        // Log individual controllers from GameGuild.API
        LogControllersFromAssembly(typeof(DependencyInjection).Assembly, logger, controllerStopwatch);

        // ===== MODULE CONTROLLERS =====
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(Identity.Users.UsersController).Assembly); // Users module
        LogControllersFromAssembly(typeof(Identity.Users.UsersController).Assembly, logger, controllerStopwatch);
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(Identity.Tenants.TenantsController).Assembly); // Tenants module
        LogControllersFromAssembly(typeof(Identity.Tenants.TenantsController).Assembly, logger, controllerStopwatch);
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(Resources.ResourcesController).Assembly); // Resources module
        LogControllersFromAssembly(typeof(Resources.ResourcesController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(OrdersController).Assembly); // Orders module
        LogControllersFromAssembly(typeof(OrdersController).Assembly, logger, controllerStopwatch);
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(PaymentsController).Assembly); // Payments module
        LogControllersFromAssembly(typeof(PaymentsController).Assembly, logger, controllerStopwatch);
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(SubscriptionsController).Assembly); // Subscriptions module
        LogControllersFromAssembly(typeof(SubscriptionsController).Assembly, logger, controllerStopwatch);
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(BillingWebhooksController).Assembly); // Billing module
        LogControllersFromAssembly(typeof(BillingWebhooksController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(ProductsController).Assembly); // Products module
        LogControllersFromAssembly(typeof(ProductsController).Assembly, logger, controllerStopwatch);
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(AuthController).Assembly); // Authentication module
        LogControllersFromAssembly(typeof(AuthController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(AiController).Assembly); // AI module
        LogControllersFromAssembly(typeof(AiController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(FerpaController).Assembly); // FERPA Compliance module
        LogControllersFromAssembly(typeof(FerpaController).Assembly, logger, controllerStopwatch);
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(GameGuild.Features.FeatureFlagsController).Assembly); // Features module
        LogControllersFromAssembly(typeof(GameGuild.Features.FeatureFlagsController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(GameGuild.Learning.Courses.ProgramCrudController).Assembly); // Courses module
        LogControllersFromAssembly(typeof(GameGuild.Learning.Courses.ProgramCrudController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(GameGuild.Learning.Assessments.AssessmentsController).Assembly); // Assessments module
        LogControllersFromAssembly(typeof(GameGuild.Learning.Assessments.AssessmentsController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(EnrollmentsController).Assembly); // Learning Enrollments module
        LogControllersFromAssembly(typeof(EnrollmentsController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(GameGuild.Learning.Cohorts.CohortsController).Assembly); // Learning Cohorts module
        LogControllersFromAssembly(typeof(GameGuild.Learning.Cohorts.CohortsController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(GameGuild.Learning.Certificates.CertificatesController).Assembly); // Learning Certificates module
        LogControllersFromAssembly(typeof(GameGuild.Learning.Certificates.CertificatesController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(GameGuild.Learning.Experience.Discovery.DiscoveryController).Assembly); // Learning Discovery module
        LogControllersFromAssembly(typeof(GameGuild.Learning.Experience.Discovery.DiscoveryController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(GameGuild.Learning.Experience.LearningPaths.LearningPathController).Assembly); // Learning Paths module
        LogControllersFromAssembly(typeof(GameGuild.Learning.Experience.LearningPaths.LearningPathController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(GameGuild.Learning.Experience.Recommendations.RecommendationsController).Assembly); // Learning Recommendations module
        LogControllersFromAssembly(typeof(GameGuild.Learning.Experience.Recommendations.RecommendationsController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(GameGuild.Learning.Experience.Social.Controllers.ReviewsController).Assembly); // Learning Social module
        LogControllersFromAssembly(typeof(GameGuild.Learning.Experience.Social.Controllers.ReviewsController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(BlogPostsController).Assembly); // Social Blog module
        LogControllersFromAssembly(typeof(BlogPostsController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(SocialProfilesController).Assembly); // Social Profiles module
        LogControllersFromAssembly(typeof(SocialProfilesController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(FeedController).Assembly); // Social Feed module
        LogControllersFromAssembly(typeof(FeedController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(SocialGroupsController).Assembly); // Social Groups module
        LogControllersFromAssembly(typeof(SocialGroupsController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(ReactionsController).Assembly); // Social Reactions module
        LogControllersFromAssembly(typeof(ReactionsController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(GameJamsController).Assembly); // Game Jams module
        LogControllersFromAssembly(typeof(GameJamsController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(ProjectsController).Assembly); // Projects module
        LogControllersFromAssembly(typeof(ProjectsController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(TestingRequestsController).Assembly); // Testing Lab module
        LogControllersFromAssembly(typeof(TestingRequestsController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(LaunchPadController).Assembly); // Launch Pad module
        LogControllersFromAssembly(typeof(LaunchPadController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(GameGuild.Content.Pages.PageController).Assembly); // Content Pages module
        LogControllersFromAssembly(typeof(GameGuild.Content.Pages.PageController).Assembly, logger, controllerStopwatch);
        
        services.AddControllers()
            .AddJsonOptions(jsonOptions =>
            {
                jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = options.JsonPropertyNamingPolicy switch
                {
                    "CamelCase" => System.Text.Json.JsonNamingPolicy.CamelCase,
                    "SnakeCaseLower" => System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
                    "SnakeCaseUpper" => System.Text.Json.JsonNamingPolicy.SnakeCaseUpper,
                    "KebabCaseLower" => System.Text.Json.JsonNamingPolicy.KebabCaseLower,
                    "KebabCaseUpper" => System.Text.Json.JsonNamingPolicy.KebabCaseUpper,
                    _ => System.Text.Json.JsonNamingPolicy.CamelCase
                };
                jsonOptions.JsonSerializerOptions.WriteIndented = options.WriteIndentedJson;
            });

        totalStopwatch.Stop();
        logger.LogInformation("Completed controller setup in {ElapsedMs}ms", totalStopwatch.ElapsedMilliseconds);

        return services;
    }
    
    /// <summary>
    ///     Logs individual controller names from an assembly with human-readable formatting.
    /// </summary>
    private static void LogControllersFromAssembly(Assembly assembly, ILogger logger, Stopwatch stopwatch)
    {
        var controllerBaseType = typeof(ControllerBase);
        var controllers = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && controllerBaseType.IsAssignableFrom(t))
            .ToList();

        foreach (var controller in controllers)
        {
            var formattedName = FormatControllerName(controller.Name);
            logger.LogInformation("Registered {ControllerName} in {ElapsedMs}ms", formattedName, stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
        }
    }
    
    /// <summary>
    ///     Formats a controller name from PascalCase to human-readable format.
    ///     e.g., "UserProfileController" -> "User Profile Controller"
    /// </summary>
    private static string FormatControllerName(string controllerName)
    {
        // Insert space before each uppercase letter (except the first)
        var formatted = Regex.Replace(controllerName, "([a-z])([A-Z])", "$1 $2");
        // Also handle consecutive uppercase letters like "APIController" -> "API Controller"
        formatted = Regex.Replace(formatted, "([A-Z]+)([A-Z][a-z])", "$1 $2");
        return formatted;
    }

    public static IServiceCollection SetupEndpoints(this IServiceCollection services, IConfiguration configuration,
        EndpointsOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Endpoints",
            EndpointsOptions.CreateDefault);
        options.Validate();

        if (options.RegisterFromMainAssembly)
        {
            // Register minimal API endpoints (IEndpoint implementations)
            services.AddEndpoints(typeof(DependencyInjection).Assembly);
        }

        return services;
    }

    /// <summary>
    ///     Registers custom middleware services.
    ///     Note: Most middlewares are registered implicitly when UseMiddleware&lt;T&gt; is called.
    ///     This method registers any additional services that middlewares depend on.
    /// </summary>
    public static IServiceCollection SetupMiddlewares(this IServiceCollection services, IConfiguration configuration)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("GameGuild.API");

        var totalStopwatch = Stopwatch.StartNew();
        logger.LogInformation("Starting middleware setup...");

        var stepStopwatch = Stopwatch.StartNew();

        // CorrelationIdMiddleware - No additional services needed (uses ILogger via DI)
        stepStopwatch.Restart();
        logger.LogInformation("Registered Correlation Id Middleware in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

        // SecurityHeadersMiddleware - No additional services needed (uses ILogger via DI)
        stepStopwatch.Restart();
        logger.LogInformation("Registered Security Headers Middleware in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

        // TenantMiddleware - Depends on IMediator (CQRS) and ITenantDomainsRepository
        stepStopwatch.Restart();
        logger.LogInformation("Registered Tenant Resolution Middleware in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

        totalStopwatch.Stop();
        logger.LogInformation("Completed middleware setup in {ElapsedMs}ms", totalStopwatch.ElapsedMilliseconds);

        return services;
    }
}
