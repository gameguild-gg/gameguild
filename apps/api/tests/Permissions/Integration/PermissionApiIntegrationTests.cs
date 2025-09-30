using FluentAssertions;
using GameGuild.Database;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Permissions.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace GameGuild.Tests.Permissions.Integration;

/// <summary>
/// Integration tests for Permission API endpoints
/// </summary>
public class PermissionApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly PostgreSqlContainer _postgreSqlContainer;
    private HttpClient _client = null!;

    public PermissionApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        
        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add test database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseNpgsql(_postgreSqlContainer.GetConnectionString());
                });
            });
        }).CreateClient();

        // Initialize database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _postgreSqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task GetTenantPermissions_WithValidTenantId_ShouldReturnPermissions()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Seed test data
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var permission = new TenantPermission(userId, tenantId);
        permission.AddPermission(PermissionType.Read);
        permission.AddPermission(PermissionType.Write);
        
        context.TenantPermissions.Add(permission);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/permissions/tenant/{tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CheckTenantPermission_WithValidPermission_ShouldReturnTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var permission = PermissionType.Read;

        // Seed test data
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var tenantPermission = new TenantPermission(userId, tenantId);
        tenantPermission.AddPermission(permission);
        
        context.TenantPermissions.Add(tenantPermission);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/permissions/check/tenant/{tenantId}/user/{userId}/permission/{permission}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<bool>();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckTenantPermission_WithInvalidPermission_ShouldReturnFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var permission = PermissionType.Admin;

        // Act (no seeded data, so permission should not exist)
        var response = await _client.GetAsync($"/api/permissions/check/tenant/{tenantId}/user/{userId}/permission/{permission}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<bool>();
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GrantTenantPermission_WithValidRequest_ShouldCreatePermission()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var requestBody = new
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = new[] { PermissionType.Read, PermissionType.Write },
            Reason = "Integration test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/permissions/grant", requestBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify permission was created
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var permission = await context.TenantPermissions
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TenantId == tenantId);
        
        permission.Should().NotBeNull();
        permission!.HasPermission(PermissionType.Read).Should().BeTrue();
        permission.HasPermission(PermissionType.Write).Should().BeTrue();
    }

    [Fact]
    public async Task GetPermissionTemplates_ShouldReturnSystemTemplates()
    {
        // Act
        var response = await _client.GetAsync("/api/permissions/templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("Admin");
        content.Should().Contain("Moderator");
    }

    [Fact]
    public async Task GetPermissionAnalytics_WithValidTenantId_ShouldReturnAnalytics()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var response = await _client.GetAsync($"/api/permissions/analytics/{tenantId}?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvalidatePermissionCache_WithValidTenantId_ShouldSucceed()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/permissions/cache/invalidate/{tenantId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}