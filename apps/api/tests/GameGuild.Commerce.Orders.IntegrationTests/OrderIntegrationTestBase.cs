using GameGuild.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Commerce.Orders.IntegrationTests;

/// <summary>
/// Base class for Orders module integration tests.
/// Provides WebApplicationFactory setup and common test utilities.
/// </summary>
public abstract class OrderIntegrationTestBase : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>
{
    protected readonly WebApplicationFactory<GameGuild.API.Program> Factory;
    protected readonly HttpClient Client;

    protected OrderIntegrationTestBase(WebApplicationFactory<GameGuild.API.Program> factory)
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
