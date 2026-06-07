using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for getting tenants page
/// </summary>
public sealed class GetTenantsPageQueryHandler(ITenantRepository tenantRepository) : IQueryHandler<GetTenantsPageQuery, PagedResult<Tenant>>
{
    public async Task<PagedResult<Tenant>> Handle(GetTenantsPageQuery request, CancellationToken cancellationToken)
    {
        (var items, var totalCount) = await tenantRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.IsActive,
            request.IsArchived,
            request.SearchTerm,
            request.SortBy,
            request.SortDescending,
            cancellationToken
        ).ConfigureAwait(false);

        return new PagedResult<Tenant>(items, totalCount, request.Page, request.PageSize);
    }
}
