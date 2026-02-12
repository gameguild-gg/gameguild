using FluentAssertions;
using GameGuild.Identity.Tenants.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Repositories;

public class TenantSettingsRepositoryTests
{
    [Fact]
    public async Task Create_Get_Update_Delete_Should_Work()
    {
        await using var context = CreateContext();
        var repo = new TenantSettingsRepository(context);

        // Create tenant first for the Include to work
        var tenant = new Tenant { Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var settings = new TenantSettings { TenantId = tenant.Id };

        await repo.CreateAsync(settings);

        var fetched = await repo.GetByTenantIdAsync(tenant.Id);
        fetched.Should().NotBeNull();

        fetched!.DefaultLanguage = "pt-BR";
        await repo.UpdateAsync(fetched);

        typeof(EntityBase).GetProperty(nameof(EntityBase.Version))!.SetValue(fetched, 1);

        await repo.DeleteAsync(tenant.Id);

        // Reload to check soft delete
        var reloaded = await context.TenantSettings.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.TenantId == tenant.Id);
        reloaded!.DeletedAt.Should().NotBeNull();
    }

    private static TestTenantDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestTenantDbContext>()
            .UseInMemoryDatabase($"TenantSettingsRepo_{Guid.NewGuid()}")
            .Options;
        return new TestTenantDbContext(options);
    }
}
