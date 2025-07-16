using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameGuild.Database;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Tenants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;


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
      Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

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

      var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
          builder.UseContentRoot(Directory.GetCurrentDirectory());

          // Override configuration settings for testing
          builder.ConfigureAppConfiguration((context, config) => {
              // Add test-specific configuration settings - must match development API configuration
              config.AddInMemoryCollection(
                new Dictionary<string, string?> {
                  { "Jwt:Key", "game-guild-super-secret-key-for-development-only-minimum-32-characters" },
                  { "Jwt:SecretKey", "game-guild-super-secret-key-for-development-only-minimum-32-characters" },
                  { "Jwt:Issuer", "GameGuild.CMS" },
                  { "Jwt:Audience", "GameGuild.Users" },
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

              if (descriptor != null) services.Remove(descriptor);

              // Remove all EF Core related services to prevent conflicts
              var efCoreServices = services
                                   .Where(s => s.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true)
                                   .ToList();

              foreach (var service in efCoreServices) services.Remove(service);

              // Add in-memory database for testing
              var dbName = databaseName ?? "TestDatabase";
              
              // Add DbContextFactory for GraphQL DataLoaders first
              services.AddDbContextFactory<ApplicationDbContext>(options => {
                  options.UseInMemoryDatabase(dbName);
                  // Enable sensitive data logging for tests
                  options.EnableSensitiveDataLogging();
              });
              
              // Add regular DbContext using the factory (ensures compatible lifetimes)
              services.AddScoped<ApplicationDbContext>(provider => {
                  var factory = provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                  return factory.CreateDbContext();
              });

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

              foreach (var filter in filters) services.Remove(filter);

              // Register mock tenant context service for tests
              services
                .AddSingleton<ITenantContextService, MockTenantContextService>();

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

    /// <summary>
    /// Creates an authenticated HTTP client with a valid JWT token for the specified user
    /// </summary>
    /// <param name="factory">The WebApplicationFactory instance</param>
    /// <param name="userId">The user ID for the token</param>
    /// <param name="tenantId">Optional tenant ID for the token</param>
    /// <param name="roles">User roles for the token</param>
    /// <returns>Authenticated HttpClient</returns>
    public static HttpClient CreateAuthenticatedClient(
      WebApplicationFactory<Program> factory,
      Guid userId,
      Guid? tenantId = null,
      string[]? roles = null
    ) {
      var client = factory.CreateClient();
      
      using var scope = factory.Services.CreateScope();
      var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      
      // Get user from database to ensure we have proper data
      var user = context.Users.FirstOrDefault(u => u.Id == userId);
      
      var userDto = new UserDto {
        Id = userId,
        Username = user?.Name ?? $"testuser-{userId}",
        Email = user?.Email ?? $"test-{userId}@example.com"
      };
      
      var claims = new List<Claim> {
        new(ClaimTypes.NameIdentifier, userId.ToString()),
        new(ClaimTypes.Email, userDto.Email),
        new(ClaimTypes.Name, userDto.Username),
        new(JwtRegisteredClaimNames.Sub, userId.ToString()),
        new(JwtRegisteredClaimNames.Email, userDto.Email),
        new("username", userDto.Username)
      };
      
      // Add roles
      foreach (var role in roles ?? new[] { "User" }) {
        claims.Add(new Claim(ClaimTypes.Role, role));
      }
      
      if (tenantId.HasValue) {
        claims.Add(new Claim(JwtClaimTypes.TenantId, tenantId.Value.ToString()));
      }
      
      // Create JWT token manually with same settings as development API configuration
      var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes("game-guild-super-secret-key-for-development-only-minimum-32-characters")
      );
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
      
      var token = new JwtSecurityToken(
        issuer: "GameGuild.CMS",
        audience: "GameGuild.Users",
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(60),
        signingCredentials: creds
      );
      
      var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
      client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenString);
      
      return client;
    }

    /// <summary>
    /// Creates a test user with permissions in the database and returns an authenticated client
    /// </summary>
    /// <param name="factory">The WebApplicationFactory instance</param>
    /// <param name="userId">Optional user ID. If not provided, a new GUID will be generated</param>
    /// <param name="tenantId">Optional tenant ID. If not provided, a new GUID will be generated</param>
    /// <param name="permissionFlags1">Permission flags for the user in the tenant (default: grant all permissions)</param>
    /// <param name="permissionFlags2">Permission flags for the user in the tenant (default: grant all permissions)</param>
    /// <returns>Tuple containing the authenticated HttpClient, userId, and tenantId</returns>
    public static async Task<(HttpClient client, Guid userId, Guid tenantId)> CreateAuthenticatedTestUserAsync(
      WebApplicationFactory<Program> factory,
      Guid? userId = null,
      Guid? tenantId = null,
      ulong permissionFlags1 = ulong.MaxValue, // Grant all permissions by default
      ulong permissionFlags2 = ulong.MaxValue
    ) {
      var actualUserId = userId ?? Guid.NewGuid();
      var actualTenantId = tenantId ?? Guid.NewGuid();
      
      using var scope = factory.Services.CreateScope();
      var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      
      // Create test user if it doesn't exist
      if (!await context.Users.AnyAsync(u => u.Id == actualUserId)) {
        var user = new GameGuild.Modules.Users.User {
          Id = actualUserId,
          Name = $"testuser-{actualUserId}",
          Email = $"test-{actualUserId}@example.com",
          IsActive = true,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
      }
      
      // Create test tenant if it doesn't exist
      if (!await context.Tenants.AnyAsync(t => t.Id == actualTenantId)) {
        var tenant = new Tenant {
          Id = actualTenantId,
          Name = $"Test Tenant {actualTenantId}",
          IsActive = true,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow
        };
        context.Tenants.Add(tenant);
      }
      
      // Create tenant permission for the user (if it doesn't exist)
      if (!await context.TenantPermissions.AnyAsync(tp => tp.UserId == actualUserId && tp.TenantId == actualTenantId)) {
        var tenantPermission = new TenantPermission {
          Id = Guid.NewGuid(),
          UserId = actualUserId,
          TenantId = actualTenantId,
          PermissionFlags1 = permissionFlags1,
          PermissionFlags2 = permissionFlags2,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow
        };
        context.TenantPermissions.Add(tenantPermission);
      }
      
      await context.SaveChangesAsync();
      
      // Create authenticated client
      var client = CreateAuthenticatedClient(factory, actualUserId, actualTenantId);
      
      return (client, actualUserId, actualTenantId);
    }
  }
}
