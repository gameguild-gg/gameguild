// using GameGuild.Common;
// using GameGuild.Common.Extensions;
//
// namespace GameGuild.Examples;
//
// /// <summary>
// /// Examples demonstrating different ways to configure and run the GameGuild API
// /// using the modern builder pattern with extension methods.
// /// </summary>
// public static class HostingExamples
// {
//     /// <summary>
//     /// Example 1: Standard production hosting
//     /// </summary>
//     public static async Task RunStandardAsync(string[] args)
//     {
//         await WebApplication
//             .CreateBuilder(args)
//             .ConfigureApplication()
//             .BuildAndConfigureAsync()
//             .ContinueWith(task => task.Result.RunGameGuildApiAsync())
//             .Unwrap();
//     }
//
//     /// <summary>
//     /// Example 2: Development hosting with custom configuration
//     /// </summary>
//     public static async Task RunDevelopmentAsync(string[] args)
//     {
//         await GameGuildApiBuilderFactory
//             .CreateForDevelopment(args)
//             .BuildAndConfigureAsync()
//             .ContinueWith(task => task.Result
//                 .MapHealthChecks("/health")
//                 .UseCustomMiddleware(app =>
//                 {
//                     // Custom development middleware
//                     app.Use(async (context, next) =>
//                     {
//                         context.Response.Headers.Add("X-Development-Mode", "true");
//                         await next();
//                     });
//                 })
//                 .RunGameGuildApiAsync())
//             .Unwrap();
//     }
//
//     /// <summary>
//     /// Example 3: Testing scenario with in-memory database
//     /// </summary>
//     public static async Task<WebApplication> CreateTestAppAsync(string[] args)
//     {
//         return await GameGuildApiBuilderFactory
//             .CreateForTesting(args)
//             .BuildAndConfigureAsync();
//     }
//
//     /// <summary>
//     /// Example 4: Custom hosting with specific configuration
//     /// </summary>
//     public static async Task RunCustomAsync(string[] args)
//     {
//         await GameGuildApiBuilderFactory
//             .CreateCustom(args, builder =>
//             {
//                 // Custom builder configuration
//                 builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001");
//             })
//             .ConfigureGameGuildApi(options =>
//             {
//                 // Custom presentation options
//                 options.EnableSwagger = true;
//                 options.EnableRateLimiting = false;
//                 options.AllowedOrigins = new[] { "http://localhost:3000", "https://app.gameguild.com" };
//             })
//             .BuildAndConfigureAsync()
//             .ContinueWith(task => task.Result.RunGameGuildApiAsync())
//             .Unwrap();
//     }
//
//     /// <summary>
//     /// Example 5: Production hosting with enhanced monitoring
//     /// </summary>
//     public static async Task RunProductionAsync(string[] args)
//     {
//         var app = await GameGuildApiBuilderFactory
//             .CreateForProduction(args)
//             .BuildAndConfigureAsync();
//
//         // Add production-specific endpoints
//         app.MapHealthChecks("/health")
//            .MapHealthChecks("/ready")
//            .UseCustomMiddleware(app =>
//            {
//                // Production middleware for monitoring
//                app.Use(async (context, next) =>
//                {
//                    using var activity = System.Diagnostics.Activity.StartActivity("GameGuild.Request");
//                    activity?.SetTag("request.path", context.Request.Path);
//                    await next();
//                });
//            });
//
//         await app.RunGameGuildApiAsync();
//     }
// }
