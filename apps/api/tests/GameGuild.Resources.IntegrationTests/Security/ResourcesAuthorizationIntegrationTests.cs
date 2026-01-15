using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Resources.IntegrationTests.Security;

/// <summary>
/// Integration tests verifying authentication and authorization on Resources module endpoints.
/// These tests ensure that:
/// - Unauthenticated requests receive 401 Unauthorized
/// - Cross-tenant access attempts receive 403 Forbidden
/// - User ownership validation works correctly
/// </summary>
public class ResourcesAuthorizationIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _anonymousClient;

    // Test tenant and user IDs
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public ResourcesAuthorizationIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Remove all existing DbContext registrations
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

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"ResourcesAuthTestDb_{Guid.NewGuid()}");
                });

                // Add test authentication scheme
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "TestScheme";
                    options.DefaultChallengeScheme = "TestScheme";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });

                services.AddHttpLogging(_ => { });
            });
        });

        _anonymousClient = _factory.CreateClient();
    }

    private HttpClient CreateAuthenticatedClient(Guid userId, Guid tenantId, bool isSystemAdmin = false)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "TestScheme", 
            $"{userId}|{tenantId}|{isSystemAdmin}");
        return client;
    }

    #region Tenant Quotas Controller Tests

    [Fact]
    public async Task TenantQuotas_Anonymous_Returns401()
    {
        // Act
        var response = await _anonymousClient.GetAsync($"/v1/tenants/{TenantA}/quotas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TenantQuotas_AuthenticatedWrongTenant_Returns403()
    {
        // Arrange - User from TenantA trying to access TenantB
        using var client = CreateAuthenticatedClient(UserA, TenantA);

        // Act
        var response = await client.GetAsync($"/v1/tenants/{TenantB}/quotas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TenantQuotas_AuthenticatedCorrectTenant_Succeeds()
    {
        // Arrange
        using var client = CreateAuthenticatedClient(UserA, TenantA);

        // Act
        var response = await client.GetAsync($"/v1/tenants/{TenantA}/quotas");

        // Assert - Should succeed (200 or 204, not 401/403)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Tenant Resources Controller Tests

    [Fact]
    public async Task TenantResources_Anonymous_Returns401()
    {
        // Act
        var response = await _anonymousClient.GetAsync($"/v1/tenants/{TenantA}/resources/usage-records");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TenantResources_CrossTenantAccess_Returns403()
    {
        // Arrange
        using var client = CreateAuthenticatedClient(UserA, TenantA);

        // Act
        var response = await client.GetAsync($"/v1/tenants/{TenantB}/resources/usage-records");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region User Quotas Controller Tests

    [Fact]
    public async Task UserQuotas_Anonymous_Returns401()
    {
        // Act
        var response = await _anonymousClient.GetAsync($"/v1/users/{UserA}/quotas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UserQuotas_OtherUserAccess_Returns403()
    {
        // Arrange - UserA trying to access UserB's quotas
        using var client = CreateAuthenticatedClient(UserA, TenantA);

        // Act
        var response = await client.GetAsync($"/v1/users/{UserB}/quotas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserQuotas_OwnAccess_Succeeds()
    {
        // Arrange
        using var client = CreateAuthenticatedClient(UserA, TenantA);

        // Act
        var response = await client.GetAsync($"/v1/users/{UserA}/quotas");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Admin Controller Tests

    [Fact]
    public async Task ResourcesAdmin_Anonymous_Returns401()
    {
        // Act
        var response = await _anonymousClient.GetAsync("/v1/resources/usage/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResourcesAdmin_NonAdmin_Returns403()
    {
        // Arrange - Regular user (not admin)
        using var client = CreateAuthenticatedClient(UserA, TenantA, isSystemAdmin: false);

        // Act
        var response = await client.GetAsync("/v1/resources/usage/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ResourcesAdmin_SystemAdmin_Succeeds()
    {
        // Arrange
        using var client = CreateAuthenticatedClient(UserA, TenantA, isSystemAdmin: true);

        // Act
        var response = await client.GetAsync("/v1/resources/usage/summary");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region System Admin Bypass Tests

    [Fact]
    public async Task SystemAdmin_CanAccessAnyTenant()
    {
        // Arrange - System admin from TenantA accessing TenantB
        using var client = CreateAuthenticatedClient(UserA, TenantA, isSystemAdmin: true);

        // Act
        var response = await client.GetAsync($"/v1/tenants/{TenantB}/quotas");

        // Assert - System admin should bypass tenant validation
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SystemAdmin_CanAccessAnyUser()
    {
        // Arrange
        using var client = CreateAuthenticatedClient(UserA, TenantA, isSystemAdmin: true);

        // Act
        var response = await client.GetAsync($"/v1/users/{UserB}/quotas");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    #endregion

    public void Dispose()
    {
        _anonymousClient?.Dispose();
    }
}

/// <summary>
/// Test authentication handler that creates claims from the Authorization header.
/// Format: "TestScheme userId|tenantId|isSystemAdmin"
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("TestScheme "))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            var parts = headerValue["TestScheme ".Length..].Split('|');
            if (parts.Length != 3)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid test auth format"));
            }

            var userId = parts[0];
            var tenantId = parts[1];
            var isSystemAdmin = bool.Parse(parts[2]);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new("sub", userId),
                new("tenant_id", tenantId),
                new("tid", tenantId)
            };

            if (isSystemAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "SystemAdmin"));
                claims.Add(new Claim("role", "SystemAdmin"));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            return Task.FromResult(AuthenticateResult.Fail(ex));
        }
    }
}
