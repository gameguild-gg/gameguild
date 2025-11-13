using FluentAssertions;
using GameGuild.API.Data;
using GameGuild.Resources.Entities;
using GameGuild.Resources.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Resources.IntegrationTests;

/// <summary>
/// Integration tests for Resource Quota operations
/// </summary>
public class ResourceQuotaIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;

    public ResourceQuotaIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
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

                // Add in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"ResourceTestDb_{Guid.NewGuid()}");
                });
            });
        });

        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task CreateResourceQuota_WithValidData_ShouldSucceed()
    {
        // Arrange
        var testTenantId = Guid.NewGuid();
        var quota = new ResourceQuota
        {
            Id = Guid.NewGuid(),
            Type = ResourceUsageType.Storage,
            HardLimit = 1000,
            SoftLimit = 800,
            CurrentUsage = 0,
            IsActive = true,
            Period = ResourceQuotaPeriod.Monthly,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Set TenantId using reflection (protected setter)
        typeof(ResourceQuota).GetProperty("TenantId")?.GetSetMethod(nonPublic: true)?.Invoke(quota, new object[] { testTenantId });

        // Act
        await _dbContext.Set<ResourceQuota>().AddAsync(quota);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedQuota = await _dbContext.Set<ResourceQuota>().FindAsync(quota.Id);
        savedQuota.Should().NotBeNull();
        savedQuota!.Type.Should().Be(ResourceUsageType.Storage);
        savedQuota.HardLimit.Should().Be(1000);
        savedQuota.SoftLimit.Should().Be(800);
        savedQuota.CurrentUsage.Should().Be(0);
        savedQuota.IsActive.Should().BeTrue();
        savedQuota.Period.Should().Be(ResourceQuotaPeriod.Monthly);
    }

    [Fact]
    public async Task UpdateResourceQuota_ShouldUpdateUsage()
    {
        // Arrange
        var testTenantId = Guid.NewGuid();
        var quota = new ResourceQuota
        {
            Id = Guid.NewGuid(),
            Type = ResourceUsageType.ApiCalls,
            HardLimit = 1000,
            SoftLimit = 800,
            CurrentUsage = 100,
            IsActive = true,
            Period = ResourceQuotaPeriod.Daily,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Set TenantId using reflection (protected setter)
        typeof(ResourceQuota).GetProperty("TenantId")?.GetSetMethod(nonPublic: true)?.Invoke(quota, new object[] { testTenantId });

        await _dbContext.Set<ResourceQuota>().AddAsync(quota);
        await _dbContext.SaveChangesAsync();

        // Act
        quota.AddUsage(200);
        quota.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        // Assert
        var updatedQuota = await _dbContext.Set<ResourceQuota>().FindAsync(quota.Id);
        updatedQuota.Should().NotBeNull();
        updatedQuota!.CurrentUsage.Should().Be(300);
    }

    [Fact]
    public async Task GetResourceQuota_ByType_ShouldReturnQuota()
    {
        // Arrange
        var testTenantId = Guid.NewGuid();
        var quota1 = new ResourceQuota
        {
            Id = Guid.NewGuid(),
            Type = ResourceUsageType.Storage,
            HardLimit = 1000,
            IsActive = true,
            Period = ResourceQuotaPeriod.Monthly,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Set TenantId using reflection (protected setter)
        typeof(ResourceQuota).GetProperty("TenantId")?.GetSetMethod(nonPublic: true)?.Invoke(quota1, new object[] { testTenantId });

        var quota2 = new ResourceQuota
        {
            Id = Guid.NewGuid(),
            Type = ResourceUsageType.ApiCalls,
            HardLimit = 5000,
            IsActive = true,
            Period = ResourceQuotaPeriod.Daily,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Set TenantId using reflection (protected setter)
        typeof(ResourceQuota).GetProperty("TenantId")?.GetSetMethod(nonPublic: true)?.Invoke(quota2, new object[] { testTenantId });

        await _dbContext.Set<ResourceQuota>().AddRangeAsync(quota1, quota2);
        await _dbContext.SaveChangesAsync();

        // Act
        var storageQuotas = await _dbContext.Set<ResourceQuota>()
            .Where(q => q.Type == ResourceUsageType.Storage)
            .ToListAsync();

        // Assert
        storageQuotas.Should().HaveCount(1);
        storageQuotas.First().Type.Should().Be(ResourceUsageType.Storage);
    }

    [Fact]
    public async Task DeleteResourceQuota_ShouldSoftDelete()
    {
        // Arrange
        var testTenantId = Guid.NewGuid();
        var quota = new ResourceQuota
        {
            Id = Guid.NewGuid(),
            Type = ResourceUsageType.Storage,
            HardLimit = 1000,
            IsActive = true,
            Period = ResourceQuotaPeriod.Monthly,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Set TenantId using reflection (protected setter)
        typeof(ResourceQuota).GetProperty("TenantId")?.GetSetMethod(nonPublic: true)?.Invoke(quota, new object[] { testTenantId });

        await _dbContext.Set<ResourceQuota>().AddAsync(quota);
        await _dbContext.SaveChangesAsync();

        // Act
        quota.SoftDelete();
        await _dbContext.SaveChangesAsync();

        // Assert
        var deletedQuota = await _dbContext.Set<ResourceQuota>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(q => q.Id == quota.Id);
        
        deletedQuota.Should().NotBeNull();
        deletedQuota!.IsDeleted.Should().BeTrue();
        deletedQuota.DeletedAt.Should().NotBeNull();
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _dbContext?.Dispose();
    }
}
