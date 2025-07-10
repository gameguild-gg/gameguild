using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameGuild.Database;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;
using GameGuild.Common;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace GameGuild.API.Tests.Fixtures {
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

      // Add required services for authorization behaviors
      services.AddHttpContextAccessor();

      // Add date time provider
      services.AddSingleton<GameGuild.Common.IDateTimeProvider, GameGuild.Common.DateTimeProvider>();

      // Add application services (includes IDomainEventPublisher registration)
      services.AddApplication();

      // Register User module services
      services.AddScoped<GameGuild.Modules.Users.IUserService, UserService>();

      // Register HttpClient for OAuth service
      services.AddHttpClient();

      // Register Auth module services
      services.AddScoped<IAuthService, AuthService>();
      services.AddScoped<IJwtTokenService, JwtTokenService>();
      services.AddScoped<IOAuthService, OAuthService>();
      services.AddScoped<IWeb3Service, Web3Service>();
      services.AddScoped<IEmailVerificationService, EmailVerificationService>();
      services.AddScoped<ITenantAuthService, TenantAuthService>();

      // Register Tenant module services - using existing mock implementations for test simplicity
      services.AddScoped<ITenantService, MockTenantService>();
      services.AddScoped<ITenantContextService, Helpers.MockTenantContextService>();

      // Add controllers from the main application assembly
      services.AddControllers()
              .AddApplicationPart(typeof(GameGuild.Modules.Users.UsersController).Assembly);
              
      // Add GraphQL for testing with explicit schema only
      services.AddGraphQLServer()
              .AddQueryType<GameGuild.Common.GraphQL.Query>()
              .AddTypeExtension<GameGuild.Modules.Users.UserQueries>()
              .AddMutationType<GameGuild.Common.GraphQL.Mutation>()
              .AddTypeExtension<GameGuild.Modules.Users.UserMutations>()
              .AddType<GameGuild.Modules.Users.UserType>()
              .AddGlobalObjectIdentification(false)
              .ModifyOptions(opt => { 
                opt.RemoveUnreachableTypes = true;
                opt.EnsureAllNodesCanBeResolved = false;
                opt.StrictValidation = false;
                opt.EnableDirectiveIntrospection = false;
              });
    }

    public HttpClient CreateClient() { return Server.CreateClient(); }

    public string GenerateTestToken(string? userId = null) {
      if (string.IsNullOrEmpty(userId)) userId = Guid.NewGuid().ToString();

      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.ASCII.GetBytes(_testSecret);
      var tokenDescriptor = new SecurityTokenDescriptor {
        Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Email, "test@example.com")
        ]),
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

  // Test models, interfaces, and implementations

  // Service interfaces and implementations

  // Mock Tenant Service for testing

  // GraphQL test types
}
