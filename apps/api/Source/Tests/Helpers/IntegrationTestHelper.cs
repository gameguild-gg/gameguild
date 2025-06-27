using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using GameGuild.Data;
using GameGuild.Modules.Auth.Filters;
using Microsoft.AspNetCore.Mvc.Authorization;


namespace GameGuild.Tests.Helpers {
  /// <summary>
  /// Helper class for running integration tests with a mock database
  /// </summary>
  public static class IntegrationTestHelper {
    /// <summary>
    /// Sets up a WebApplicationFactory with an in-memory database for integration tests
    /// </summary>
    /// <param name="databaseName">Optional unique database name for test isolation. If not provided, uses a default name.</param>
    public static WebApplicationFactory<Program> GetTestFactory(string? databaseName = null) {
      // Set required environment variables for tests
      // Use InMemory connection string instead of SQLite to avoid provider conflicts
      Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", "InMemory");
      Environment.SetEnvironmentVariable("USE_IN_MEMORY_DB", "true");

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

      var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
          builder.UseContentRoot(Directory.GetCurrentDirectory());

          // Override configuration settings for testing
          builder.ConfigureAppConfiguration((context, config) => {
              // Add test-specific configuration settings
              config.AddInMemoryCollection(
                new Dictionary<string, string?> {
                  { "Jwt:Key", "test-jwt-secret-key-for-integration-testing-purposes-only-minimum-32-characters" },
                  { "Jwt:SecretKey", "test-jwt-secret-key-for-integration-testing-purposes-only-minimum-32-characters" },
                  { "Jwt:Issuer", "TestIssuer" },
                  { "Jwt:Audience", "TestAudience" },
                  { "Jwt:AccessTokenExpiryMinutes", "15" },
                  { "Jwt:ExpiryInMinutes", "15" },
                  { "Jwt:RefreshTokenExpiryInDays", "7" },
                  { "OAuth:GitHub:ClientId", "test-github-client-id" },
                  { "OAuth:GitHub:ClientSecret", "test-github-client-secret" }
                }
              );
            }
          );

          builder.ConfigureServices(services => {
              // Remove the existing DbContext configurations to prevent multiple database provider registration
              var descriptor =
                services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

              if (descriptor != null) { services.Remove(descriptor); }

              // Remove all EF Core related services to prevent conflicts
              var efCoreServices = services
                                   .Where(s => s.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true)
                                   .ToList();

              foreach (var service in efCoreServices) { services.Remove(service); }

              // Add in-memory database for testing
              services.AddDbContext<ApplicationDbContext>(options => {
                  // Use the provided database name or a default one
                  var dbName = databaseName ?? "TestDatabase";
                  options.UseInMemoryDatabase(dbName);
                  // Enable sensitive data logging for tests
                  options.EnableSensitiveDataLogging();
                }
              );

              // Override auth configuration for tests
              services.AddAuthentication("Test")
                      .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                        "Test",
                        options => { }
                      );

              // Remove JWT authentication filter since it's causing issues with tests
              var filters = services
                            .Where(s => s.ServiceType == typeof(Microsoft.AspNetCore.Mvc.Filters.IFilterProvider))
                            .ToList();

              foreach (var filter in filters) { services.Remove(filter); }

              // Register mock tenant context service for tests
              services
                .AddSingleton<GameGuild.Modules.Tenant.Services.ITenantContextService, MockTenantContextService>();

              // Add controllers with test filter that bypasses authentication
              services.AddControllers(options => { options.Filters.Add(new AllowAnonymousFilter()); });

              // Add a special replacement for JwtAuthenticationFilter that always allows access in tests
              services.AddScoped<JwtAuthenticationFilter, TestJwtAuthenticationFilter>();

              // Ensure the database is created for the test DbContext
              using var scope = services.BuildServiceProvider().CreateScope();
              var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
              dbContext.Database.EnsureCreated();
            }
          );
        }
      );

      return factory;
    }
  }
}
