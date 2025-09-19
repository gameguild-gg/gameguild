using GameGuild.Core.Configuration;
using GameGuild.Core.GraphQL;
using GameGuild.Core.Middleware;
using GameGuild.Core.REST;

namespace GameGuild;

/// <summary> Extension methods for WebApplication to configure the request pipeline and application concerns. </summary>
internal static class WebApplicationExtensions {
  /// <summary> Configures the development pipeline with debugging tools and middleware. </summary>
  /// <param name="app"> The web application </param>
  /// <returns> The web application for chaining </returns>
  public static WebApplication ConfigureDevelopmentPipeline(this WebApplication app) {
    ArgumentNullException.ThrowIfNull(app);

    if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

    return app;
  }

  /// <summary> Configures the production pipeline with security and performance optimizations. </summary>
  /// <param name="app"> The web application </param>
  /// <returns> The web application for chaining </returns>
  public static WebApplication ConfigureProductionPipeline(this WebApplication app) {
    ArgumentNullException.ThrowIfNull(app);

    if (app.Environment.IsDevelopment()) return app;

    app.UseExceptionHandler("/Error");
    app.UseHsts();

    return app;
  }

  /// <summary> Configures the common middleware pipeline for all environments. </summary>
  /// <param name="app"> The web application </param>
  /// <returns> The web application for chaining </returns>
  public static WebApplication ConfigureCommonPipeline(this WebApplication app) {
    ArgumentNullException.ThrowIfNull(app);

    // Add correlation ID middleware very early in pipeline
    app.UseCorrelationId();

    // Add comprehensive request logging after correlation ID
    app.UseRequestLogging(options => {
      options.LogRequestHeaders = app.Environment.IsDevelopment();
      options.LogResponseHeaders = app.Environment.IsDevelopment();
      options.LogRequestBody = app.Environment.IsDevelopment();
      options.SlowRequestThresholdMs = app.Environment.IsDevelopment() ? 1000 : 2000;
    });

    // Add global exception handling after logging but before other middleware
    app.UseGlobalExceptionHandling();

    // TODO: Restore REST conventions after fixing corrupted files
    // app.UseRestConventions();
    // app.UseRestApiVersioning();

    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors();
    app.UseAuthentication();

    // Use cookie policy for secure cookie configuration
    app.UseCookiePolicy();

    // Add custom rate limiting middleware after authentication but before authorization
    app.UseRateLimiting();

    // Add GraphQL security middleware for complexity/depth analysis
    app.UseGraphQLSecurity();

    app.UseAuthorization();
    app.UseRateLimiter();

    return app;
  }  /// <summary> Configures the complete GameGuild application pipeline. </summary>
     /// <param name="app"> The web application </param>
     /// <returns> The web application for chaining </returns>
  public static WebApplication ConfigurePipeline(this WebApplication app) {
    ArgumentNullException.ThrowIfNull(app);

    app = app.ConfigureDevelopmentPipeline().ConfigureProductionPipeline().ConfigureCommonPipeline().UseOpenApi();

    // Map health check endpoints
    app.UseApplicationHealthChecks();

    // Map controller endpoints
    app.MapControllers();

    // Map GraphQL endpoint
    app.MapGraphQL("/graphql");

    // Additional minimal endpoints would be mapped here when implemented via IEndpoint.

    return app;
  }

  /// <summary> Configures OpenAPI UI in the request pipeline with both native support and Swagger UI. </summary>
  /// <param name="app"> The web application </param>
  /// <param name="documentName"> The document name (default: "v1") </param>
  /// <returns> The web application for chaining </returns>
  public static WebApplication UseOpenApi(this WebApplication app, string documentName = "v1") {
    ArgumentNullException.ThrowIfNull(app);

    if (!app.Environment.IsDevelopment() && !app.Environment.IsStaging()) return app;

    // Native .NET 9 OpenAPI endpoint
    app.MapOpenApi();

    // Swashbuckle Swagger UI
    app.UseSwagger();

    app.UseSwaggerUI(options => {
      options.SwaggerEndpoint($"/swagger/{documentName}/swagger.json", "GameGuild API v1");
      options.RoutePrefix = "swagger"; // Swagger UI will be available at /swagger
    }
    );

    return app;
  }
}
