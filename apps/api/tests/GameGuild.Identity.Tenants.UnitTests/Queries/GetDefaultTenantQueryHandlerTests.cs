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

        public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await context.Set<Tenant>().FirstOrDefaultAsync(tenant => tenant.Id == id, cancellationToken);
        }

        public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return await context.Set<Tenant>().FirstOrDefaultAsync(tenant => tenant.Slug == slug, cancellationToken);
        }

        public async Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            return !await context.Set<Tenant>().AnyAsync(
                tenant => tenant.Slug == slug && (!excludeId.HasValue || tenant.Id != excludeId.Value),
                cancellationToken);
        }

        public async Task<IEnumerable<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
        {
            return await context.Set<Tenant>().Where(tenant => tenant.IsActive).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await context.Set<Tenant>().ToListAsync(cancellationToken);
        }

        public async Task<(IEnumerable<Tenant> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            bool? isActive = null,
            bool? isArchived = null,
            string? searchTerm = null,
            string? sortBy = "Name",
            bool sortDescending = false,
            CancellationToken cancellationToken = default)
        {
            var query = context.Set<Tenant>().AsQueryable();
            if (isActive.HasValue)
                query = query.Where(tenant => tenant.IsActive == isActive.Value);

            if (isArchived.HasValue)
                query = query.Where(tenant => tenant.IsArchived == isArchived.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(tenant => tenant.Name.Contains(searchTerm) || tenant.Slug.Contains(searchTerm));

            query = sortBy?.ToLowerInvariant() switch
            {
                "slug" => sortDescending ? query.OrderByDescending(tenant => tenant.Slug) : query.OrderBy(tenant => tenant.Slug),
                _ => sortDescending ? query.OrderByDescending(tenant => tenant.Name) : query.OrderBy(tenant => tenant.Name)
            };

            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return (items, total);
        }

        public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            context.Set<Tenant>().Add(tenant);
            await context.SaveChangesAsync(cancellationToken);
            return tenant;
        }

        public async Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            context.Set<Tenant>().Update(tenant);
            await context.SaveChangesAsync(cancellationToken);
            return tenant;
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var tenant = await GetByIdAsync(id, cancellationToken);
            if (tenant is not null)
                await DeleteAsync(tenant, cancellationToken);
        }

        public async Task DeleteAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            context.Set<Tenant>().Remove(tenant);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<PagedResult<TenantAuditLogEntry>> GetAuditLogAsync(Guid tenantId, DateTime? startDate, DateTime? endDate, string? action, Guid? actorId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = context.Set<TenantAuditLog>().Where(entry => entry.TenantId.HasValue && entry.TenantId.Value == tenantId);

            if (startDate.HasValue)
                query = query.Where(entry => entry.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(entry => entry.Timestamp <= endDate.Value);

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(entry => entry.Action == action);

            if (actorId.HasValue)
                query = query.Where(entry => entry.ActorId == actorId.Value);

            var total = await query.CountAsync(cancellationToken);
            var skip = (page - 1) * pageSize;
            var items = await query
                .OrderByDescending(entry => entry.Timestamp)
                .Skip(skip)
                .Take(pageSize)
                .Select(entry => new TenantAuditLogEntry
                {
                    Id = entry.Id,
                    TenantId = entry.TenantId!.Value,
                    Timestamp = entry.Timestamp,
                    Action = entry.Action,
                    ActorId = entry.ActorId,
                    ActorName = entry.ActorName,
                    ActorEmail = entry.ActorEmail,
                    BeforeValues = entry.BeforeValues ?? new Dictionary<string, object?>(),
                    AfterValues = entry.AfterValues ?? new Dictionary<string, object?>(),
                    IpAddress = entry.IpAddress,
                    UserAgent = entry.UserAgent,
                    CorrelationId = entry.CorrelationId,
                    Metadata = entry.Metadata ?? new Dictionary<string, string>()
                })
                .ToListAsync(cancellationToken);
            return new PagedResult<TenantAuditLogEntry>(items, total, skip, pageSize);
        }
    }
}
