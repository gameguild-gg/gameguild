using GameGuild.CQRS;

namespace GameGuild.Identity.Authorization.Queries;

// ============================================================================
// Separation of Duties (SoD) Queries
// ============================================================================

/// <summary>
///     Query to get a SoD rule by ID
/// </summary>
public record GetSoDRuleByIdQuery(Guid RuleId) : IQuery<SoDRule?>;

public class GetSoDRuleByIdHandler(ISoDService service)
    : IQueryHandler<GetSoDRuleByIdQuery, SoDRule?>
{
    public async Task<SoDRule?> Handle(
        GetSoDRuleByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetRuleByIdAsync(request.RuleId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to get SoD rules for a tenant
/// </summary>
public record GetSoDRulesQuery(Guid? TenantId) : IQuery<List<SoDRule>>;

public class GetSoDRulesHandler(ISoDService service)
    : IQueryHandler<GetSoDRulesQuery, List<SoDRule>>
{
    public async Task<List<SoDRule>> Handle(
        GetSoDRulesQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetRulesForTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to get active SoD rules for a tenant
/// </summary>
public record GetActiveSoDRulesQuery(Guid? TenantId) : IQuery<List<SoDRule>>;

public class GetActiveSoDRulesHandler(ISoDService service)
    : IQueryHandler<GetActiveSoDRulesQuery, List<SoDRule>>
{
    public async Task<List<SoDRule>> Handle(
        GetActiveSoDRulesQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetActiveRulesAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to detect SoD violations for a user
/// </summary>
public record DetectSoDViolationsQuery(Guid UserId, Guid? TenantId) : IQuery<List<SoDViolation>>;

public class DetectSoDViolationsHandler(ISoDService service)
    : IQueryHandler<DetectSoDViolationsQuery, List<SoDViolation>>
{
    public async Task<List<SoDViolation>> Handle(
        DetectSoDViolationsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.DetectViolationsAsync(request.UserId, request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to get SoD violations for a user
/// </summary>
public record GetUserSoDViolationsQuery(Guid UserId, Guid? TenantId) : IQuery<List<SoDViolation>>;

public class GetUserSoDViolationsHandler(ISoDService service)
    : IQueryHandler<GetUserSoDViolationsQuery, List<SoDViolation>>
{
    public async Task<List<SoDViolation>> Handle(
        GetUserSoDViolationsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetViolationsForUserAsync(request.UserId, request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Query to get active SoD violations
/// </summary>
public record GetActiveSoDViolationsQuery(Guid? TenantId) : IQuery<List<SoDViolation>>;

public class GetActiveSoDViolationsHandler(ISoDService service)
    : IQueryHandler<GetActiveSoDViolationsQuery, List<SoDViolation>>
{
    public async Task<List<SoDViolation>> Handle(
        GetActiveSoDViolationsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await service.GetActiveViolationsAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}
