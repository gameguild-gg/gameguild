using GameGuild;
using GameGuild.Modules.Features.Entities;

namespace GameGuild.Modules.Features.Specifications;

/// <summary>
///     Specification for retrieving tenant-specific feature flags
/// </summary>
public class TenantSpecificFeatureFlagsSpecification : SpecificationBase<FeatureFlag> {
    /// <summary>
    ///     Initializes a new instance of the <see cref="TenantSpecificFeatureFlagsSpecification"/> class
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    public TenantSpecificFeatureFlagsSpecification(Guid tenantId) : base(x => x.TenantId == tenantId) {
        ApplyOrderBy(x => x.Name);
        EnableAsNoTracking();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TenantSpecificFeatureFlagsSpecification"/> class
    /// </summary>
    /// <param name="tenantId">The tenant identifier as string</param>
    public TenantSpecificFeatureFlagsSpecification(string tenantId) : this(Guid.Parse(tenantId)) {
    }
}

