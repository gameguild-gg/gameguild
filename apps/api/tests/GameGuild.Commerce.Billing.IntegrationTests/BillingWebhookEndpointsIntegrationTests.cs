using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Commerce.Billing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GameGuild.Commerce.Billing.IntegrationTests;

/// <summary>
/// Integration tests for Billing Webhook API endpoints
/// </summary>
public class BillingWebhookEndpointsIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private const string StripeWebhookSecret = "whsec_billing_integration";
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;
    private static readonly string DatabaseName = $"BillingTestDb_{Guid.NewGuid()}";

    public BillingWebhookEndpointsIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Billing:Stripe:WebhookSecret"] = StripeWebhookSecret,
                    ["Billing:Stripe:WebhookEndpointId"] = "we_billing_integration",
                    ["Billing:Stripe:ApiVersion"] = "2023-10-16",
                    ["Billing:Stripe:LiveMode"] = "false",
                    ["Billing:Stripe:WebhookToleranceSeconds"] = "300"
                });
            });
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

    [Fact]
    public async Task GooglePayWebhook_ShouldReturnBadRequest_WithMissingAuthHeader()
    {
        // Arrange
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/billing/webhooks/google-pay", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("authorization");
    }

    [Fact]
    public async Task GooglePayWebhook_ShouldReturnBadRequest_WithMissingProjectId()
    {
        // Arrange
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        // Act
        var response = await _client.PostAsync("/api/v1/billing/webhooks/google-pay", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("project");
    }

    [Fact]
    public async Task StripeWebhook_ShouldReturnBadRequest_WithMissingSignature()
    {
        // Arrange
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/billing/webhooks/stripe", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StripeWebhook_ShouldReturnBadRequest_WithForgedSignature()
    {
        var payload = CreateStripePayload($"evt_forged_{Guid.NewGuid():N}");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/billing/webhooks/stripe")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "t=1,v1=forged");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StripeWebhook_ShouldDurablyAcceptAuthenticEventExactlyOnce()
    {
        var eventId = $"evt_authentic_{Guid.NewGuid():N}";
        var payload = CreateStripePayload(eventId);

        var firstResponse = await PostStripeWebhookAsync(payload, SignStripePayload(payload));
        var secondResponse = await PostStripeWebhookAsync(payload, SignStripePayload(payload));

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await context.Set<BillingWebhookEvent>().CountAsync(candidate => candidate.ExternalEventId == eventId))
            .Should().Be(1);
    }

    [Fact]
    public async Task PayPalWebhook_ShouldReturnBadRequest_WithMissingHeaders()
    {
        // Arrange
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/billing/webhooks/paypal", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApplePayWebhook_ShouldReturnBadRequest_WithMissingAppleHeaders()
    {
        // Arrange
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/billing/webhooks/apple-pay", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    private Task<HttpResponseMessage> PostStripeWebhookAsync(string payload, string signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/billing/webhooks/stripe")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", signature);
        return _client.SendAsync(request);
    }

    private static string CreateStripePayload(string eventId) => JsonSerializer.Serialize(new
    {
        id = eventId,
        @object = "event",
        api_version = "2023-10-16",
        created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        data = new { @object = new { id = "cus_contract", @object = "customer" } },
        livemode = false,
        pending_webhooks = 1,
        request = new { id = (string?)null, idempotency_key = (string?)null },
        type = "customer.created"
    });

    private static string SignStripePayload(string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(StripeWebhookSecret));
        var signature = Convert.ToHexString(
                hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{payload}")))
            .ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }
}
