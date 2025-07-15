using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Tests.MockModules;


namespace GameGuild.Tests.Fixtures;

/// <summary>
/// Simplified test server fixture that explicitly registers TestModuleQueries for debugging
/// </summary>
public class SimpleGraphQLTestFixture : IDisposable
{
    public TestServer Server { get; }
    
    private readonly IHost _host;

    public SimpleGraphQLTestFixture()
    {
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webHost =>
            {
                webHost.UseTestServer();
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGraphQL();
                        endpoints.MapControllers();
                    });
                });
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
        // Basic configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        
        // Add logging first (required by many services)
        services.AddLogging();

        // Add database context FIRST (required by PermissionService and other common services)
        services.AddDbContext<ApplicationDbContext>(options => 
            options.UseInMemoryDatabase($"SimpleGraphQL_{Guid.NewGuid()}"));

        // Add common services AFTER database context
        services.AddCommonServices();

        // Add our test module
        GameGuild.Tests.MockModules.TestModuleDependencyInjection.AddTestModule(services);

        // Add HTTP context accessor
        services.AddHttpContextAccessor();
        
        // Add IDateTimeProvider
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Add GraphQL server with explicit TestModuleQueries registration
        services.AddGraphQLServer()
            .AddQueryType<Query>()  // Add the base Query type
            .AddTypeExtension<TestModuleQueries>();  // Add our extension

        // Add controllers
        services.AddControllers();
    }

    public HttpClient CreateClient() => Server.CreateClient();

    public void Dispose()
    {
        _host?.Dispose();
        Server?.Dispose();
    }
}
