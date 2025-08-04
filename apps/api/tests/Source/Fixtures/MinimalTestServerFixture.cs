using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameGuild.Common;
using GameGuild.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace GameGuild.Tests.Fixtures;

/// <summary>
/// Minimal test fixture that only includes GraphQL infrastructure without domain modules
/// Used to test the core GraphQL setup in isolation
/// </summary>
public class MinimalTestServerFixture : IDisposable
{
    public TestServer Server { get; }

    private readonly IHost _host;
    private readonly string _testSecret = "THIS_IS_A_TEST_SECRET_DO_NOT_USE_IN_PRODUCTION_ENVIRONMENT";

    public MinimalTestServerFixture()
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.UseStartup<MinimalTestStartup>();
                webHost.ConfigureServices(services =>
                {
                    // Configure the fixture's services
                    ConfigureServices(services);
                });
            });

        _host = hostBuilder.Start();
        Server = _host.GetTestServer();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Configure test configuration
        var configData = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", _testSecret },
            { "Jwt:Issuer", "GameGuild.Test" },
            { "Jwt:Audience", "GameGuild.Test.Users" },
            { "Jwt:ExpiryInMinutes", "15" },
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Add EF Core InMemory database for testing
        var databaseName = $"MinimalTestDatabase_{Guid.NewGuid()}";
        services.AddDbContext<ApplicationDbContext>(options => 
            options.UseInMemoryDatabase(databaseName));

        // Add only the core infrastructure without domain modules
        services.AddApplication(); // This adds MediatR and basic services
        
        // Add GraphQL infrastructure only (no modules)
        services.AddGraphQLInfrastructure();

        // Add basic controllers from the main application assembly
        services.AddControllers();
    }

    public HttpClient CreateClient()
    {
        return Server.CreateClient();
    }

    public string GenerateTestToken(string? userId = null)
    {
        if (string.IsNullOrEmpty(userId))
            userId = Guid.NewGuid().ToString();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com"),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_testSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(15);

        var token = new JwtSecurityToken(
            issuer: "GameGuild.Test",
            audience: "GameGuild.Test.Users",
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void Dispose()
    {
        _host?.Dispose();
        Server?.Dispose();
    }
}

/// <summary>
/// Minimal startup class for testing only GraphQL infrastructure
/// </summary>
public class MinimalTestStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // This will be configured by the fixture
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        // Add GraphQL endpoint
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQL("/graphql");
            endpoints.MapControllers();
        });
    }
}
