using GameGuild.Abstractions;
using GameGuild.Features.Entities;

namespace GameGuild.Features.Specifications;

/// <summary>
///     Specification for retrieving feature flags by type
/// </summary>
public class FeatureFlagsByTypeSpecification : SpecificationBase<FeatureFlag>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FeatureFlagsByTypeSpecification" /> class
    /// </summary>
    /// <param name="type">The feature flag type</param>
    public FeatureFlagsByTypeSpecification(FeatureFlagType type) : base(x => x.Type == type)
    {
        ApplyOrderBy(x => x.Name);
        EnableAsNoTracking();
    }
}
