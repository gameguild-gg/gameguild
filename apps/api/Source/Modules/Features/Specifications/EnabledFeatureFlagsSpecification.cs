using GameGuild.Shared;
using GameGuild.Modules.Features.Entities;

namespace GameGuild.Modules.Features.Specifications;

/// <summary>
///     Specification for retrieving only enabled feature flags
/// </summary>
public class EnabledFeatureFlagsSpecification : BaseSpecification<FeatureFlag>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EnabledFeatureFlagsSpecification"/> class
    /// </summary>
    public EnabledFeatureFlagsSpecification() : base(x => x.IsEnabled)
    {
        EnableAsNoTracking();
    }
}

