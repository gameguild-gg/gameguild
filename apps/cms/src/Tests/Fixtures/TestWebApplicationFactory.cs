using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GameGuild.Data;

namespace GameGuild.Tests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration and E2E tests that properly configures content root and services
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set the content root to the project directory
        var projectDir = Directory.GetCurrentDirectory();
        builder.UseContentRoot(projectDir);
        
        // Set required environment variables for tests
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", "InMemory");
        Environment.SetEnvironmentVariable("USE_IN_MEMORY_DB", "true");
        
        // Add JWT environment variables
        Environment.SetEnvironmentVariable("JWT_SECRET", "test-jwt-secret-key-for-integration-testing-purposes-only-minimum-32-characters");
        Environment.SetEnvironmentVariable("JWT_REFRESH_SECRET", "test-jwt-refresh-secret-key-for-integration-testing-minimum-32-characters");
        
        // GitHub OAuth settings (mock values for tests)
        Environment.SetEnvironmentVariable("GITHUB_CLIENT_ID", "test-github-client-id");
        Environment.SetEnvironmentVariable("GITHUB_CLIENT_SECRET", "test-github-client-secret");
        
        // Google OAuth settings (mock values for tests)
        Environment.SetEnvironmentVariable("GOOGLE_CLIENT_ID", "test-google-client-id");
        Environment.SetEnvironmentVariable("GOOGLE_CLIENT_SECRET", "test-google-client-secret");
        
        // Override configuration settings for testing
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test-specific configuration settings
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Key", "test-jwt-secret-key-for-integration-testing-purposes-only-minimum-32-characters" },
                { "Jwt:SecretKey", "test-jwt-secret-key-for-integration-testing-purposes-only-minimum-32-characters" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" },
                { "Jwt:AccessTokenExpiryMinutes", "15" },
                { "Jwt:ExpiryInMinutes", "15" },
                { "Jwt:RefreshTokenExpiryInDays", "7" },
                { "OAuth:GitHub:ClientId", "test-github-client-id" },
                { "OAuth:GitHub:ClientSecret", "test-github-client-secret" }
            });
        });
        
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext configurations to prevent multiple database provider registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            
            // Remove all EF Core related services to prevent conflicts
            var efCoreServices = services.Where(s => s.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true).ToList();
            foreach (var service in efCoreServices)
            {
                services.Remove(service);
            }
            
            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        });
    }
}
