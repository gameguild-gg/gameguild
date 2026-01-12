using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for getting tenants page
/// </summary>
public class GetTenantsPageQueryHandler(ITenantRepository tenantRepository) : IQueryHandler<GetTenantsPageQuery, Models.PagedResult<Tenant>>
{
    public async Task<Models.PagedResult<Tenant>> Handle(GetTenantsPageQuery request, CancellationToken cancellationToken)
    {
        (var items, var totalCount) = await tenantRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            !request.IncludeInactive, // isActive filter - if IncludeInactive is true, don't filter
            cancellationToken
        );

        return new Models.PagedResult<Tenant>(items, totalCount, request.Page, request.PageSize);
    }
}
