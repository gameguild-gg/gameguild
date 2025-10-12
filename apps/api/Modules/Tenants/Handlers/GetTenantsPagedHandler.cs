using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Handler for GetTenantsPagedQuery </summary>
public class GetTenantsPagedHandler : IQueryHandler<GetTenantsPagedQuery, Result<PagedResult<Tenant>>>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantsPagedHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<PagedResult<Tenant>>> Handle(GetTenantsPagedQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _tenantRepository.GetQueryable();

            // Apply filters
            if (!request.IncludeInactive)
            {
                query = query.Where(t => t.IsActive);
            }

            if (!request.IncludeArchived)
            {
                query = query.Where(t => !t.IsArchived);
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(t => 
                    t.Name.ToLower().Contains(searchTerm) ||
                    (t.Description != null && t.Description.ToLower().Contains(searchTerm)) ||
                    t.Slug.ToLower().Contains(searchTerm) ||
                    (t.AdminEmail != null && t.AdminEmail.ToLower().Contains(searchTerm)));
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
                "createdat" => request.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
                "updatedat" => request.SortDescending ? query.OrderByDescending(t => t.UpdatedAt) : query.OrderBy(t => t.UpdatedAt),
                "isactive" => request.SortDescending ? query.OrderByDescending(t => t.IsActive) : query.OrderBy(t => t.IsActive),
                _ => request.SortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name)
            };

            var totalCount = await query.CountAsync(cancellationToken);
            
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var result = new PagedResult<Tenant>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return Result<PagedResult<Tenant>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<Tenant>>.Failure($"Error retrieving paged tenants: {ex.Message}");
        }
    }
}