using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to get SDK configuration
/// </summary>
public sealed record GetSdkConfigurationQuery : IQuery<SdkConfiguration>
{
    public required string Environment { get; init; }

    public string? TenantId { get; init; }

    public bool IncludeTargetingRules { get; init; }

    public bool IncludeAnalytics { get; init; }
}
