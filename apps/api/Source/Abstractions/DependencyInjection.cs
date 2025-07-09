using GameGuild.Common.Extensions;
using MediatR;
using Web.Api.Endpoints;
using Web.Api.Infrastructure;
using System.Reflection;
using FluentValidation;

namespace Web.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        // API Documentation
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { 
                Title = "GameGuild API", 
                Version = "v1",
                Description = "A comprehensive API for GameGuild platform with CQRS architecture" 
            });
            
            // Include XML comments for better documentation
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        // Controllers (for backward compatibility with existing REST endpoints)
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressModelStateInvalidFilter = true; // We handle validation through MediatR
            });

        // Exception Handling
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
            };
        });

        // Minimal API Endpoints (Clean Architecture approach)
        services.AddEndpoints();

        // CORS for frontend integration
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins("http://localhost:3000", "https://localhost:3001") // Adjust for your frontend URLs
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        // Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<GameGuild.Data.ApplicationDbContext>("database");

        // Response Compression
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        });

        // Rate Limiting
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("DefaultPolicy", limiterOptions =>
            {
                limiterOptions.PermitLimit = 100;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 50;
            });
        });

        return services;
    }

    private static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        // Register all endpoint implementations
        var assembly = Assembly.GetExecutingAssembly();
        
        var endpointTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(IEndpoint)))
            .ToArray();

        foreach (var endpointType in endpointTypes)
        {
            services.AddScoped(typeof(IEndpoint), endpointType);
        }

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR for CQRS
        services.AddMediatR(Assembly.GetExecutingAssembly(), typeof(GameGuild.Program).Assembly);

        // Unified MediatR Pipeline Behaviors (order matters!)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(GameGuild.Common.Behaviors.UnifiedLoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(GameGuild.Common.Behaviors.UnifiedValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(GameGuild.Common.Behaviors.AuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(GameGuild.Common.Behaviors.CachingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(GameGuild.Common.Behaviors.PerformanceBehavior<,>));

        // FluentValidation support
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(typeof(GameGuild.Program).Assembly);

        // Domain Event Handlers
        services.AddDomainEventHandlers();

        // Background Services for domain events
        services.AddHostedService<DomainEventProcessorService>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
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

        // Time Provider
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Background Services
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

        return services;
    }

    private static IServiceCollection AddDomainEventHandlers(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && 
                       t.GetInterfaces().Any(i => i.IsGenericType && 
                                                 i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
            .ToArray();

        foreach (var handlerType in handlerTypes)
        {
            var interfaceType = handlerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));
            
            services.AddScoped(interfaceType, handlerType);
        }

        return services;
    }

    public static void MapEndpoints(this WebApplication app)
    {
        // Map all registered endpoints
        var endpoints = app.Services.GetServices<IEndpoint>();
        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(app);
        }

        // Health checks endpoint
        app.MapHealthChecks("/health");
    }
}
