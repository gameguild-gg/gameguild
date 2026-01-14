using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Commerce.Products.IntegrationTests;

/// <summary>
/// Base class for Products module integration tests.
/// Provides WebApplicationFactory setup and common test utilities.
/// </summary>
public abstract class ProductIntegrationTestBase : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>
{
    protected readonly WebApplicationFactory<GameGuild.API.Program> Factory;
    protected readonly HttpClient Client;

    protected ProductIntegrationTestBase(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Configure test-specific services
            });
        });

        Client = Factory.CreateClient();
    }

    protected IServiceScope CreateScope() => Factory.Services.CreateScope();
}
