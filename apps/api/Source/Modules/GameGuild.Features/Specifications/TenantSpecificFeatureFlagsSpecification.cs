
namespace GameGuild.Features;

/// <summary>
///     Specification for retrieving tenant-specific feature flags
/// </summary>
public class TenantSpecificFeatureFlagsSpecification : Specification<FeatureFlag>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TenantSpecificFeatureFlagsSpecification" /> class
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    // ReSharper disable VirtualMemberCallInConstructor - Required for Specification pattern initialization
#pragma warning disable CA2214 // Do not call overridable methods in constructors - Required for Specification pattern
    public TenantSpecificFeatureFlagsSpecification(Guid tenantId) : base(x => x.TenantId == tenantId)
    {
        ApplyOrderBy(x => x.Name);
        EnableAsNoTracking();
    }
#pragma warning restore CA2214
    // ReSharper restore VirtualMemberCallInConstructor

    /// <summary>
    ///     Initializes a new instance of the <see cref="TenantSpecificFeatureFlagsSpecification" /> class
    /// </summary>
    /// <param name="tenantId">The tenant identifier as string</param>
    public TenantSpecificFeatureFlagsSpecification(string tenantId) : this(Guid.Parse(tenantId)) { }
}
