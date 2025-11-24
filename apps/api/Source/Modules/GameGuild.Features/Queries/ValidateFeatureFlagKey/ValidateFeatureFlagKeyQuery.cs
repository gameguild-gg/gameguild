using GameGuild.CQRS;

namespace GameGuild.Features.Queries;

/// <summary>
///     Query to validate feature flag key
/// </summary>
public record ValidateFeatureFlagKeyQuery : IQuery<ValidationResult>
{
    public required string Key { get; init; }

    public Guid? ExcludeId { get; init; }
}
