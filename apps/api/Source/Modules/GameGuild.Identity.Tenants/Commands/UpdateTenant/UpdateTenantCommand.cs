using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to update tenant details
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="Name">New tenant name</param>
/// <param name="Description">New tenant description</param>
public record UpdateTenantCommand(Guid TenantId, string? Name = null, string? Description = null) : ICommand;
