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

        var tenantId = Guid.NewGuid();
        var settings = new TenantSettings { TenantId = tenantId };

        await repo.CreateAsync(settings);

        var fetched = await repo.GetByTenantIdAsync(tenantId);
        fetched.Should().NotBeNull();

        fetched!.DefaultLanguage = "pt-BR";
        await repo.UpdateAsync(fetched);

        await repo.DeleteAsync(tenantId);

        // Reload to check soft delete
        var reloaded = await context.TenantSettings.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.TenantId == tenantId);
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
