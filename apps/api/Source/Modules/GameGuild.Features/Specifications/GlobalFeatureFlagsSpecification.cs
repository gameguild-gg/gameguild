
namespace GameGuild.Features;

/// <summary>
///     Specification for retrieving global feature flags
/// </summary>
public class GlobalFeatureFlagsSpecification : Specification<FeatureFlag>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GlobalFeatureFlagsSpecification" /> class
    /// </summary>
    // ReSharper disable VirtualMemberCallInConstructor - Required for Specification pattern initialization
#pragma warning disable CA2214 // Do not call overridable methods in constructors - Required for Specification pattern
    public GlobalFeatureFlagsSpecification() : base(x => x.IsGlobal)
    {
        ApplyOrderBy(x => x.Name);
        EnableAsNoTracking();
    }
#pragma warning restore CA2214
    // ReSharper restore VirtualMemberCallInConstructor
}
