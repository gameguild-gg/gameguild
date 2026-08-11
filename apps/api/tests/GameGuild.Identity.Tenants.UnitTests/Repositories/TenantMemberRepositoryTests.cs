using FluentAssertions;
using GameGuild.Identity.Tenants.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Repositories;

public class TenantMemberRepositoryTests
{
    [Fact]
    public async Task Create_And_GetByUserAndTenant_Should_Work()
    {
        await using var context = CreateContext();
        var repo = new TenantMemberRepository(context);

        // Create tenant first for the Include to work
        var tenant = new Tenant { Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var member = new TenantMember { TenantId = tenant.Id, UserId = Guid.NewGuid(), Role = "Member", IsActive = true };

        await repo.CreateAsync(member);
        var fetched = await repo.GetByUserAndTenantAsync(member.UserId, tenant.Id);

        fetched.Should().NotBeNull();
        fetched!.Role.Should().Be("Member");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Member()
    {
        await using var context = CreateContext();
        var repo = new TenantMemberRepository(context);

        // Create tenant first for the Include to work
        var tenant = new Tenant { Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var member = new TenantMember { TenantId = tenant.Id, UserId = Guid.NewGuid(), Role = "Member", IsActive = true };
        await repo.CreateAsync(member);

        var fetched = await repo.GetByIdAsync(member.Id);

        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(member.Id);
    }

    [Fact]
    public async Task GetByTenantIdAsync_Should_Filter_Inactive()
    {
        await using var context = CreateContext();
        var repo = new TenantMemberRepository(context);

        // Create tenant first for the Include to work
        var tenant = new Tenant { Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        await repo.CreateAsync(new TenantMember { TenantId = tenant.Id, UserId = Guid.NewGuid(), Role = "Member", IsActive = true });
        await repo.CreateAsync(new TenantMember { TenantId = tenant.Id, UserId = Guid.NewGuid(), Role = "Member", IsActive = false });

        var activeOnly = await repo.GetByTenantIdAsync(tenant.Id, includeInactive: false);
        var all = await repo.GetByTenantIdAsync(tenant.Id, includeInactive: true);

        activeOnly.Should().HaveCount(1);
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_True_When_Member_Exists()
    {
        await using var context = CreateContext();
        var repo = new TenantMemberRepository(context);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await repo.CreateAsync(new TenantMember { TenantId = tenantId, UserId = userId, Role = "Member", IsActive = true });

        (await repo.ExistsAsync(userId, tenantId)).Should().BeTrue();
    }

    [Fact]
    public async Task GetByUserIdAsync_Should_Filter_Inactive()
    {
        await using var context = CreateContext();
        var repo = new TenantMemberRepository(context);

        // Create tenants first for the Include to work
        var tenant1 = new Tenant { Name = "Test Tenant 1", Slug = "test-tenant-1" };
        var tenant2 = new Tenant { Name = "Test Tenant 2", Slug = "test-tenant-2" };
        context.Tenants.Add(tenant1);
        context.Tenants.Add(tenant2);
        await context.SaveChangesAsync();

        var userId = Guid.NewGuid();
        await repo.CreateAsync(new TenantMember { TenantId = tenant1.Id, UserId = userId, Role = "Member", IsActive = true });
        await repo.CreateAsync(new TenantMember { TenantId = tenant2.Id, UserId = userId, Role = "Member", IsActive = false });

        var activeOnly = await repo.GetByUserIdAsync(userId, includeInactive: false);
        var all = await repo.GetByUserIdAsync(userId, includeInactive: true);

        activeOnly.Should().HaveCount(1);
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_Should_Return_Page()
    {
        await using var context = CreateContext();
        var repo = new TenantMemberRepository(context);

        // Create tenant first for the Include to work
        var tenant = new Tenant { Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        await repo.CreateAsync(new TenantMember { TenantId = tenant.Id, UserId = Guid.NewGuid(), Role = "Member", IsActive = true });
        await repo.CreateAsync(new TenantMember { TenantId = tenant.Id, UserId = Guid.NewGuid(), Role = "Member", IsActive = true });
        await repo.CreateAsync(new TenantMember { TenantId = tenant.Id, UserId = Guid.NewGuid(), Role = "Member", IsActive = true });

        var (members, total) = await repo.GetPagedAsync(tenant.Id, page: 2, pageSize: 2, includeInactive: true);

        total.Should().Be(3);
        members.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteAsync_Should_SoftDelete()
    {
        await using var context = CreateContext();
        var repo = new TenantMemberRepository(context);

        // Create tenant first for the Include to work
        var tenant = new Tenant { Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var member = new TenantMember { TenantId = tenant.Id, UserId = Guid.NewGuid(), Role = "Member", IsActive = true };
        await repo.CreateAsync(member);

        typeof(EntityBase).GetProperty(nameof(EntityBase.Version))!.SetValue(member, 1);

        await repo.DeleteAsync(member.Id);

        // Need to reload from DB to check DeletedAt since the repository fetches fresh
        var reloaded = await context.TenantMembers.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == member.Id);
        reloaded!.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Inactive_DefaultTenantMembership()
    {
        await using var context = CreateContext();
        var repo = new TenantMemberRepository(context);
        var tenant = new Tenant { Name = "GameGuild", Slug = "gameguild", IsDefault = true };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        var member = new TenantMember
        {
            TenantId = tenant.Id,
            UserId = Guid.NewGuid(),
            Role = "Member",
            IsActive = false
        };

        var action = () => repo.CreateAsync(member);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*default tenant membership*");
    }

    [Fact]
    public async Task UpdateAsync_Should_Reject_Deactivating_DefaultTenantMembership()
    {
        await using var context = CreateContext();
        var repo = new TenantMemberRepository(context);
        var tenant = new Tenant { Name = "GameGuild", Slug = "gameguild", IsDefault = true };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        var member = new TenantMember
        {
            TenantId = tenant.Id,
            UserId = Guid.NewGuid(),
            Role = "Member",
            IsActive = true
        };
        await repo.CreateAsync(member);
        member.Deactivate("attempted leave");

        var action = () => repo.UpdateAsync(member);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*default tenant membership*");
    }

    [Fact]
    public async Task DeleteAsync_Should_Reject_DefaultTenantMembership()
    {
        await using var context = CreateContext();
        var repo = new TenantMemberRepository(context);
        var tenant = new Tenant { Name = "GameGuild", Slug = "gameguild", IsDefault = true };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        var member = new TenantMember
        {
            TenantId = tenant.Id,
            UserId = Guid.NewGuid(),
            Role = "Member",
            IsActive = true
        };
        await repo.CreateAsync(member);

        var action = () => repo.DeleteAsync(member.Id);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*default tenant membership*");
    }

    private static TestTenantDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestTenantDbContext>()
            .UseInMemoryDatabase($"TenantMemberRepo_{Guid.NewGuid()}")
            .Options;
        return new TestTenantDbContext(options);
    }
}
