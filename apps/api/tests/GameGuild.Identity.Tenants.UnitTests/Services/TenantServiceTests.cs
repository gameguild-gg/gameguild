using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Services;

/// <summary>
/// Unit tests for TenantService
/// </summary>
public class TenantServiceTests
{
    [Fact]
    public async Task GetActiveTenantsAsync_Should_Return_ReadOnly_List()
    {
        var repo = new Mock<ITenantRepository>();
        var tenants = new List<Tenant> { new() { Name = "A" }, new() { Name = "B" } };
        repo.Setup(r => r.GetActiveTenantsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tenants);

        var service = new TenantService(repo.Object, NullLogger<TenantService>.Instance);

        var result = await service.GetActiveTenantsAsync();

        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(tenants);
    }

    [Fact]
    public async Task GetTenantBySlugAsync_Should_Throw_On_Empty_Slug()
    {
        var repo = new Mock<ITenantRepository>();
        var service = new TenantService(repo.Object, NullLogger<TenantService>.Instance);

        var act = () => service.GetTenantBySlugAsync(" ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateTenantAsync_Should_Throw_When_Slug_Not_Unique()
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.IsSlugUniqueAsync("dup", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = new TenantService(repo.Object, NullLogger<TenantService>.Instance);

        var act = () => service.CreateTenantAsync("Name", "dup");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateTenantAsync_Should_Create_Tenant_When_Unique()
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.IsSlugUniqueAsync("unique", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.CreateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant t, CancellationToken _) => t);

        var service = new TenantService(repo.Object, NullLogger<TenantService>.Instance);

        var created = await service.CreateTenantAsync("Name", "unique", "desc", "admin@test.com");

        created.Name.Should().Be("Name");
        created.Slug.Should().Be("unique");
        created.Description.Should().Be("desc");
        created.AdminEmail.Should().Be("admin@test.com");
        created.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTenantAsync_Should_Throw_When_Not_Found()
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Tenant?)null);

        var service = new TenantService(repo.Object, NullLogger<TenantService>.Instance);

        var act = () => service.UpdateTenantAsync(Guid.NewGuid(), "Name");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ActivateTenantAsync_Should_Set_Active()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", Slug = "t", IsActive = false };
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant t, CancellationToken _) => t);

        var service = new TenantService(repo.Object, NullLogger<TenantService>.Instance);

        var updated = await service.ActivateTenantAsync(tenant.Id);

        updated.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateTenantAsync_Should_Set_Inactive()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", Slug = "t", IsActive = true };
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant t, CancellationToken _) => t);

        var service = new TenantService(repo.Object, NullLogger<TenantService>.Instance);

        var updated = await service.DeactivateTenantAsync(tenant.Id);

        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ArchiveTenantAsync_Should_Mark_Archived_And_Inactive()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", Slug = "t", IsActive = true };
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant t, CancellationToken _) => t);

        var service = new TenantService(repo.Object, NullLogger<TenantService>.Instance);

        var updated = await service.ArchiveTenantAsync(tenant.Id, "test");

        updated.IsArchived.Should().BeTrue();
        updated.IsActive.Should().BeFalse();
        updated.ArchivedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreTenantAsync_Should_Clear_Archive()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", Slug = "t", IsActive = false, IsArchived = true, ArchivedAt = DateTime.UtcNow };
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant t, CancellationToken _) => t);

        var service = new TenantService(repo.Object, NullLogger<TenantService>.Instance);

        var updated = await service.RestoreTenantAsync(tenant.Id);

        updated.IsArchived.Should().BeFalse();
        updated.ArchivedAt.Should().BeNull();
        updated.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 0, 1, 10)]
    [InlineData(-1, -1, 1, 10)]
    [InlineData(1, 1000, 1, 100)]
    public async Task GetTenantsPagedAsync_Should_Clamp_Page_And_PageSize(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetPagedAsync(expectedPage, expectedPageSize, true, null, null, "Name", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Tenant>(), 0));

        var service = new TenantService(repo.Object, NullLogger<TenantService>.Instance);

        await service.GetTenantsPagedAsync(page, pageSize, includeArchived: false);

        repo.Verify(r => r.GetPagedAsync(expectedPage, expectedPageSize, true, null, null, "Name", false, It.IsAny<CancellationToken>()), Times.Once);
    }
}
