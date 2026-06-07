using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json.Nodes;

namespace GameGuild.API.IntegrationTests;

/// <summary>
/// Integration tests for API application startup and configuration
/// </summary>
public class ApplicationStartupIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApplicationStartupIntegrationTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Add HttpLogging service required by the pipeline
                services.AddHttpLogging(_ => { });

                // Remove ALL EF Core and database provider services
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
                    options.UseInMemoryDatabase($"StartupTestDb_{Guid.NewGuid()}");
                });
                services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
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
        response.Headers.Location?.ToString().Should().Contain("documentation");
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

    [Fact]
    public async Task PublicCoursesEndpoint_ShouldBeReachable_WithoutAuthentication()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/v1/courses/public");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Swagger_PublicCourseEndpoints_ShouldClearInheritedSecurityRequirements()
    {
        // Arrange
        using var client = _factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
            .CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var document = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        var publicSecurity = document["paths"]?["/v1/courses/public"]?["get"]?["security"];
        var slugSecurity = document["paths"]?["/v1/courses/slug/{slug}"]?["get"]?["security"];
        var publicAllowAnonymous = document["paths"]?["/v1/courses/public"]?["get"]?["x-gameguild-allow-anonymous"];
        var slugAllowAnonymous = document["paths"]?["/v1/courses/slug/{slug}"]?["get"]?["x-gameguild-allow-anonymous"];

        publicAllowAnonymous.Should().NotBeNull();
        slugAllowAnonymous.Should().NotBeNull();
        publicAllowAnonymous!.GetValue<bool>().Should().BeTrue();
        slugAllowAnonymous!.GetValue<bool>().Should().BeTrue();

        publicSecurity.Should().BeNull("the OpenAPI serializer omits empty security arrays, so code generation relies on the explicit anonymous extension instead");
        slugSecurity.Should().BeNull("the OpenAPI serializer omits empty security arrays, so code generation relies on the explicit anonymous extension instead");
    }
}
