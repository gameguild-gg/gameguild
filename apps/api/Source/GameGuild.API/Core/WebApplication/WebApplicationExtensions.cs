using Asp.Versioning.ApiExplorer;

namespace GameGuild.Core;

/// <summary>
///     Extension methods for WebApplication to configure the request pipeline and application concerns.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    ///     Configures the development pipeline with debugging tools and middleware.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication ConfigureDevelopmentPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Environment.IsDevelopment()) { app.UseDeveloperExceptionPage(); }

        return app;
    }

    /// <summary>
    ///     Configures the production pipeline with security and performance optimizations.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication ConfigureProductionPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Environment.IsDevelopment()) return app;

        app.UseExceptionHandler("/Error");
        app.UseHsts();

        return app;
    }

    /// <summary>
    ///     Configures the common middleware pipeline for all environments.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication ConfigureCommonPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        return app;
    }

    /// <summary>
    ///     Configures the complete GameGuild application pipeline.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Get the configured OpenAPI version to ensure UI matches the generated document
        string openApiVersion = app.Configuration.GetValue<string>("OpenApi:Version") ?? "v1";

        app = app.ConfigureDevelopmentPipeline().ConfigureProductionPipeline().ConfigureCommonPipeline().UseOpenApi(openApiVersion);

        // Map controller endpoints
        app.MapControllers();

        // Redirect root route to Swagger documentation
        app.MapGet("/", () => Results.Redirect("/docs")).ExcludeFromDescription();

        // Map all registered IEndpoint implementations
        app.MapEndpoints(null);

        // Map authentication endpoints
        // Disabled - using Authentication module controllers instead
        // app.MapAuthEndpoints();

        // Map user "me" endpoint
        // Disabled - using Users module controllers instead
        // app.MapUserMeEndpoint();

        // Map tenants endpoint
        // Disabled - using Tenants module controllers instead
        // app.MapTenantsEndpoints();

        // NOTE: Roles and Permissions are handled by controllers in Authentication.Presentation
        // Commenting out duplicate minimal API endpoints to avoid Swagger conflicts

        // Map role management endpoints
        // app.MapRolesEndpoints(); // Duplicate of RolesController at /api/roles

        // Map permission management endpoints
        // app.MapPermissionsEndpoints(); // Duplicate of PermissionsController at /api/permissions

        // Additional minimal endpoints would be mapped here when implemented via IEndpoint.

        return app;
    }

    /// <summary>
    ///     Configures OpenAPI UI in the request pipeline with both native support and Swagger UI.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <param name="documentName">The document name (default: "v1")</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseOpenApi(this WebApplication app, string documentName = "v1")
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            // Native .NET 9 OpenAPI endpoint
            app.MapOpenApi();

            // Swashbuckle Swagger UI
            app.UseSwagger();

            app.UseSwaggerUI(c =>
                {
                    c.RoutePrefix = "docs"; // Swagger UI will be available at /docs

                    // Try to load API version descriptions and generate an endpoint per version
                    var provider = app.Services.GetService<IApiVersionDescriptionProvider>();

                    if (provider is not null)
                    {
                        foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
                        {
                            c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"GameGuild API {description.GroupName.ToUpperInvariant()}");
                        }
                    }
                    else
                    {
                        // Fallback to a single document when versioning is not configured
                        c.SwaggerEndpoint($"/swagger/{documentName}/swagger.json", $"GameGuild API {documentName.ToUpperInvariant()}");
                    }
                }
            );
        }

        return app;
    }
}
