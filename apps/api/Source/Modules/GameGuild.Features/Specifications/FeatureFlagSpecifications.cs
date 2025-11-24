using GameGuild.Abstractions;
using GameGuild.Features.Entities;

namespace GameGuild.Features.Specifications;

/// <summary>
///     Specification for finding feature flags by various criteria
/// </summary>
public static class FeatureFlagSpecifications
{
    public static ISpecification<FeatureFlag> ByKey(string key) { return new FeatureFlagByKeySpecification(key); }

    public static ISpecification<FeatureFlag> EnabledFlags() { return new EnabledFeatureFlagsSpecification(); }

    public static ISpecification<FeatureFlag> GlobalFlags() { return new GlobalFeatureFlagsSpecification(); }

    public static ISpecification<FeatureFlag> TenantSpecificFlags(Guid tenantId) { return new TenantSpecificFeatureFlagsSpecification(tenantId.ToString()); }

    public static ISpecification<FeatureFlag> ByType(FeatureFlagType type) { return new FeatureFlagsByTypeSpecification(type); }
}
