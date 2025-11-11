using GameGuild.Authentication.DTOs;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record ValidateAbacPolicyCommand : ICommand<AbacPolicyValidationResult>
{
    public string JsonExpression { get; init; } = string.Empty;

    public string? Name { get; init; }

    public Guid? TenantId { get; init; }
}
