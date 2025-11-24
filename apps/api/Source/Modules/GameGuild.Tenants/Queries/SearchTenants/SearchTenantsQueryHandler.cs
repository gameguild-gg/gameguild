using GameGuild.CQRS;
using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Queries;

/// <summary>
///     Handler for searching tenants
/// </summary>
public class SearchTenantsQueryHandler(ITenantRepository tenantRepository) : IQueryHandler<SearchTenantsQuery, IEnumerable<Tenant>>
{
    public async Task<IEnumerable<Tenant>> Handle(SearchTenantsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.GetAllAsync(cancellationToken);

        // Apply filters
        var filtered = tenants.AsEnumerable();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            filtered = filtered.Where(t => t.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) || t.Slug.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (request.IsActive.HasValue) { filtered = filtered.Where(t => t.IsActive == request.IsActive.Value); }

        if (!string.IsNullOrEmpty(request.AdminEmail)) { filtered = filtered.Where(t => t.AdminEmail?.Contains(request.AdminEmail, StringComparison.OrdinalIgnoreCase) == true); }

        if (request.CreatedAfter.HasValue) { filtered = filtered.Where(t => t.CreatedAt >= request.CreatedAfter.Value); }

        if (request.CreatedBefore.HasValue) { filtered = filtered.Where(t => t.CreatedAt <= request.CreatedBefore.Value); }

        if (request.MaxResponses.HasValue) { filtered = filtered.Take(request.MaxResponses.Value); }

        return filtered.ToList();
    }
}
