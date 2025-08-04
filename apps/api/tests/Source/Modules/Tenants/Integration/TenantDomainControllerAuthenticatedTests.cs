using System.Net;
using System.Text;
using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Tenants;
using GameGuild.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;


namespace GameGuild.Tests.Modules.Tenants.Integration;

public class TenantDomainControllerAuthenticatedTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable {
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _context;

    public TenantDomainControllerAuthenticatedTests(WebApplicationFactory<Program> factory) {
        // Use a unique database name for this test class to avoid interference
        var uniqueDbName = $"TenantDomainAuthenticatedTests_{Guid.NewGuid()}";
        _factory = IntegrationTestHelper.GetTestFactory(uniqueDbName);
        
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task CreateDomain_WithAuthenticatedAdmin_ReturnsCreatedDomain() {
        // Arrange
        var (client, user) = await AuthenticationHelper.CreateAdminUserWithClientAsync(_factory, _context);
        
        var createDto = new CreateTenantDomainDto {
            TenantId = Guid.NewGuid(), // Create a test tenant ID
            TopLevelDomain = "example.com",
            Subdomain = null,
            IsMainDomain = true,
            IsSecondaryDomain = false,
        };

        var json = JsonSerializer.Serialize(createDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/tenant-domains", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TenantDomain>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        Assert.NotNull(result);
        Assert.Equal(createDto.TenantId, result.TenantId);
        Assert.Equal(createDto.TopLevelDomain, result.TopLevelDomain);
        Assert.Equal(createDto.IsMainDomain, result.IsMainDomain);
    }

    [Fact]
    public async Task CreateDomain_WithRegularUser_ReturnsUnauthorized() {
        // Arrange
        var (client, user) = await AuthenticationHelper.CreateRegularUserWithClientAsync(_factory, _context);
        
        var createDto = new CreateTenantDomainDto {
            TenantId = Guid.NewGuid(), // Create a test tenant ID
            TopLevelDomain = "example.com",
            Subdomain = null,
            IsMainDomain = true,
            IsSecondaryDomain = false,
        };

        var json = JsonSerializer.Serialize(createDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/tenant-domains", content);

        // Assert
        // Regular users without specific permissions should get Forbidden or Unauthorized
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || 
                   response.StatusCode == HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDomainsByTenant_WithAuthenticatedUser_ReturnsOkWithDomains() {
        // Arrange
        var (client, user) = await AuthenticationHelper.CreateAdminUserWithClientAsync(_factory, _context);
        
        var testTenantId = Guid.NewGuid();
        
        // Create a test domain first
        var domain = new TenantDomain {
            Id = Guid.NewGuid(),
            TenantId = testTenantId,
            TopLevelDomain = "testdomain.com",
            Subdomain = null,
            IsMainDomain = true,
            IsSecondaryDomain = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        
        _context.TenantDomains.Add(domain);
        await _context.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/api/tenant-domains?tenantId={testTenantId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<TenantDomain[]>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal(domain.TopLevelDomain, results[0].TopLevelDomain);
    }

    public void Dispose() {
        _scope?.Dispose();
    }
}
