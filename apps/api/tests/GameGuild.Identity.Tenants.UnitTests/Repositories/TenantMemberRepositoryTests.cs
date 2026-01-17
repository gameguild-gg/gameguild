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

        var tenantId = Guid.NewGuid();
        var member = new TenantMember { TenantId = tenantId, UserId = Guid.NewGuid(), Role = "Member" };

        await repo.CreateAsync(member);
        var fetched = await repo.GetByUserAndTenantAsync(member.UserId, tenantId);

        fetched.Should().NotBeNull();
        fetched!.Role.Should().Be("Member");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Member()
    {
        await using var context = CreateContext();
        var repo = new TenantMemberRepository(context);

        var member = new TenantMember { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid(), Role = "Member" };
        context.Set<TenantMember>().Add(member);
        await context.SaveChangesAsync();

        var fetched = await repo.GetByIdAsync(member.Id);

        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(member.Id);
    }

    [Fact]
    public async Task GetByTenantIdAsync_Should_Filter_Inactive()
    {
        await using var context = CreateContext();
        var repo = new TenantMemberRepository(context);

        var tenantId = Guid.NewGuid();
        context.Set<TenantMember>().AddRange(
            new TenantMember { TenantId = tenantId, UserId = Guid.NewGuid(), Role = "Member", IsActive = true },
            new TenantMember { TenantId = tenantId, UserId = Guid.NewGuid(), Role = "Member", IsActive = false }
        );
        await context.SaveChangesAsync();

        var activeOnly = await repo.GetByTenantIdAsync(tenantId, includeInactive: false);
        var all = await repo.GetByTenantIdAsync(tenantId, includeInactive: true);

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
        context.Set<TenantMember>().Add(new TenantMember { TenantId = tenantId, UserId = userId, Role = "Member" });
        await context.SaveChangesAsync();

        (await repo.ExistsAsync(userId, tenantId)).Should().BeTrue();
    }

    [Fact]
    public async Task GetByUserIdAsync_Should_Filter_Inactive()
    {
        await using var context = CreateContext();
        var repo = new TenantMemberRepository(context);

        var userId = Guid.NewGuid();
        context.Set<TenantMember>().AddRange(
            new TenantMember { TenantId = Guid.NewGuid(), UserId = userId, Role = "Member", IsActive = true },
            new TenantMember { TenantId = Guid.NewGuid(), UserId = userId, Role = "Member", IsActive = false }
        );
        await context.SaveChangesAsync();

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

        var tenantId = Guid.NewGuid();
        context.Set<TenantMember>().AddRange(
            new TenantMember { TenantId = tenantId, UserId = Guid.NewGuid(), Role = "Member" },
            new TenantMember { TenantId = tenantId, UserId = Guid.NewGuid(), Role = "Member" },
            new TenantMember { TenantId = tenantId, UserId = Guid.NewGuid(), Role = "Member" }
        );
        await context.SaveChangesAsync();

        var (members, total) = await repo.GetPagedAsync(tenantId, page: 2, pageSize: 2, includeInactive: true);

        total.Should().Be(3);
        members.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteAsync_Should_SoftDelete()
    {
        await using var context = CreateContext();
        var repo = new TenantMemberRepository(context);

        var member = new TenantMember { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid(), Role = "Member" };
        context.Set<TenantMember>().Add(member);
        await context.SaveChangesAsync();

        await repo.DeleteAsync(member.Id);

        member.DeletedAt.Should().NotBeNull();
    }

    private static TestTenantDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestTenantDbContext>()
            .UseInMemoryDatabase($"TenantMemberRepo_{Guid.NewGuid()}")
            .Options;
        return new TestTenantDbContext(options);
    }
}
