namespace GameGuild.Identity.Tenants;

/// <summary>
///     Request model for updating tenant information
/// </summary>
/// <param name="Name">New tenant name</param>
/// <param name="Description">New tenant description</param>
public record UpdateTenantRequest(string Name, string? Description);
