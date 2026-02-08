using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for retrieving tenant audit log entries
/// </summary>
public class GetTenantAuditLogQueryHandler(ITenantRepository tenantRepository) 
    : IRequestHandler<GetTenantAuditLogQuery, PagedResult<TenantAuditLogEntry>>
{
    /// <summary>
    ///     Handles the audit log query by retrieving filtered and paginated audit entries
    /// </summary>
    public async Task<PagedResult<TenantAuditLogEntry>> Handle(GetTenantAuditLogQuery request, CancellationToken cancellationToken)
    {
        // Verify tenant exists
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with ID '{request.TenantId}' not found");
        }

        // Get audit log entries from repository
        var auditEntries = await tenantRepository.GetAuditLogAsync(
            request.TenantId,
            request.StartDate,
            request.EndDate,
            request.Action,
            request.ActorId,
            request.Page,
            request.PageSize,
            cancellationToken
        );

        return auditEntries;
    }
}
