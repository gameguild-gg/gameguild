using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace GameGuild.API.IntegrationTests;

/// <summary>
/// Integration tests for API health endpoints
/// </summary>
public class HealthEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    public HealthEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Remove ALL existing DbContext and EF Core registrations
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                                d.ServiceType == typeof(ApplicationDbContext) ||
                                d.ServiceType.FullName?.Contains("EntityFramework") == true ||
                                d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"HealthTestDb_{Guid.NewGuid()}");
                });
                services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

                // Add HTTP logging services (required by the pipeline)
                services.AddHttpLogging(_ => { });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturn200_WithHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task GetHealth_ShouldReturnJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetHealth_ShouldIncludeStatusInformation()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.Should().NotBeNull();
        content.Should().Contain("status");
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task GetHealth_ShouldIncludeTimestamp()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("timestamp");
    }

    [Fact]
    public async Task GetHealth_MultipleCalls_ShouldReturnConsistentResults()
    {
        // Act
        var response1 = await _client.GetAsync("/health");
        var response2 = await _client.GetAsync("/health");
        var response3 = await _client.GetAsync("/health");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
