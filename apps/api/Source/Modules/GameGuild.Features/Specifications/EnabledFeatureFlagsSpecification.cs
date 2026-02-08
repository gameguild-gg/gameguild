
namespace GameGuild.Features;

/// <summary>
///     Specification for retrieving only enabled feature flags
/// </summary>
public class EnabledFeatureFlagsSpecification : Specification<FeatureFlag>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EnabledFeatureFlagsSpecification" /> class
    /// </summary>
    // ReSharper disable once VirtualMemberCallInConstructor - Required for Specification pattern initialization
#pragma warning disable CA2214 // Do not call overridable methods in constructors - Required for Specification pattern
    public EnabledFeatureFlagsSpecification() : base(x => x.IsEnabled) { EnableAsNoTracking(); }
#pragma warning restore CA2214
}
