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

    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    return app;
  }

  /// <summary> Configures the complete GameGuild application pipeline. </summary>
  /// <param name="app"> The web application </param>
  /// <returns> The web application for chaining </returns>
  public static WebApplication ConfigurePipeline(this WebApplication app) {
    ArgumentNullException.ThrowIfNull(app);

    app = app.ConfigureDevelopmentPipeline().ConfigureProductionPipeline().ConfigureCommonPipeline().UseOpenApi();

    // Map controller endpoints
    app.MapControllers();

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
