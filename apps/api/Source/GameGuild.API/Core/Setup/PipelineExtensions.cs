using System.Net;
using Asp.Versioning.ApiExplorer;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Tenants;
using Serilog;

namespace GameGuild.API.Setup;

/// <summary>
///     Extension methods for configuring the HTTP request pipeline.
/// </summary>
public static class PipelineExtensions
{
    /// <summary>
    ///     Configures the complete application pipeline.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Request pipeline middleware order matters for correct behavior.

        // 01. Developer Exception Page (detailed errors in development only)
        if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

        // 02. Forwarded Headers (proxy/load balancer support, must be early)
        app.UseForwardedHeaders();

        // 03. HSTS (HTTP Strict Transport Security, production only)
        if (app.Environment.IsProduction()) app.UseHsts();

        // 04. HTTPS Redirection (force secure connections for external traffic only)
        if (!app.Environment.IsDevelopment())
        {
            app.UseWhen(
                context => !IsLoopbackRequest(context),
                branch => branch.UseHttpsRedirection());
        }

        // 05. Exception Handler (global error handling for non-development)
        if (!app.Environment.IsDevelopment()) app.UseExceptionHandler();

        // 06. Correlation ID (distributed tracing, reads/generates X-Correlation-Id header)
        app.UseCorrelationId();

        // 07. Serilog Request Logging (structured request/response logs with enrichment)
        app.UseSerilogRequestLogging(SerilogExtensions.ConfigureRequestLogging);

        // 08. HTTP Logging (ASP.NET Core built-in request/response diagnostics)
        if (app.Configuration.GetValue<bool>("PresentationLayer:EnableHttpLogging"))
        {
            app.UseHttpLogging();
        }

        // 09. Request Localization (culture/language resolution)
        app.UseRequestLocalization();

        // 10. Security Headers (X-Content-Type-Options, X-Frame-Options, CSP, Referrer-Policy, etc.)
        app.UseSecurityHeaders();

        // 11. Routing (endpoint matching, required before auth)
        app.UseRouting();

        // 12. CORS (Cross-Origin Resource Sharing, after routing)
        app.UseCors();

        // 13. Response Caching (HTTP cache headers)
        // WARNING: With JWT + multi-tenant, ensure proper Vary headers or restrict to public endpoints
        app.UseResponseCaching();

        // 14. Response Compression (gzip/brotli for smaller payloads)
        app.UseResponseCompression();

        // 15. Tenant Resolution (multi-tenant context, after routing, before auth)
        // Resolves tenant from: X-Tenant-Id header > Host domain > Query string > Default tenant
        app.UseTenantResolution();

        // 16. Authentication (identify user from JWT/cookies)
        app.UseAuthentication();

        // 17. Actor Context (build immutable ActorContext from claims + tenant)
        // SECURITY: Must be after Authentication (needs ClaimsPrincipal) and Tenant Resolution
        // SECURITY: Must be before Authorization (authorization handlers use ActorContext)
        app.UseActorContext();

        // 18. Authorization (enforce permissions after user is identified)
        app.UseAuthorization();

        // 19. Rate Limiting (throttle requests per client/endpoint)
        if (app.Configuration.GetValue<bool>("PresentationLayer:EnableRateLimiting"))
        {
            app.UseRateLimiter();
        }

        // 20. Controller Endpoints (REST API routes via [ApiController])
        app.MapControllers();

        // 21. Health Check Endpoints (disabled - HealthController provides /health, /ready, /live instead)
        // We use a controller instead of MapHealthChecks() for more control over response format,
        // HTTP status codes, and Kubernetes probe semantics.
        // app.MapHealthChecks("/health");

        // 22. Minimal API Endpoints (IEndpoint implementations, including RootRedirectEndpoint)
        app.MapEndpoints(null);

        // 23. Swagger JSON (Swashbuckle middleware generates /swagger/{version}/swagger.json)
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            app.UseSwagger();
        }

        // 24. Compatibility OpenAPI URL.
        // The native .NET OpenAPI document generator currently over-recurses on this API surface.
        // Keep /openapi/{document}.json stable by pointing callers to the Swashbuckle document.
        app.MapGet("/openapi/{documentName}.json",
            (string documentName) => Results.Redirect($"/swagger/{documentName}/swagger.json"));

        // 25. Swagger UI (interactive API documentation at /documentation)
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "documentation";

                var provider = app.Services.GetService<IApiVersionDescriptionProvider>();
                if (provider is not null)
                {
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                            $"GameGuild API {description.GroupName.ToUpperInvariant()}");
                    }
                }
                else
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "GameGuild API V1");
                }
            });
        }

        return app;
    }

    private static bool IsLoopbackRequest(HttpContext context)
    {
        var remoteIpAddress = context.Connection.RemoteIpAddress;
        return remoteIpAddress is not null && IPAddress.IsLoopback(remoteIpAddress);
    }
}
