using GameGuild.CQRS;
using GameGuild.Modules.Features.Models;

namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to get SDK configuration
/// </summary>
public sealed record GetSdkConfigurationQuery : IRequest<SdkConfiguration>
{
    public required string Environment { get; init; }

    public string? TenantId { get; init; }

    public bool IncludeTargetingRules { get; init; }

    public bool IncludeAnalytics { get; init; }
}

