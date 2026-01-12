using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to create a new tenant
/// </summary>
/// <param name="Name">Tenant name</param>
/// <param name="Slug">Tenant slug (unique identifier)</param>
/// <param name="AdminEmail">Administrator email address</param>
/// <param name="Description">Optional tenant description</param>
public record CreateTenantCommand(string Name, string Slug, string AdminEmail, string? Description = null) : ICommand<Guid>;
