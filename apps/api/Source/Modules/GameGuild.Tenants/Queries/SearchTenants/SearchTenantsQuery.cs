using GameGuild.CQRS;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Queries;

/// <summary>
///     Query to search tenants by various criteria
/// </summary>
public record SearchTenantsQuery(
    string? SearchTerm = null,
    bool? IsActive = null,
    bool? IsArchived = null,
    DateTime? CreatedAfter = null,
    DateTime? CreatedBefore = null,
    string? AdminEmail = null,
    int? MaxResponses = 100
) : IQuery<IEnumerable<Tenant>>;
