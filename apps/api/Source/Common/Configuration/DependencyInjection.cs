using System.Reflection;
using GameGuild.Modules.Authentication.Configuration;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;


namespace GameGuild.Common;

public static class DependencyInjection {
  public static IServiceCollection AddPresentation(this IServiceCollection services) {
    // API Documentation
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c => {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "GameGuild API", Version = "v1", Description = "A comprehensive API for GameGuild platform with CQRS architecture" });

        // Include XML comments for better documentation
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (System.IO.File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
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
    services.AddCors(options => {
        options.AddDefaultPolicy(builder => {
            builder
              .WithOrigins("http://localhost:3000", "https://localhost:3001") // Adjust for your frontend URLs
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
          }
        );
      }
    );

    // Health Checks
    services.AddHealthChecks()
            .AddCheck("database", () => HealthCheckResult.Healthy(), new[] { "database" });

    // Response Compression
    services.AddResponseCompression(options => {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
      }
    );

    // Rate Limiting
    services.AddRateLimiter(options => {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter(
          "DefaultPolicy",
          limiterOptions => {
            limiterOptions.PermitLimit = 100;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 50;
          }
        );
      }
    );

    return services;
  }

  public static IServiceCollection AddApplication(this IServiceCollection services) {
    // // MediatR for CQRS
    // services.AddMediatR(cfg => {
    //     cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    //     cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    //   }
    // );
    services.AddMediatR(Assembly.GetExecutingAssembly(), typeof(Program).Assembly);

    // Unified MediatR Pipeline Behaviors (order matters!)
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnifiedValidationBehavior<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

    // FluentValidation support 
    // Note: Validators will be found and registered automatically by MediatR behaviors

    // Domain Event Handlers
    services.AddDomainEventHandlers();

    // Background Services for domain events
    services.AddHostedService<DomainEventProcessorService>();

    return services;
  }

  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration = null) {
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

    // Background Services
    services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

    return services;
  }

  private static IServiceCollection AddDomainEventHandlers(this IServiceCollection services) {
    var assembly = Assembly.GetExecutingAssembly();

    var handlerTypes = assembly.GetTypes()
                               .Where(t => t is { IsClass: true, IsAbstract: false } &&
                                           t.GetInterfaces()
                                            .Any(i => i.IsGenericType &&
                                                      i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)
                                            )
                               )
                               .ToArray();

    foreach (var handlerType in handlerTypes) {
      var interfaceType = handlerType.GetInterfaces()
                                     .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));

      services.AddScoped(interfaceType, handlerType);
    }

    return services;
  }
}
