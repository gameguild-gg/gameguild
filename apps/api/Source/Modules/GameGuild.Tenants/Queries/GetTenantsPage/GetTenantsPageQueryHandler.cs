using GameGuild.CQRS;
using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Queries;

/// <summary>
///     Handler for getting tenants page
/// </summary>
public class GetTenantsPageQueryHandler(ITenantRepository tenantRepository) : IQueryHandler<GetTenantsPageQuery, PagedResult<Tenant>>
{
    public async Task<PagedResult<Tenant>> Handle(GetTenantsPageQuery request, CancellationToken cancellationToken)
    {
        (var items, var totalCount) = await tenantRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            !request.IncludeInactive, // isActive filter - if IncludeInactive is true, don't filter
            cancellationToken
        );

        return new PagedResult<Tenant>(items, totalCount, request.Page, request.PageSize);
    }
}
