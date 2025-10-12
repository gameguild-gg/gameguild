using GameGuild.Modules.Features.Entities;

namespace GameGuild.Modules.Features.Specifications;

/// <summary>
///     Specification for retrieving a feature flag by its key
/// </summary>
public class FeatureFlagByKeySpecification : SpecificationBase<FeatureFlag>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FeatureFlagByKeySpecification"/> class
    /// </summary>
    /// <param name="key">The feature flag key</param>
    public FeatureFlagByKeySpecification(string key) : base(x => x.Key == key)
    {
        AddInclude(x => x.Targets);
        EnableAsNoTracking();
    }
}

