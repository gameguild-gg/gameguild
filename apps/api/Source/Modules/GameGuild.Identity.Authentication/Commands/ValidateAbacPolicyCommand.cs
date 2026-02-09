using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record ValidateAbacPolicyCommand : ICommand<AbacPolicyValidationResult>
{
    public string JsonExpression { get; init; } = string.Empty;

    public string? Name { get; init; }

    public Guid? TenantId { get; init; }
}
