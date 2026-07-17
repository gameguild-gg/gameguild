using System.Net;
using System.Text.Json.Nodes;
using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.API.IntegrationTests;

public sealed class ModuleOpenApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ModuleOpenApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddHttpLogging(_ => { });

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

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"ModuleOpenApiTestDb_{Guid.NewGuid()}");
                });
                services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            });
        });
    }

    [Fact]
    public async Task Swagger_ShouldExposeImplementedModuleRoutes()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var document = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        var paths = document["paths"]!.AsObject();

        paths.Should().ContainKey("/v1/ai/status");
        paths.Should().ContainKey("/v1/ai/chat");
        paths.Should().ContainKey("/v1/ai/prompt-templates");
        paths.Should().ContainKey("/api/compliance/ferpa/students/{studentUserId}/records");
        paths.Should().ContainKey("/api/social/profiles/users/{userId}");
        paths.Should().ContainKey("/api/game-jams");
        paths.Should().ContainKey("/api/learning/enrollments");
        paths.Should().ContainKey("/api/social/blog");
        paths.Should().ContainKey("/api/social/feed/users/{userId}");
        paths.Should().ContainKey("/api/social/groups");
        paths.Should().ContainKey("/api/social/reactions");
    }
}
