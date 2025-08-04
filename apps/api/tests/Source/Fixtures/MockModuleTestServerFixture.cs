using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameGuild.Tests.MockModules;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using GameGuild.Tests.Infrastructure.Integration;


namespace GameGuild.Tests.Fixtures;

/// <summary>
/// Minimal test server fixture for testing GraphQL infrastructure with mock modules only
/// </summary>
public class MockModuleTestServerFixture : IDisposable
{
    public TestServer Server { get; }
    
    private readonly IHost _host;
    private readonly string _testSecret = "THIS_IS_A_TEST_SECRET_FOR_MOCK_MODULE_TESTING_DO_NOT_USE_IN_PRODUCTION";

    public MockModuleTestServerFixture()
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.UseStartup<MockModuleTestStartup>();
                webHost.ConfigureServices(services =>
                {
                    ConfigureServices(services);
                });
            });

        _host = hostBuilder.Start();
        Server = _host.GetTestServer();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Basic configuration for testing
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = _testSecret,
                ["JwtSettings:Issuer"] = "GameGuild.Tests",
                ["JwtSettings:Audience"] = "GameGuild.Tests",
                ["JwtSettings:ExpiryMinutes"] = "60",
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // For infrastructure tests, we don't need a real database
        // since we're only testing GraphQL infrastructure with mock modules
        
        // Add only minimal MediatR setup for testing (without production handlers)
        // Remove duplicate registration - let TestModuleDependencyInjection handle it
        
        // Add mock IAuthService for Authentication handlers (required by MediatR)
        services.AddScoped<GameGuild.Modules.Authentication.IAuthService, MockAuthService>();

        // Add our test module (includes test-specific MediatR handlers)
        TestModuleDependencyInjection.AddTestModule(services);

        // Add HTTP context accessor (required by pipeline behaviors)
        services.AddHttpContextAccessor();
        
        // Add IDateTimeProvider for PerformanceBehavior
        services.AddSingleton<GameGuild.Common.IDateTimeProvider, GameGuild.Common.DateTimeProvider>();

        // Build a minimal GraphQL server for infrastructure testing only
        // Don't use AddGraphQLInfrastructure as it loads all production modules that need database
        services.AddGraphQLServer()
            .AddQueryType<GameGuild.Common.Query>()
            .AddMutationType<GameGuild.Common.Mutation>()
            .AddTestModuleGraphQL() // Use the dedicated test module GraphQL registration
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .ModifyRequestOptions(opt => 
            {
                opt.IncludeExceptionDetails = true;
            })
            .AddErrorFilter(error => error); // Simple pass-through error filter

        // Add controllers for the test server
        services.AddControllers();
    }

    public HttpClient CreateClient() => Server.CreateClient();

    /// <summary>
    /// Generate a test JWT token for authentication tests
    /// </summary>
    public string GenerateTestToken(string? userId = null, string? email = null)
    {
        userId ??= Guid.NewGuid().ToString();
        email ??= "test@example.com";

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_testSecret);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim("tenant", "test-tenant"),
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "GameGuild.Tests",
            Audience = "GameGuild.Tests",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public void Dispose()
    {
        _host?.Dispose();
        Server?.Dispose();
    }
}
