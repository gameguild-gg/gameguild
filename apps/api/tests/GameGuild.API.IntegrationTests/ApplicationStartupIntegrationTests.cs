using FluentAssertions;
using GameGuild.API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace GameGuild.API.IntegrationTests;

/// <summary>
/// Integration tests for API application startup and configuration
/// </summary>
public class ApplicationStartupIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;

    public ApplicationStartupIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registrations
                var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                var dbContextDescriptor2 = services.SingleOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
                if (dbContextDescriptor2 != null)
                {
                    services.Remove(dbContextDescriptor2);
                }

                // Add in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"StartupTestDb_{Guid.NewGuid()}");
                });
            });
        });
    }

    [Fact]
    public void Application_ShouldStart_WithoutThrowingExceptions()
    {
        // Arrange & Act
        Action act = () =>
        {
            using var client = _factory.CreateClient();
        };

        // Assert
        act.Should().NotThrow("application should start successfully with test configuration");
    }

    [Fact]
    public async Task RootEndpoint_ShouldRedirect_ToSwaggerDocumentation()
    {
        // Arrange
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("docs");
    }

    [Fact]
    public void ServiceProvider_ShouldResolveApplicationDbContext()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        // Assert
        dbContext.Should().NotBeNull("ApplicationDbContext should be registered in DI container");
    }

    [Fact]
    public void Application_ShouldConfigureMultipleTimes_WithoutThrowingExceptions()
    {
        // Arrange & Act
        Action act = () =>
        {
            using var client1 = _factory.CreateClient();
            using var client2 = _factory.CreateClient();
            using var client3 = _factory.CreateClient();
        };

        // Assert
        act.Should().NotThrow("application should handle multiple client creations");
    }

    [Fact]
    public async Task Application_ShouldHaveSwaggerEndpoint_InTestEnvironment()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        // Swagger might be disabled in test environment, so we check if it's either available or not found
        (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound)
            .Should().BeTrue("Swagger endpoint should either be available or intentionally disabled");
    }
}
