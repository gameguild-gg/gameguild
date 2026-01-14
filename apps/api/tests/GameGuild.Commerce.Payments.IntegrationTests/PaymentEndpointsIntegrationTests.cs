using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace GameGuild.Commerce.Payments.IntegrationTests;

/// <summary>
/// Integration tests for Payment API endpoints
/// </summary>
public class PaymentEndpointsIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;
    private static readonly string DatabaseName = $"PaymentsTestDb_{Guid.NewGuid()}";

    public PaymentEndpointsIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
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

                // Add in-memory database with shared name for all requests
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(DatabaseName);
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllPayments_ShouldReturn200_WithEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/payments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAllPayments_ShouldSupportPagination()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/payments?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllPayments_ShouldSupportFiltering_ByStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/payments?status=completed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllPayments_ShouldSupportFiltering_ByDateRange()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30).ToString("O");
        var endDate = DateTime.UtcNow.ToString("O");

        // Act
        var response = await _client.GetAsync($"/api/v1/payments?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllPayments_ShouldSupportFiltering_ByTenantId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/payments?tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProcessPayment_ShouldReturnBadRequest_WithInvalidData()
    {
        // Arrange
        var request = new
        {
            TenantId = Guid.Empty,
            SubscriptionId = Guid.NewGuid(),
            Amount = -100m,
            PaymentMethodId = "invalid"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCanceledPayments_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/payments/canceled");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPaymentById_ShouldReturn404_WhenNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
