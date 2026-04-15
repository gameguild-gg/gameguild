using FluentAssertions;
using GameGuild.API.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Identity.Tenants.IntegrationTests;

/// <summary>
/// Integration tests for Tenant CRUD operations
/// </summary>
public class TenantCrudIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;

    public TenantCrudIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
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
                    options.UseInMemoryDatabase($"TenantTestDb_{Guid.NewGuid()}");
                });
            });
        });

        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task CreateTenant_WithValidData_ShouldSucceed()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Slug = "test-tenant",
            AdminEmail = "admin@test.com",
            IsActive = true
        };

        // Act
        await _dbContext.Set<Tenant>().AddAsync(tenant);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedTenant = await _dbContext.Set<Tenant>().FindAsync(tenant.Id);
        savedTenant.Should().NotBeNull();
        savedTenant!.Name.Should().Be("Test Tenant");
        savedTenant.Slug.Should().Be("test-tenant");
        savedTenant.AdminEmail.Should().Be("admin@test.com");
        savedTenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetTenant_BySlug_ShouldReturnTenant()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Slug Test Tenant",
            Slug = "slug-test-tenant",
            AdminEmail = "admin@slug.com",
            IsActive = true
        };

        await _dbContext.Set<Tenant>().AddAsync(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        // Note: Global query filter handles IsDeleted check automatically
        var retrievedTenant = await _dbContext.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Slug == "slug-test-tenant");

        // Assert
        retrievedTenant.Should().NotBeNull();
        retrievedTenant!.Name.Should().Be("Slug Test Tenant");
        retrievedTenant.Id.Should().Be(tenant.Id);
    }

    [Fact]
    public async Task UpdateTenant_ShouldUpdateProperties()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            Slug = "original-slug",
            AdminEmail = "admin@original.com",
            IsActive = true
        };

        await _dbContext.Set<Tenant>().AddAsync(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        tenant.Name = "Updated Name";
        tenant.Touch();
        await _dbContext.SaveChangesAsync();

        // Assert
        var updatedTenant = await _dbContext.Set<Tenant>().FindAsync(tenant.Id);
        updatedTenant.Should().NotBeNull();
        updatedTenant!.Name.Should().Be("Updated Name");
        updatedTenant.Slug.Should().Be("original-slug");
    }

    [Fact]
    public async Task ActivateTenant_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Tenant",
            Slug = "inactive-tenant",
            AdminEmail = "admin@inactive.com",
            IsActive = false
        };

        await _dbContext.Set<Tenant>().AddAsync(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        tenant.Activate();
        await _dbContext.SaveChangesAsync();

        // Assert
        var activatedTenant = await _dbContext.Set<Tenant>().FindAsync(tenant.Id);
        activatedTenant.Should().NotBeNull();
        activatedTenant!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateTenant_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Active Tenant",
            Slug = "active-tenant",
            AdminEmail = "admin@active.com",
            IsActive = true
        };

        await _dbContext.Set<Tenant>().AddAsync(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        tenant.Deactivate();
        await _dbContext.SaveChangesAsync();

        // Assert
        var deactivatedTenant = await _dbContext.Set<Tenant>().FindAsync(tenant.Id);
        deactivatedTenant.Should().NotBeNull();
        deactivatedTenant!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveTenants_ShouldReturnOnlyActiveTenants()
    {
        // Arrange
        var activeTenant1 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Active 1",
            Slug = "active-1",
            AdminEmail = "admin@active1.com",
            IsActive = true
        };

        var activeTenant2 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Active 2",
            Slug = "active-2",
            AdminEmail = "admin@active2.com",
            IsActive = true
        };

        var inactiveTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Inactive",
            Slug = "inactive",
            AdminEmail = "admin@inactive.com",
            IsActive = false
        };

        await _dbContext.Set<Tenant>().AddRangeAsync(activeTenant1, activeTenant2, inactiveTenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var activeTenants = await _dbContext.Set<Tenant>()
            .Where(t => t.IsActive)
            .ToListAsync();

        // Assert
        activeTenants.Should().HaveCount(2);
        activeTenants.Should().AllSatisfy(t => t.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task DeleteTenant_ShouldSoftDelete()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Delete Test",
            Slug = "delete-test",
            AdminEmail = "admin@delete.com",
            IsActive = true
        };

        await _dbContext.Set<Tenant>().AddAsync(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        tenant.SoftDelete();
        await _dbContext.SaveChangesAsync();

        // Assert
        var deletedTenant = await _dbContext.Set<Tenant>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenant.Id);

        deletedTenant.Should().NotBeNull();
        deletedTenant!.IsDeleted.Should().BeTrue();
        deletedTenant.DeletedAt.Should().NotBeNull();
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _dbContext?.Dispose();
    }
}
