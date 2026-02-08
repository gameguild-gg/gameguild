
namespace GameGuild.Features;

/// <summary>
///     Specification for retrieving feature flags by type
/// </summary>
public class FeatureFlagsByTypeSpecification : Specification<FeatureFlag>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FeatureFlagsByTypeSpecification" /> class
    /// </summary>
    /// <param name="type">The feature flag type</param>
    // ReSharper disable VirtualMemberCallInConstructor - Required for Specification pattern initialization
#pragma warning disable CA2214 // Do not call overridable methods in constructors - Required for Specification pattern
    public FeatureFlagsByTypeSpecification(FeatureFlagType type) : base(x => x.Type == type)
    {
        ApplyOrderBy(x => x.Name);
        EnableAsNoTracking();
    }
#pragma warning restore CA2214
    // ReSharper restore VirtualMemberCallInConstructor
}
