using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameGuild.Data;
using GameGuild.Modules.Tenants.Models;
using GameGuild.Modules.Tenants.Services;
using GameGuild.Modules.Users.Models;
using GameGuild.Modules.Users.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace GameGuild.API.Tests.Source.Tests.Fixtures {
  /// <summary>
  /// A test fixture that provides a TestServer for E2E tests
  /// </summary>
  public class TestServerFixture : IDisposable {
    public TestServer Server { get; }

    private readonly IHost _host;
    private readonly string _testSecret = "THIS_IS_A_TEST_SECRET_DO_NOT_USE_IN_PRODUCTION_ENVIRONMENT";

    public TestServerFixture() {
      var hostBuilder = new HostBuilder()
        .ConfigureWebHost(webHost => {
            webHost.UseTestServer();
            webHost.UseStartup<TestStartup>();
            webHost.ConfigureServices(services => {
                // Configure the fixture's services
                ConfigureServices(services);
              }
            );
          }
        );

      _host = hostBuilder.Start();
      Server = _host.GetTestServer();
    }

    private void ConfigureServices(IServiceCollection services) {
      // Configure test configuration
      var configData = new Dictionary<string, string?> {
        { "Jwt:SecretKey", _testSecret },
        { "Jwt:Issuer", "GameGuild.Test" },
        { "Jwt:Audience", "GameGuild.Test.Users" },
        { "Jwt:ExpiryInMinutes", "15" },
        { "Jwt:RefreshTokenExpiryInDays", "7" },
        { "OAuth:GitHub:ClientId", "test-github-client" },
        { "OAuth:GitHub:ClientSecret", "test-github-secret" },
        { "OAuth:Google:ClientId", "test-google-client" },
        { "OAuth:Google:ClientSecret", "test-google-secret" }
      };

      var configuration = new ConfigurationBuilder()
                          .AddInMemoryCollection(configData)
                          .Build();

      services.AddSingleton<IConfiguration>(configuration);

      // Configure in-memory database
      services.AddDbContext<TestDbContext>(options =>
                                             options.UseInMemoryDatabase("GameGuildTestDb")
      );

      // Configure real ApplicationDbContext for the services
      services.AddDbContext<ApplicationDbContext>(options =>
                                                    options.UseInMemoryDatabase("GameGuildTestDb")
      );

      // Configure JWT authentication for testing
      services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
              .AddJwtBearer(options => {
                  options.TokenValidationParameters =
                    new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_testSecret)), ValidateIssuer = false, ValidateAudience = false };
                }
              );

      // Register User module services
      services.AddScoped<GameGuild.Modules.Users.Services.IUserService, UserService>();

      // Register HttpClient for OAuth service
      services.AddHttpClient();

      // Register MediatR for CQRS pattern
      services.AddMediatR(typeof(GameGuild.Modules.Auth.Services.AuthService));

      // Register Auth module services
      services.AddScoped<GameGuild.Modules.Auth.Services.IAuthService, GameGuild.Modules.Auth.Services.AuthService>();
      services.AddScoped<GameGuild.Modules.Auth.Services.IJwtTokenService, GameGuild.Modules.Auth.Services.JwtTokenService>();
      services.AddScoped<GameGuild.Modules.Auth.Services.IOAuthService, GameGuild.Modules.Auth.Services.OAuthService>();
      services.AddScoped<GameGuild.Modules.Auth.Services.IWeb3Service, GameGuild.Modules.Auth.Services.Web3Service>();
      services.AddScoped<GameGuild.Modules.Auth.Services.IEmailVerificationService, GameGuild.Modules.Auth.Services.EmailVerificationService>();
      services.AddScoped<GameGuild.Modules.Auth.Services.ITenantAuthService, GameGuild.Modules.Auth.Services.TenantAuthService>();

      // Register Tenant module services - using existing mock implementations for test simplicity
      services.AddScoped<ITenantService, MockTenantService>();
      services.AddScoped<ITenantContextService, Helpers.MockTenantContextService>();

      // Add controllers
      services.AddControllers();
    }

    public HttpClient CreateClient() { return Server.CreateClient(); }

    public string GenerateTestToken(string? userId = null) {
      if (string.IsNullOrEmpty(userId)) userId = Guid.NewGuid().ToString();

      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.ASCII.GetBytes(_testSecret);
      var tokenDescriptor = new SecurityTokenDescriptor {
        Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Email, "test@example.com") }),
        Expires = DateTime.UtcNow.AddDays(1),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
      };
      var token = tokenHandler.CreateToken(tokenDescriptor);

      return tokenHandler.WriteToken(token);
    }

    public async Task SeedTestUser(string userId, string email, string name, string password = "TestPassword123!") {
      using var scope = Server.Services.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // Create a user entity for testing using the real User model
      var user = new User {
        Id = Guid.Parse(userId),
        Email = email,
        Name = name,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };

      dbContext.Users.Add(user);
      await dbContext.SaveChangesAsync();
    }

    public async Task SeedTestUserWithProfile(string userId, string email, string name, string bio, string avatarUrl) {
      using var scope = Server.Services.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // Create a user entity with profile for testing using real models
      var user = new User {
        Id = Guid.Parse(userId),
        Email = email,
        Name = name,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };

      dbContext.Users.Add(user);
      await dbContext.SaveChangesAsync();
    }

    public void Dispose() {
      _host?.Dispose();
      Server?.Dispose();
    }
  }

  /// <summary>
  /// A startup class for the test server that configures test services
  /// </summary>
  public class TestStartup {
    public void ConfigureServices(IServiceCollection services) {
      // Add controllers and other services
      services.AddControllers();

      // Configure GraphQL - commented out to avoid dependency issues
      // services.AddGraphQLServer()
      //     .AddQueryType<TestQuery>()
      //     .AddMutationType<TestMutation>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      app.UseRouting();
      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints => {
          endpoints.MapControllers();
          // endpoints.MapGraphQL("/graphql"); // Commented out to avoid dependency issues
        }
      );
    }
  }

  // Test models, interfaces, and implementations
  public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options) {
    public DbSet<TestUserEntity> Users { get; set; }

    public DbSet<TestUserProfileEntity> UserProfiles { get; set; }
  }

  public class TestUserEntity {
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for test only (not used by EF Core)
    public TestUserProfileEntity? Profile { get; set; }
  }

  public class TestUserProfileEntity {
    public string Id { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string WebsiteUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for test only (not used by EF Core)
    public TestUserEntity? User { get; set; }
  }

  // Service interfaces and implementations
  public interface IUserService {
    Task<TestUserEntity> GetByIdAsync(string id);

    Task<TestUserEntity> CreateAsync(TestUserEntity user);

    Task<TestUserEntity?> UpdateAsync(string id, TestUserEntity user);

    Task<bool> DeleteAsync(string id);
  }

  public class TestUserService(TestDbContext dbContext) : IUserService {
    public async Task<TestUserEntity> GetByIdAsync(string id) { return await dbContext.Users.FindAsync(id); }

    public async Task<TestUserEntity> CreateAsync(TestUserEntity user) {
      dbContext.Users.Add(user);
      await dbContext.SaveChangesAsync();

      return user;
    }

    public async Task<TestUserEntity?> UpdateAsync(string id, TestUserEntity user) {
      var existingUser = await dbContext.Users.FindAsync(id);

      if (existingUser == null) return null;

      existingUser.Name = user.Name;
      existingUser.Email = user.Email;
      existingUser.UpdatedAt = DateTime.UtcNow;

      await dbContext.SaveChangesAsync();

      return existingUser;
    }

    public async Task<bool> DeleteAsync(string id) {
      var user = await dbContext.Users.FindAsync(id);

      if (user == null) return false;

      dbContext.Users.Remove(user);
      await dbContext.SaveChangesAsync();

      return true;
    }
  }

  public interface IUserProfileService {
    Task<TestUserProfileEntity> GetByIdAsync(string id);

    Task<TestUserProfileEntity> GetByUserIdAsync(string userId);

    Task<TestUserProfileEntity> CreateAsync(TestUserProfileEntity profile);

    Task<TestUserProfileEntity> UpdateAsync(string id, TestUserProfileEntity profile);

    Task<bool> DeleteAsync(string id);
  }

  public class TestUserProfileService(TestDbContext dbContext) : IUserProfileService {
    public async Task<TestUserProfileEntity> GetByIdAsync(string id) { return await dbContext.UserProfiles.FindAsync(id); }

    public async Task<TestUserProfileEntity> GetByUserIdAsync(string userId) {
      // Use LINQ to find by UserId
      return await dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId)!;
    }

    public async Task<TestUserProfileEntity> CreateAsync(TestUserProfileEntity profile) {
      dbContext.UserProfiles.Add(profile);
      await dbContext.SaveChangesAsync();

      return profile;
    }

    public async Task<TestUserProfileEntity> UpdateAsync(string id, TestUserProfileEntity profile) {
      var existingProfile = await dbContext.UserProfiles.FindAsync(id);

      if (existingProfile == null) return null;

      existingProfile.Bio = profile.Bio;
      existingProfile.AvatarUrl = profile.AvatarUrl;
      existingProfile.Location = profile.Location;
      existingProfile.WebsiteUrl = profile.WebsiteUrl;
      existingProfile.UpdatedAt = DateTime.UtcNow;

      await dbContext.SaveChangesAsync();

      return existingProfile;
    }

    public async Task<bool> DeleteAsync(string id) {
      var profile = await dbContext.UserProfiles.FindAsync(id);

      if (profile == null) return false;

      dbContext.UserProfiles.Remove(profile);
      await dbContext.SaveChangesAsync();

      return true;
    }
  }

  // Mock Tenant Service for testing
  public class MockTenantService : ITenantService {
    private readonly Dictionary<Guid, Tenant> _tenants = new();
    private readonly Dictionary<(Guid userId, Guid tenantId), TenantPermission> _permissions = new();

    public MockTenantService() {
      // Create a default test tenant
      var testTenant = new Tenant { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Test Tenant", Slug = "test-tenant", IsActive = true };
      _tenants.Add(testTenant.Id, testTenant);
    }

    public Task<IEnumerable<Tenant>> GetAllTenantsAsync() { return Task.FromResult<IEnumerable<Tenant>>(_tenants.Values); }

    public Task<Tenant?> GetTenantByIdAsync(Guid id) {
      _tenants.TryGetValue(id, out var tenant);

      return Task.FromResult(tenant);
    }

    public Task<Tenant?> GetTenantByNameAsync(string name) {
      var tenant = _tenants.Values.FirstOrDefault(t => t.Name == name);

      return Task.FromResult(tenant);
    }

    public Task<Tenant> CreateTenantAsync(Tenant tenant) {
      tenant.Id = Guid.NewGuid();
      _tenants.Add(tenant.Id, tenant);

      return Task.FromResult(tenant);
    }

    public Task<Tenant> UpdateTenantAsync(Tenant tenant) {
      _tenants[tenant.Id] = tenant;

      return Task.FromResult(tenant);
    }

    public Task<bool> SoftDeleteTenantAsync(Guid id) {
      if (_tenants.TryGetValue(id, out var tenant)) {
        tenant.SoftDelete();

        return Task.FromResult(true);
      }

      return Task.FromResult(false);
    }

    public Task<bool> RestoreTenantAsync(Guid id) {
      if (_tenants.TryGetValue(id, out var tenant)) {
        tenant.Restore();

        return Task.FromResult(true);
      }

      return Task.FromResult(false);
    }

    public Task<bool> HardDeleteTenantAsync(Guid id) { return Task.FromResult(_tenants.Remove(id)); }

    public Task<bool> ActivateTenantAsync(Guid id) {
      if (_tenants.TryGetValue(id, out var tenant)) {
        tenant.IsActive = true;

        return Task.FromResult(true);
      }

      return Task.FromResult(false);
    }

    public Task<bool> DeactivateTenantAsync(Guid id) {
      if (_tenants.TryGetValue(id, out var tenant)) {
        tenant.IsActive = false;

        return Task.FromResult(true);
      }

      return Task.FromResult(false);
    }

    public Task<IEnumerable<Tenant>> GetDeletedTenantsAsync() {
      var deletedTenants = _tenants.Values.Where(t => t.IsDeleted);

      return Task.FromResult<IEnumerable<Tenant>>(deletedTenants);
    }

    public Task<TenantPermission> AddUserToTenantAsync(Guid userId, Guid tenantId) {
      var permission = new TenantPermission { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId };
      _permissions[(userId, tenantId)] = permission;

      return Task.FromResult(permission);
    }

    public Task<bool> RemoveUserFromTenantAsync(Guid userId, Guid tenantId) { return Task.FromResult(_permissions.Remove((userId, tenantId))); }

    public Task<IEnumerable<TenantPermission>> GetUsersInTenantAsync(Guid tenantId) {
      var permissions = _permissions.Values.Where(p => p.TenantId == tenantId);

      return Task.FromResult<IEnumerable<TenantPermission>>(permissions);
    }

    public Task<IEnumerable<TenantPermission>> GetTenantsForUserAsync(Guid userId) {
      var permissions = _permissions.Values.Where(p => p.UserId == userId);

      return Task.FromResult<IEnumerable<TenantPermission>>(permissions);
    }
  }

  // GraphQL test types
  public class TestQuery {
    // GraphQL query resolvers would be defined here
    // For testing, we can leave it empty
  }

  public class TestMutation {
    // GraphQL mutation resolvers would be defined here
    // For testing, we can leave it empty
  }
}
