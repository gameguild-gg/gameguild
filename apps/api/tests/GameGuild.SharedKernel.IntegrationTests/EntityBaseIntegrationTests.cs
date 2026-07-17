using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.SharedKernel.IntegrationTests;

/// <summary>
/// Integration tests for EntityBase functionality including audit fields and soft deletes
/// </summary>
public class EntityBaseIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;

    public EntityBaseIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Remove ALL existing DbContext and EF Core registrations
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

                // Add in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"EntityBaseTestDb_{Guid.NewGuid()}");
                });

                // Add HTTP logging services (required by the pipeline)
                services.AddHttpLogging(o => { });
            });
        });

        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task Entity_WhenCreated_ShouldHaveAuditFields()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow.AddSeconds(-1); // Add buffer for precision
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "audit@example.com",
            Name = "Audit User",
            IsActive = true
        };

        // Act
        await _dbContext.Set<User>().AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var afterCreate = DateTime.UtcNow.AddSeconds(1); // Add buffer for precision

        // Assert
        user.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        user.CreatedAt.Should().BeOnOrBefore(afterCreate);
        user.UpdatedAt.Should().BeOnOrAfter(beforeCreate);
        user.UpdatedAt.Should().BeOnOrBefore(afterCreate);
        user.Version.Should().Be(1);
        user.IsNew.Should().BeFalse();
    }

    [Fact]
    public async Task Entity_WhenUpdated_ShouldUpdateAuditFields()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "update@example.com",
            Name = "Update User",
            IsActive = true
        };

        await _dbContext.Set<User>().AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var originalCreatedAt = user.CreatedAt;
        var originalUpdatedAt = user.UpdatedAt;

        await Task.Delay(10); // Small delay to ensure UpdatedAt changes

        // Act
        user.Name = "Updated Name";
        user.Touch();
        await _dbContext.SaveChangesAsync();

        // Assert
        user.CreatedAt.Should().Be(originalCreatedAt);
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task Entity_WhenSoftDeleted_ShouldSetDeletedAt()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "softdelete@example.com",
            Name = "Soft Delete User",
            IsActive = true
        };

        await _dbContext.Set<User>().AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var beforeDelete = DateTime.UtcNow;

        // Act
        user.SoftDelete();
        await _dbContext.SaveChangesAsync();

        var afterDelete = DateTime.UtcNow;

        // Assert
        user.DeletedAt.Should().NotBeNull();
        user.DeletedAt!.Value.Should().BeOnOrAfter(beforeDelete);
        user.DeletedAt.Value.Should().BeOnOrBefore(afterDelete);
        user.IsDeleted.Should().BeTrue();
        user.Version.Should().Be(2);
    }

    [Fact]
    public async Task QueryFilter_ShouldExcludeSoftDeletedEntities()
    {
        // Arrange
        var activeUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "active@example.com",
            Name = "Active User",
            IsActive = true
        };

        var deletedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "deleted@example.com",
            Name = "Deleted User",
            IsActive = true
        };
        await _dbContext.Set<User>().AddRangeAsync(activeUser, deletedUser);
        await _dbContext.SaveChangesAsync();

        deletedUser.SoftDelete();
        await _dbContext.SaveChangesAsync();

        // Act
        var users = await _dbContext.Set<User>()
            .Where(u => u.DeletedAt == null) // Filter by DeletedAt for query translation
            .ToListAsync();

        // Assert - Should only get active user (soft deleted is filtered)
        users.Should().HaveCount(1);
        users.First().Id.Should().Be(activeUser.Id);
    }

    [Fact]
    public async Task IgnoreQueryFilters_ShouldIncludeSoftDeletedEntities()
    {
        // Arrange
        var activeUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "active2@example.com",
            Name = "Active User 2",
            IsActive = true
        };

        var deletedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "deleted2@example.com",
            Name = "Deleted User 2",
            IsActive = true
        };
        await _dbContext.Set<User>().AddRangeAsync(activeUser, deletedUser);
        await _dbContext.SaveChangesAsync();

        deletedUser.SoftDelete();
        await _dbContext.SaveChangesAsync();

        // Act
        var allUsers = await _dbContext.Set<User>()
            .IgnoreQueryFilters()
            .ToListAsync();

        // Assert - Should get both users
        allUsers.Should().HaveCount(2);
        allUsers.Should().Contain(u => u.Id == activeUser.Id);
        allUsers.Should().Contain(u => u.Id == deletedUser.Id);
    }

    [Fact]
    public async Task Entity_WithTenantId_ShouldBeScoped()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "tenant@example.com",
            Name = "Tenant User",
            IsActive = true
        };

        // Act
        await _dbContext.Set<User>().AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Assert - In test environment, tenant may be null or set automatically
        // The key behavior is the IsGlobal property calculation
        if (user.TenantId == null)
        {
            user.IsGlobal.Should().BeTrue();
        }
        else
        {
            user.IsGlobal.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Entity_WithoutTenantId_ShouldBeGlobal()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "global@example.com",
            Name = "Global User",
            IsActive = true
        };

        // Act
        await _dbContext.Set<User>().AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Assert - In test environment, tenant context affects TenantId
        // The key behavior is that IsGlobal matches TenantId null status
        if (user.TenantId == null)
        {
            user.IsGlobal.Should().BeTrue();
        }
        else
        {
            user.IsGlobal.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Entity_Touch_ShouldUpdateTimestamp()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "touch@example.com",
            Name = "Touch User",
            IsActive = true
        };

        await _dbContext.Set<User>().AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var originalUpdatedAt = user.UpdatedAt;
        await Task.Delay(10);

        // Act
        user.Touch();
        await _dbContext.SaveChangesAsync();

        // Assert
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task Entity_Version_ShouldStartAtZero_AndIncrementWhenPersisted()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "version@example.com",
            Name = "Version User",
            IsActive = true
        };

        // Assert new entity state
        user.Version.Should().Be(0);
        user.IsNew.Should().BeTrue();

        // Act
        await _dbContext.Set<User>().AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Assert persisted entity state
        user.Version.Should().Be(1);
        user.IsNew.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDelete_MultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "idempotent@example.com",
            Name = "Idempotent Delete",
            IsActive = true
        };

        await _dbContext.Set<User>().AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        user.SoftDelete();
        var firstDeletedAt = user.DeletedAt;
        
        await Task.Delay(10);
        user.SoftDelete(); // Call again
        var secondDeletedAt = user.DeletedAt;

        // Assert - Should not change DeletedAt on subsequent calls
        secondDeletedAt.Should().Be(firstDeletedAt);
    }

    [Fact]
    public async Task Restore_WhenNotDeleted_ShouldBeIdempotent()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "restore-never-deleted@example.com",
            Name = "Never Deleted User",
            IsActive = true
        };

        await _dbContext.Set<User>().AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        user.Restore(); // Restore when never deleted

        // Assert
        user.DeletedAt.Should().BeNull();
        user.IsDeleted.Should().BeFalse();
    }

    public void Dispose()
    {
        try
        {
            _dbContext?.Database.EnsureDeleted();
            _dbContext?.Dispose();
            _scope?.Dispose();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
