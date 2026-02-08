
namespace GameGuild.Features;

/// <summary>
///     Specification for retrieving a feature flag by its key
/// </summary>
public class FeatureFlagByKeySpecification : Specification<FeatureFlag>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FeatureFlagByKeySpecification" /> class
    /// </summary>
    /// <param name="key">The feature flag key</param>
    // ReSharper disable VirtualMemberCallInConstructor - Required for Specification pattern initialization
#pragma warning disable CA2214 // Do not call overridable methods in constructors - Required for Specification pattern
    public FeatureFlagByKeySpecification(string key) : base(x => x.Key == key)
    {
        AddInclude(x => x.Targets);
        EnableAsNoTracking();
    }
#pragma warning restore CA2214
    // ReSharper restore VirtualMemberCallInConstructor
}
