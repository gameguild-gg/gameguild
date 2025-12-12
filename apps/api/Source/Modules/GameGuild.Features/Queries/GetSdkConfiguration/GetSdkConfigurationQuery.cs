using GameGuild.CQRS;
using GameGuild.Features.Models;

namespace GameGuild.Features.Queries;

/// <summary>
///     Query to get SDK configuration
/// </summary>
public record GetSdkConfigurationQuery : IQuery<SdkConfiguration>
{
    public required string Environment { get; init; }

    public string? TenantId { get; init; }

    public bool IncludeTargetingRules { get; init; }

    public bool IncludeAnalytics { get; init; }
}
