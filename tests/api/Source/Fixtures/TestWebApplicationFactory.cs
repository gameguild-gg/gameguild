using System.Security.Claims;
using System.Text;
using GameGuild.Database;
using GameGuild.Modules.Authentication.Services;
using GameGuild.Modules.Tenants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace GameGuild.API.Tests.Fixtures;

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

    // Add JWT environment variables
    Environment.SetEnvironmentVariable(
      "JWT_SECRET",
      "test-jwt-secret-key-for-integration-testing-purposes-only-minimum-32-characters"
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
        // Add test-specific configuration settings
        config.AddInMemoryCollection(
          new Dictionary<string, string?> {
            { "Jwt:SecretKey", "test-jwt-secret-key-for-integration-testing-purposes-only-minimum-32-characters" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
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

        // Configure JWT for testing - ensure consistent configuration
        // This should match the configuration expected by AuthConfiguration
        var testJwtConfig = new ConfigurationBuilder().AddInMemoryCollection(
                                                        new Dictionary<string, string?> {
                                                          { "Jwt:SecretKey", "test-jwt-secret-key-for-integration-testing-purposes-only-minimum-32-characters" },
                                                          { "Jwt:Issuer", "TestIssuer" },
                                                          { "Jwt:Audience", "TestAudience" },
                                                          { "Jwt:ExpiryInMinutes", "60" }
                                                        }
                                                      )
                                                      .Build();

        // Remove existing JWT configuration and re-add with test settings
        var jwtServiceDescriptor =
          services.SingleOrDefault(d => d.ServiceType == typeof(IJwtTokenService));

        if (jwtServiceDescriptor != null) services.Remove(jwtServiceDescriptor);

        services.AddSingleton<IJwtTokenService>(provider =>
                                                                                  new JwtTokenService(testJwtConfig)
        );

        // Configure JWT Bearer options for tests by overriding the existing options
        services.PostConfigure<JwtBearerOptions>(
          JwtBearerDefaults.AuthenticationScheme,
          options => {
            options.TokenValidationParameters = new TokenValidationParameters {
              ValidateIssuer = true,
              ValidateAudience = true,
              ValidateLifetime = true,
              ValidateIssuerSigningKey = true,
              ValidIssuer = "TestIssuer",
              ValidAudience = "TestAudience",
              IssuerSigningKey =
                new SymmetricSecurityKey(
                  Encoding.UTF8.GetBytes(
                    "test-jwt-secret-key-for-integration-testing-purposes-only-minimum-32-characters"
                  )
                ),
              ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew tolerance
              RequireSignedTokens = true,
              TryAllIssuerSigningKeys = true
            };

            // Add event handlers for debugging
            options.Events = new JwtBearerEvents {
              OnAuthenticationFailed = context => {
                var logger =
                  context.HttpContext.RequestServices.GetRequiredService<ILogger<TestWebApplicationFactory>>();
                logger.LogError(
                  "JWT authentication failed: {Exception} for token: {Token}",
                  context.Exception.Message,
                  context.Request.Headers.Authorization.FirstOrDefault()
                );

                return Task.CompletedTask;
              },
              OnTokenValidated = context => {
                var logger =
                  context.HttpContext.RequestServices.GetRequiredService<ILogger<TestWebApplicationFactory>>();
                logger.LogInformation(
                  "JWT token validated successfully for user: {UserId}",
                  context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                );

                return Task.CompletedTask;
              },
              OnMessageReceived = context => {
                var logger =
                  context.HttpContext.RequestServices.GetRequiredService<ILogger<TestWebApplicationFactory>>();
                logger.LogDebug("JWT message received: {HasToken}", !string.IsNullOrEmpty(context.Token));

                return Task.CompletedTask;
              }
            };
          }
        );
      }
    );
  }
}

/// <summary>
/// Middleware to capture and log detailed error information during tests
/// </summary>
public class TestErrorLoggingMiddleware(RequestDelegate next, ILogger<TestErrorLoggingMiddleware> logger) {
  public async Task InvokeAsync(HttpContext context) {
    try {
      await next(context);

      // Log details for non-success responses
      if (context.Response.StatusCode >= 400) {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        var hasAuth = !string.IsNullOrEmpty(authHeader);
        var userClaims = context.User.Identity?.IsAuthenticated == true
                           ? string.Join(", ", context.User.Claims.Select(c => $"{c.Type}={c.Value}"))
                           : "Not authenticated";

        logger.LogWarning(
          "Request failed with status {StatusCode} for {Method} {Path}. Query: {Query}. HasAuth: {HasAuth}. User: {UserClaims}",
          context.Response.StatusCode,
          context.Request.Method,
          context.Request.Path,
          context.Request.QueryString,
          hasAuth,
          userClaims
        );
      }
    }
    catch (Exception ex) {
      logger.LogError(
        ex,
        "Unhandled exception in request {Method} {Path}. Query: {Query}",
        context.Request.Method,
        context.Request.Path,
        context.Request.QueryString
      );

      throw;
    }
  }
}
