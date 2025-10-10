using GameGuild.Core.Cqrs;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Command to automatically assign a user to a tenant based on their email domain
/// </summary>
public sealed record AssignUserByEmailDomainCommand(
    Guid UserId,
    string Email,
    string DefaultRole = "Member") : ICommand<Result<TenantMemberDto>>;
