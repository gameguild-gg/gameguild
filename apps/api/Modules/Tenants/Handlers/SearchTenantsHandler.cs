using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Handler for SearchTenantsQuery </summary>
public class SearchTenantsHandler : IQueryHandler<SearchTenantsQuery, Result<IEnumerable<Tenant>>>
{
    private readonly ITenantRepository _tenantRepository;

    public SearchTenantsHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<IEnumerable<Tenant>>> Handle(SearchTenantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _tenantRepository.GetQueryable();

            // Apply search term
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(t => 
                    t.Name.ToLower().Contains(searchTerm) ||
                    (t.Description != null && t.Description.ToLower().Contains(searchTerm)) ||
                    t.Slug.ToLower().Contains(searchTerm) ||
                    (t.AdminEmail != null && t.AdminEmail.ToLower().Contains(searchTerm)));
            }

            // Apply filters
            if (request.IsActive.HasValue)
            {
                query = query.Where(t => t.IsActive == request.IsActive.Value);
            }

            if (request.IsArchived.HasValue)
            {
                query = query.Where(t => t.IsArchived == request.IsArchived.Value);
            }

            if (request.CreatedAfter.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= request.CreatedAfter.Value);
            }

            if (request.CreatedBefore.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= request.CreatedBefore.Value);
            }

            if (!string.IsNullOrEmpty(request.AdminEmail))
            {
                query = query.Where(t => t.AdminEmail != null && t.AdminEmail.ToLower() == request.AdminEmail.ToLower());
            }

            // Apply limit
            if (request.MaxResults.HasValue)
            {
                query = query.Take(request.MaxResults.Value);
            }

            var tenants = await query
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<Tenant>>.Success(tenants);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Tenant>>.Failure($"Error searching tenants: {ex.Message}");
        }
    }
}