using MediatR;
namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to validate feature flag key
/// </summary>
public sealed record ValidateFeatureFlagKeyQuery : IRequest<ValidationResult>
{
    public required string Key { get; init; }

    public Guid? ExcludeId { get; init; }
}

