namespace GameGuild.Tenants.Commands;

/// <summary>
///     Request model for deleting a tenant
/// </summary>
/// <param name="ConfirmationToken">Confirmation token to prevent accidental deletions</param>
public record DeleteTenantRequest(string ConfirmationToken);
