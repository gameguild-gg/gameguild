using GameGuild.Database;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Tenants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


// Add this for TestJwtAuthenticationFilter and TestAuthHandler

namespace GameGuild.Tests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration and E2E tests that properly configures content root and services
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program> {
  protected override void ConfigureWebHost(IWebHostBuilder builder) {
    // Set the content root to the project directory
    var projectDir = Directory.GetCurrentDirectory();
    builder.UseContentRoot(projectDir);

    // Set required environment variables for tests
    Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", "InMemory");
    Environment.SetEnvironmentVariable("USE_IN_MEMORY_DB", "true");
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
    Environment.SetEnvironmentVariable("ASPNETCORE_DETAILEDERRORS", "true");

    // Add JWT environment variables - must match development API configuration
    Environment.SetEnvironmentVariable(
      "JWT_SECRET",
      "game-guild-super-secret-key-for-development-only-minimum-32-characters"
    );
    Environment.SetEnvironmentVariable(
      "JWT_REFRESH_SECRET",
      "test-jwt-refresh-secret-key-for-integration-testing-minimum-32-characters"
    );

    // GitHub OAuth settings (mock values for tests)
    Environment.SetEnvironmentVariable("GITHUB_CLIENT_ID", "test-github-client-id");
    Environment.SetEnvironmentVariable("GITHUB_CLIENT_SECRET", "test-github-client-secret");

    // Google OAuth settings (mock values for tests)
    Environment.SetEnvironmentVariable("GOOGLE_CLIENT_ID", "test-google-client-id");
    Environment.SetEnvironmentVariable("GOOGLE_CLIENT_SECRET", "test-google-client-secret");

    // Override configuration settings for testing
    builder.ConfigureAppConfiguration((context, config) => {
        // Add test-specific configuration settings - must match development API configuration
        config.AddInMemoryCollection(
          new Dictionary<string, string?> {
            { "Jwt:SecretKey", "game-guild-super-secret-key-for-development-only-minimum-32-characters" },
            { "Jwt:Issuer", "GameGuild.CMS" },
            { "Jwt:Audience", "GameGuild.Users" },
            { "Jwt:ExpiryInMinutes", "60" },
            { "Jwt:RefreshTokenExpiryInDays", "7" },
            { "OAuth:GitHub:ClientId", "test-github-client-id" },
            { "OAuth:GitHub:ClientSecret", "test-github-client-secret" }
          }
        );
      }
    );

    builder.ConfigureServices(services => {
        // Configure logging for better debugging
        services.AddLogging(builder => {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
          }
        );

        // Configure API behavior for detailed error responses in tests
        services.Configure<ApiBehaviorOptions>(options => {
            options.InvalidModelStateResponseFactory = context => {
              var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TestWebApplicationFactory>>();

              // Log detailed validation errors
              var errors = context.ModelState.Where(x => x.Value?.Errors.Count > 0)
                                  .ToDictionary(
                                    kvp => kvp.Key,
                                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                                  );

              logger.LogWarning(
                "Model validation failed for {Path}. Errors: {@Errors}",
                context.HttpContext.Request.Path,
                errors
              );

              return new BadRequestObjectResult(new { Title = "One or more validation errors occurred.", Status = 400, Errors = errors, TraceId = context.HttpContext.TraceIdentifier });
            };
          }
        );

        // Remove the existing DbContext configurations to prevent multiple database provider registration
        var descriptor =
          services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

        if (descriptor != null) services.Remove(descriptor);

        // Remove all EF Core related services to prevent conflicts
        var efCoreServices = services
                             .Where(s => s.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true)
                             .ToList();

        foreach (var service in efCoreServices) services.Remove(service);

        // Replace TenantContextService with mock for testing
        var tenantContextServiceDescriptor = services.SingleOrDefault(d =>
                                                                        d.ServiceType == typeof(ITenantContextService)
        );

        if (tenantContextServiceDescriptor != null) services.Remove(tenantContextServiceDescriptor);

        services
          .AddSingleton<ITenantContextService,
            Helpers.MockTenantContextService>();

        // Add in-memory database for testing with unique database name
        var databaseName = $"TestDatabase_{Guid.NewGuid()}";
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));

        // Don't override GraphQL configuration - let the main application handle it
        // The main application already configures GraphQL through AddGraphQLInfrastructure
        // Trying to reconfigure it here causes duplicate registration issues

        // Configure JWT for testing - must match the development API configuration
        // This should match the configuration expected by AuthModuleDependencyInjection
        var testJwtConfig = new ConfigurationBuilder().AddInMemoryCollection(
                                                        new Dictionary<string, string?> {
                                                          { "Jwt:SecretKey", "game-guild-super-secret-key-for-development-only-minimum-32-characters" },
                                                          { "Jwt:Issuer", "GameGuild.CMS" },
                                                          { "Jwt:Audience", "GameGuild.Users" },
                                                          { "Jwt:ExpiryInMinutes", "60" }
                                                        }
                                                      )
                                                      .Build();

        // Remove existing JWT configuration service and replace with test-compatible version
        var jwtServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IJwtTokenService));
        if (jwtServiceDescriptor != null) 
        {
            services.Remove(jwtServiceDescriptor);
            // Add test-compatible JWT service that uses the same configuration as the main app
            services.AddSingleton<IJwtTokenService>(provider => new JwtTokenService(testJwtConfig));
        }

        // Don't remove or re-configure authentication services - let the main application handle them
        // The application already configures JWT authentication through AuthModuleDependencyInjection
        // We just need to ensure our test JWT configuration is compatible

        // Configure minimal controller setup for tests - let the main app handle authentication filters
        // This avoids conflicts with the authentication setup done by the main application
      }
    );
  }
}
