namespace GameGuild.Identity.Tenants;

/// <summary>
///     Request model for deleting a tenant
/// </summary>
/// <param name="ConfirmationToken">Confirmation token to prevent accidental deletions</param>
public sealed record DeleteTenantRequest(string ConfirmationToken);
