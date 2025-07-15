using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Users;
using GameGuild.Tests.Infrastructure.Integration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;


namespace GameGuild.Tests.Fixtures {
  /// <summary>
  /// A test fixture that provides a TestServer for E2E tests
  /// </summary>
  public class TestServerFixture : IDisposable {
    public TestServer Server { get; }

    private readonly IHost _host;
    private readonly string _testSecret = "game-guild-super-secret-key-for-development-only-minimum-32-characters";

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
      
      // Ensure database is created and ready for testing
      EnsureDatabaseCreated();
    }

    private async void EnsureDatabaseCreated()
    {
      using var scope = Server.Services.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      await dbContext.Database.EnsureCreatedAsync();
    }

    private void ConfigureServices(IServiceCollection services) {
      // Configure test configuration - must match development API configuration
      var configData = new Dictionary<string, string?> {
        { "Jwt:SecretKey", _testSecret },
        { "Jwt:Issuer", "GameGuild.CMS" },
        { "Jwt:Audience", "GameGuild.Users" },
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

      // Add HTTP context accessor (required by pipeline behaviors)
      services.AddHttpContextAccessor();
      
      // Add IDateTimeProvider for PerformanceBehavior
      services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

      // Add EF Core InMemory database for testing with unique database name
      var databaseName = $"TestDatabase_{Guid.NewGuid()}";
      services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));

      // Add application services (includes IDomainEventPublisher registration)
      services.AddApplication();

      // Add essential domain modules manually  
      services.AddUserModule();
      services.AddUserProfileModule();
      services.AddTenantModule();
      services.AddProjectModule();
      services.AddCredentialsModule();
      services.AddProgramModule();
      services.AddProductModule();
      services.AddSubscriptionModule();
      services.AddPaymentModule();
      services.AddCommonServices();

      // Add test module for infrastructure testing
      services = MockModules.TestModuleDependencyInjection.AddTestModule(services);

      // Add authentication module
      services = AuthModuleDependencyInjection.AddAuthModule(services, configuration);
      
      // Replace the IAuthService with a mock for testing (avoids complex dependency chain)
      services.AddScoped<IAuthService, MockAuthService>();
      
      // Add GraphQL infrastructure with testing configuration
      services.AddGraphQLInfrastructure(DependencyInjection.GraphQLOptionsFactory.ForTesting());
      
      // Manually add simple UserProfile GraphQL implementation for testing
      services.AddGraphQLServer()
              .AddTypeExtension<UserProfiles.SimpleUserProfileQueries>()
              .AddType<GameGuild.Modules.UserProfiles.UserProfileType>();
      
      // Configure GraphQL to return 200 OK for validation errors
      services.Configure<HotChocolate.AspNetCore.GraphQLHttpOptions>(options =>
      {
          // Configure to handle validation errors properly
          options.EnableGetRequests = true;
      });

      // Add controllers from the main application assembly
      services.AddControllers()
              .AddApplicationPart(typeof(UsersController).Assembly);
    }

    public HttpClient CreateClient() { return Server.CreateClient(); }

    public string GenerateTestToken(string? userId = null) {
      if (string.IsNullOrEmpty(userId)) userId = Guid.NewGuid().ToString();

      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.ASCII.GetBytes(_testSecret);
      var tokenDescriptor = new SecurityTokenDescriptor {
        Subject = new ClaimsIdentity([
          new Claim(ClaimTypes.NameIdentifier, userId), 
          new Claim(ClaimTypes.Email, "test@example.com"),
          new Claim("sub", userId),
          new Claim("email", "test@example.com")
        ]),
        Issuer = "GameGuild.Test",
        Audience = "GameGuild.Test.Users",
        Expires = DateTime.UtcNow.AddDays(1),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
      };
      var token = tokenHandler.CreateToken(tokenDescriptor);

      return tokenHandler.WriteToken(token);
    }

    public async Task SeedTestUser(string userId, string email, string name, string password = "TestPassword123!") {
      using var scope = Server.Services.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // Ensure database is created before seeding
      await dbContext.Database.EnsureCreatedAsync();

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

      // Ensure database is created before seeding
      await dbContext.Database.EnsureCreatedAsync();

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

      // Create a user profile for testing using the real UserProfile model
      var userProfile = new GameGuild.Modules.UserProfiles.UserProfile {
        Id = Guid.Parse(userId), // UserProfile ID should match User ID for 1:1 relationship
        GivenName = name.Split(' ').FirstOrDefault() ?? string.Empty,
        FamilyName = name.Split(' ').Skip(1).FirstOrDefault() ?? string.Empty,
        DisplayName = name,
        Description = bio,
        Title = "Test Profile",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };

      dbContext.Resources.Add(userProfile);
      await dbContext.SaveChangesAsync();
    }

    public void Dispose() {
      _host?.Dispose();
      Server?.Dispose();
    }
  }

  // Test models, interfaces, and implementations

  // Service interfaces and implementations

  // Mock Tenant Service for testing

  // GraphQL test types
}
