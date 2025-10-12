using GameGuild.Modules.Features.Entities;

namespace GameGuild.Modules.Features.Specifications;

/// <summary>
///     Specification for retrieving global feature flags
/// </summary>
public class GlobalFeatureFlagsSpecification : SpecificationBase<FeatureFlag>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GlobalFeatureFlagsSpecification"/> class
    /// </summary>
    public GlobalFeatureFlagsSpecification() : base(x => x.IsGlobal)
    {
        ApplyOrderBy(x => x.Name);
        EnableAsNoTracking();
    }
}

