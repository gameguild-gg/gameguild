using GameGuild.Common;


namespace GameGuild.Examples;

/// <summary>
/// Examples demonstrating different ways to configure and run the GameGuild API
/// using the modern builder pattern with clean method chaining.
/// </summary>
public static class HostingExamples
{
    /// <summary>
    /// Example 1: Standard production hosting with clean fluent syntax
    /// </summary>
    public static async Task RunStandardAsync(string[] args)
    {
        await WebApplication
            .CreateBuilder(args)
            .ConfigureGameGuildApplication()
            .BuildWithPipelineAsync()
            .ContinueWith(app => app.Result.RunAsync())
            .Unwrap();
    }

    /// <summary>
    /// Example 2: Development hosting with enhanced debugging
    /// </summary>
    public static async Task RunDevelopmentAsync(string[] args)
    {
        var app = await GameGuildApiBuilderFactory
            .CreateForDevelopment(args)
            .BuildWithPipelineAsync();

        // Add development-specific middleware
        app.UseCustomMiddleware(devApp =>
        {
            devApp.Use(async (context, next) =>
            {
                context.Response.Headers["X-Development-Mode"] = "true";
                context.Response.Headers["X-Environment"] = "Development";
                await next();
            });
        });

        await app.RunAsync();
    }

    /// <summary>
    /// Example 3: Testing scenario with in-memory database
    /// </summary>
    public static async Task<WebApplication> CreateTestAppAsync(string[] args)
    {
        return await GameGuildApiBuilderFactory
            .CreateForTesting(args)
            .BuildWithPipelineAsync();
    }

    /// <summary>
    /// Example 4: Production hosting with monitoring and health checks
    /// </summary>
    public static async Task RunProductionAsync(string[] args)
    {
        var app = await GameGuildApiBuilderFactory
            .CreateForProduction(args)
            .BuildWithPipelineAsync();

        // Configure production-specific endpoints
        app.MapHealthChecks("/health")
           .MapHealthChecks("/ready")
           .UseCustomMiddleware(prodApp =>
           {
               prodApp.Use(async (context, next) =>
               {
                   using var activity = new System.Diagnostics.Activity("GameGuild.Request");
                   activity.Start();
                   activity.SetTag("request.path", context.Request.Path.ToString());
                   activity.SetTag("environment", "Production");
                   
                   try
                   {
                       await next();
                   }
                   finally
                   {
                       activity.Stop();
                   }
               });
           });

        await app.RunAsync();
    }

    /// <summary>
    /// Example 5: Custom configuration with specific options
    /// </summary>
    public static async Task RunWithCustomConfigAsync(string[] args)
    {
        await WebApplication
            .CreateBuilder(args)
            .ConfigureGameGuildApplication()
            .BuildWithPipelineAsync()
            .ContinueWith(async appTask =>
            {
                var app = appTask.Result;
                
                // Custom endpoint
                app.MapGet("/api/info", () => new
                {
                    Application = "GameGuild API",
                    Version = "1.0.0",
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    Timestamp = DateTime.UtcNow
                });

                await app.RunAsync();
            })
            .Unwrap();
    }

    /// <summary>
    /// Example 6: Most concise production-ready setup - single expression
    /// </summary>
    public static async Task RunConciseAsync(string[] args) =>
        await WebApplication
            .CreateBuilder(args)
            .ConfigureGameGuildApplication()
            .BuildWithPipelineAsync()
            .ContinueWith(app => app.Result.RunAsync())
            .Unwrap();
}
