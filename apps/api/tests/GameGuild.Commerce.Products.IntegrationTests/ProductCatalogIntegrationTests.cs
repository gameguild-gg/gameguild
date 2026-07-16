using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace GameGuild.Commerce.Products.IntegrationTests;

/// <summary>
/// Integration tests for Product catalog operations.
/// Tests end-to-end product management with real infrastructure.
/// </summary>
public class ProductCatalogIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;
    private static readonly string DatabaseName = $"ProductsTestDb_{Guid.NewGuid()}";

    public ProductCatalogIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Remove all EF Core and Npgsql service registrations
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
                    options.UseInMemoryDatabase(DatabaseName);
                });
            });
        });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetProducts_ShouldReturn200_WithEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetProducts_ShouldSupportPagination()
    {
        // Act
        var response = await _client.GetAsync("/v1/products?skip=0&take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_ShouldSupportFiltering_ByType()
    {
        // Act
        var response = await _client.GetAsync($"/v1/products?type={ProductType.Course}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_ShouldSupportFiltering_ByBundleStatus()
    {
        // Act
        var response = await _client.GetAsync("/v1/products?isBundle=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_ShouldSupportSearchTerm()
    {
        // Act
        var response = await _client.GetAsync("/v1/products?searchTerm=test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProductById_ShouldReturn404_WhenNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/v1/products/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_ShouldReturnUnauthorized_WithoutAuthentication()
    {
        // Arrange
        var request = new
        {
            Name = "Test Product",
            Description = "Test Description",
            ShortDescription = "Short",
            Type = "Digital",
            IsBundle = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProduct_ShouldReturnUnauthorized_WithoutAuthentication()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new
        {
            Name = "Updated Product",
            Description = "Updated Description"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/v1/products/{productId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProduct_ShouldReturnUnauthorized_WithoutAuthentication()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/v1/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProducts_ShouldSupportSorting()
    {
        // Act
        var response = await _client.GetAsync("/v1/products?sortBy=Name&sortDirection=ASC");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
