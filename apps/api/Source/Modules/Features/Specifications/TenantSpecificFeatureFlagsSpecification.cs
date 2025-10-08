using GameGuild.Shared;
using GameGuild.Shared;
using GameGuild.Modules.Features.Entities;
using GameGuild.Shared;

namespace GameGuild.Modules.Features.Specifications;

/// <summary>
///     Specification for retrieving tenant-specific feature flags
/// </summary>
public class TenantSpecificFeatureFlagsSpecification : BaseSpecification<FeatureFlag>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TenantSpecificFeatureFlagsSpecification"/> class
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    public TenantSpecificFeatureFlagsSpecification(TenantId tenantId) : base(x => x.TenantId == tenantId)
    {
        ApplyOrderBy(x => x.Name);
        EnableAsNoTracking();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TenantSpecificFeatureFlagsSpecification"/> class
    /// </summary>
    /// <param name="tenantId">The tenant identifier as string</param>
    public TenantSpecificFeatureFlagsSpecification(string tenantId) : this(new TenantId(Guid.Parse(tenantId)))
    {
    }
}

