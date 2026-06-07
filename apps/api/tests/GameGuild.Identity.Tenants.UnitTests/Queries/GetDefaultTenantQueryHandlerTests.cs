using FluentAssertions;
using GameGuild.Identity.Tenants.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Queries;

public class GetDefaultTenantQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Default_Active_Tenant()
    {
        await using var context = CreateContext();
        context.Set<Tenant>().AddRange(
            new Tenant { Name = "Inactive", Slug = "inactive", IsDefault = true, IsActive = false },
            new Tenant { Name = "Default", Slug = "default", IsDefault = true, IsActive = true }
        );
        await context.SaveChangesAsync();

        var repo = new QueryableTenantRepository(context);
        var handler = new GetDefaultTenantQueryHandler(repo);

        var result = await handler.Handle(new GetDefaultTenantQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Default");
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_No_Default()
    {
        await using var context = CreateContext();
        context.Set<Tenant>().Add(new Tenant { Name = "Active", Slug = "active", IsDefault = false, IsActive = true });
        await context.SaveChangesAsync();

        var repo = new QueryableTenantRepository(context);
        var handler = new GetDefaultTenantQueryHandler(repo);

        var result = await handler.Handle(new GetDefaultTenantQuery(), CancellationToken.None);

        result.Should().BeNull();
    }

    private static TestTenantDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestTenantDbContext>()
            .UseInMemoryDatabase($"DefaultTenant_{Guid.NewGuid()}")
            .Options;
        return new TestTenantDbContext(options);
    }

    private sealed class QueryableTenantRepository(TestTenantDbContext context) : ITenantRepository
    {
        public Task<IQueryable<Tenant>> GetQueryableAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(context.Set<Tenant>().AsQueryable());
        }

        public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IEnumerable<Tenant> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            bool? isActive = null,
            bool? isArchived = null,
            string? searchTerm = null,
            string? sortBy = "Name",
            bool sortDescending = false,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(Tenant tenant, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<TenantAuditLogEntry>> GetAuditLogAsync(Guid tenantId, DateTime? startDate, DateTime? endDate, string? action, Guid? actorId, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
