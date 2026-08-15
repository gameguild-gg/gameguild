using FluentAssertions;
using GameGuild.Identity.Tenants.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Repositories;

public class TenantRepositoryTests
{
    [Fact]
    public async Task Create_And_GetById_Should_Work()
    {
        await using var context = CreateContext();
        var repo = new TenantRepository(context);

        var tenant = new Tenant { Name = "Tenant", Slug = "tenant", AdminEmail = "admin@example.com", IsActive = true };

        var created = await repo.CreateAsync(tenant);
        var fetched = await repo.GetByIdAsync(created.Id);

        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Tenant");
    }

    [Fact]
    public async Task GetBySlugAsync_Should_Return_Tenant()
    {
        await using var context = CreateContext();
        var repo = new TenantRepository(context);

        var tenant = new Tenant { Name = "Tenant", Slug = "tenant" };
        context.Set<Tenant>().Add(tenant);
        await context.SaveChangesAsync();

        var fetched = await repo.GetBySlugAsync("tenant");

        fetched.Should().NotBeNull();
        fetched!.Slug.Should().Be("tenant");
    }

    [Fact]
    public async Task IsSlugUniqueAsync_Should_Honor_ExcludeId()
    {
        await using var context = CreateContext();
        var repo = new TenantRepository(context);

        var tenant = new Tenant { Name = "Tenant", Slug = "dup", AdminEmail = "admin@example.com" };
        context.Set<Tenant>().Add(tenant);
        await context.SaveChangesAsync();

        (await repo.IsSlugUniqueAsync("dup")).Should().BeFalse();
        (await repo.IsSlugUniqueAsync("dup", tenant.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveTenantsAsync_Should_Filter_Inactive()
    {
        await using var context = CreateContext();
        var repo = new TenantRepository(context);

        context.Set<Tenant>().AddRange(
            new Tenant { Name = "Active", Slug = "active", IsActive = true },
            new Tenant { Name = "Inactive", Slug = "inactive", IsActive = false }
        );
        await context.SaveChangesAsync();

        var result = await repo.GetActiveTenantsAsync();

        result.Should().ContainSingle(t => t.IsActive);
    }

    [Fact]
    public async Task GetPagedAsync_Should_Return_Page()
    {
        await using var context = CreateContext();
        var repo = new TenantRepository(context);

        context.Set<Tenant>().AddRange(
            new Tenant { Name = "A", Slug = "a" },
            new Tenant { Name = "B", Slug = "b" },
            new Tenant { Name = "C", Slug = "c" }
        );
        await context.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAsync(page: 2, pageSize: 2, isActive: null);

        total.Should().Be(3);
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All()
    {
        await using var context = CreateContext();
        var repo = new TenantRepository(context);

        context.Set<Tenant>().AddRange(
            new Tenant { Name = "A", Slug = "a" },
            new Tenant { Name = "B", Slug = "b" }
        );
        await context.SaveChangesAsync();

        var result = await repo.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetQueryableAsync_Should_Filter_SoftDeleted_Tenants()
    {
        await using var context = CreateContext();
        var repo = new TenantRepository(context);

        var activeTenant = new Tenant { Name = "Active", Slug = "active" };
        var deletedTenant = new Tenant { Name = "Deleted", Slug = "deleted" };

        context.Set<Tenant>().AddRange(activeTenant, deletedTenant);
        await context.SaveChangesAsync();

        typeof(EntityBase).GetProperty(nameof(EntityBase.Version))!.SetValue(deletedTenant, 1);
        await repo.DeleteAsync(deletedTenant);

        var query = await repo.GetQueryableAsync();
        var items = await query.ToListAsync();

        items.Should().ContainSingle(t => t.Id == activeTenant.Id);
        items.Should().NotContain(t => t.Id == deletedTenant.Id);
    }

    [Fact]
    public async Task UpdateAsync_Should_Persist_Changes()
    {
        await using var context = CreateContext();
        var repo = new TenantRepository(context);

        var tenant = new Tenant { Name = "Tenant", Slug = "tenant" };
        context.Set<Tenant>().Add(tenant);
        await context.SaveChangesAsync();

        tenant.Name = "Updated";
        await repo.UpdateAsync(tenant);

        var fetched = await repo.GetByIdAsync(tenant.Id);
        fetched!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteAsync_ById_Should_SoftDelete()
    {
        await using var context = CreateContext();
        var repo = new TenantRepository(context);

        var tenant = new Tenant { Name = "Tenant", Slug = "tenant" };
        context.Set<Tenant>().Add(tenant);
        await context.SaveChangesAsync();

        typeof(EntityBase).GetProperty(nameof(EntityBase.Version))!.SetValue(tenant, 1);

        await repo.DeleteAsync(tenant.Id);

        tenant.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ByEntity_Should_SoftDelete()
    {
        await using var context = CreateContext();
        var repo = new TenantRepository(context);

        var tenant = new Tenant { Name = "Tenant", Slug = "tenant" };
        context.Set<Tenant>().Add(tenant);
        await context.SaveChangesAsync();

        typeof(EntityBase).GetProperty(nameof(EntityBase.Version))!.SetValue(tenant, 1);

        await repo.DeleteAsync(tenant);

        tenant.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectDeactivatingDefaultTenant()
    {
        await using var context = CreateContext();
        var repo = new TenantRepository(context);
        var tenant = new Tenant
        {
            Name = "GameGuild Platform",
            Slug = "gameguild-platform",
            IsDefault = true,
            IsActive = true
        };
        context.Set<Tenant>().Add(tenant);
        await context.SaveChangesAsync();
        tenant.Deactivate();

        var act = () => repo.UpdateAsync(tenant);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*default tenant must remain active*");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRejectDeletingDefaultTenant()
    {
        await using var context = CreateContext();
        var repo = new TenantRepository(context);
        var tenant = new Tenant
        {
            Name = "GameGuild Platform",
            Slug = "gameguild-platform",
            IsDefault = true,
            IsActive = true
        };
        context.Set<Tenant>().Add(tenant);
        await context.SaveChangesAsync();

        var act = () => repo.DeleteAsync(tenant);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*default tenant cannot be deleted*");
    }

    [Fact]
    public async Task GetAuditLogAsync_Should_Return_Page()
    {
        await using var context = CreateContext();
        var repo = new TenantRepository(context);

        var tenant = new Tenant { Name = "Tenant", Slug = "tenant" };
        context.Set<Tenant>().Add(tenant);

        var log = new TenantAuditLog
        {
            Action = "create",
            Timestamp = DateTime.UtcNow,
            ActorName = "Admin"
        };
        log.SetProperties(new Dictionary<string, object?> { ["TenantId"] = tenant.Id });

        context.Set<TenantAuditLog>().Add(log);
        await context.SaveChangesAsync();

        var result = await repo.GetAuditLogAsync(tenant.Id, null, null, null, null, page: 1, pageSize: 10);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
    }

    private static TestTenantDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestTenantDbContext>()
            .UseInMemoryDatabase($"TenantRepo_{Guid.NewGuid()}")
            .Options;
        return new TestTenantDbContext(options);
    }
}
