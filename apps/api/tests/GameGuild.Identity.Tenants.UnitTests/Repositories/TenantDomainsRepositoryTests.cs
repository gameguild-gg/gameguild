using FluentAssertions;
using GameGuild.Identity.Tenants.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Repositories;

public class TenantDomainsRepositoryTests
{
    [Fact]
    public async Task GetByDomainAsync_Should_Handle_Subdomain()
    {
        await using var context = CreateContext();
        var repo = new TenantDomainsRepository(context);

        var tenantId = Guid.NewGuid();
        await repo.CreateAsync(new TenantDomain { TenantId = tenantId, TopLevelDomain = "example.com", Subdomain = "app" });
        await repo.CreateAsync(new TenantDomain { TenantId = tenantId, TopLevelDomain = "example.com", Subdomain = null });

        var subdomain = await repo.GetByDomainAsync("app.example.com");
        var root = await repo.GetByDomainAsync("example.com");

        subdomain.Should().NotBeNull();
        root.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByTenantIdAsync_Should_Return_Domains()
    {
        await using var context = CreateContext();
        var repo = new TenantDomainsRepository(context);

        var tenantId = Guid.NewGuid();
        await repo.CreateAsync(new TenantDomain { TenantId = tenantId, TopLevelDomain = "example.com", IsMainDomain = true });
        await repo.CreateAsync(new TenantDomain { TenantId = tenantId, TopLevelDomain = "alt.com", IsSecondaryDomain = true });

        var domains = await repo.GetByTenantIdAsync(tenantId);

        domains.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMainDomainAsync_Should_Return_Main()
    {
        await using var context = CreateContext();
        var repo = new TenantDomainsRepository(context);

        var tenantId = Guid.NewGuid();
        await repo.CreateAsync(new TenantDomain { TenantId = tenantId, TopLevelDomain = "example.com", IsMainDomain = false });
        await repo.CreateAsync(new TenantDomain { TenantId = tenantId, TopLevelDomain = "main.com", IsMainDomain = true });

        var main = await repo.GetMainDomainAsync(tenantId);

        main.Should().NotBeNull();
        main!.TopLevelDomain.Should().Be("main.com");
    }

    [Fact]
    public async Task DomainExistsAsync_Should_Respect_ExcludeId()
    {
        await using var context = CreateContext();
        var repo = new TenantDomainsRepository(context);

        var domain = new TenantDomain { TenantId = Guid.NewGuid(), TopLevelDomain = "example.com", Subdomain = "app" };
        await repo.CreateAsync(domain);

        (await repo.DomainExistsAsync("example.com", "app")).Should().BeTrue();
        (await repo.DomainExistsAsync("example.com", "app", domain.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task IsDomainUniqueAsync_Should_Return_False_When_Exists()
    {
        await using var context = CreateContext();
        var repo = new TenantDomainsRepository(context);

        await repo.CreateAsync(new TenantDomain { TenantId = Guid.NewGuid(), TopLevelDomain = "example.com", Subdomain = "app" });

        (await repo.IsDomainUniqueAsync("app.example.com")).Should().BeFalse();
    }

    [Fact]
    public async Task Create_Update_Delete_Should_Work()
    {
        await using var context = CreateContext();
        var repo = new TenantDomainsRepository(context);

        var domain = new TenantDomain { TenantId = Guid.NewGuid(), TopLevelDomain = "example.com" };
        await repo.CreateAsync(domain);

        domain.Subdomain = "api";
        await repo.UpdateAsync(domain);

        await repo.DeleteAsync(domain.Id);

        // Reload from DB to check soft delete
        var reloaded = await context.TenantDomains.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == domain.Id);
        reloaded!.DeletedAt.Should().NotBeNull();
    }

    private static TestTenantDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestTenantDbContext>()
            .UseInMemoryDatabase($"TenantDomainRepo_{Guid.NewGuid()}")
            .Options;
        return new TestTenantDbContext(options);
    }
}
