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

public sealed class CommonOpenApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CommonOpenApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                var descriptorsToRemove = services
                    .Where(descriptor => descriptor.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                                         descriptor.ServiceType == typeof(ApplicationDbContext) ||
                                         descriptor.ServiceType.FullName?.Contains("EntityFramework") == true ||
                                         descriptor.ImplementationType?.FullName?.Contains("Npgsql") == true)
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase($"CommonOpenApiTestDb_{Guid.NewGuid()}"));
                services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            });
        });
    }

    [Fact]
    public async Task Swagger_ShouldExposeSharedContractsWithoutSensitiveIdentityFields()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        var paths = document["paths"]!.AsObject();
        var schemas = document["components"]!["schemas"]!.AsObject();

        paths.Should().ContainKey("/v1/ai/status");
        paths.Should().ContainKey("/v1/orders/{orderId}");
        var publishedPropertyNames = schemas
            .SelectMany(schema => schema.Value?["properties"] is JsonObject properties
                ? properties.Select(property => property.Key)
                : [])
            .ToArray();
        publishedPropertyNames.Should().NotContain("passwordHash");
    }
}
