using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace GameGuild.Commerce.Orders.IntegrationTests;

/// <summary>
/// Integration tests for complete order workflows.
/// Tests end-to-end order processing with real infrastructure.
/// </summary>
public class OrderWorkflowIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;
    private static readonly string DatabaseName = $"OrdersTestDb_{Guid.NewGuid()}";

    public OrderWorkflowIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
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
    public async Task CreateOrder_ShouldReturnUnauthorized_WithoutAuthentication()
    {
        // Arrange
        var request = new
        {
            UserId = Guid.NewGuid(),
            IdempotencyKey = Guid.NewGuid().ToString(),
            Currency = "USD"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrder_ShouldReturnUnauthorized_WithoutAuthentication()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/v1/orders/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CompleteOrder_ShouldReturnUnauthorized_WithoutAuthentication()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new
        {
            PaymentId = Guid.NewGuid().ToString(),
            PaymentMethod = "card"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/v1/orders/{orderId}:complete", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CancelOrder_ShouldReturnUnauthorized_WithoutAuthentication()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new { Reason = "Test cancellation" };

        // Act
        var response = await _client.PostAsJsonAsync($"/v1/orders/{orderId}:cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefundOrder_ShouldReturnUnauthorized_WithoutAuthentication()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new
        {
            Amount = 100.00m,
            Reason = "Test refund"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/v1/orders/{orderId}:refund", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddItemToOrder_ShouldReturnUnauthorized_WithoutAuthentication()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new
        {
            ProductId = Guid.NewGuid(),
            Quantity = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/v1/orders/{orderId}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
