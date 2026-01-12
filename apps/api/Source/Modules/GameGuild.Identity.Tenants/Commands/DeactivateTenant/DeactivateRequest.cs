namespace GameGuild.Identity.Tenants;

/// <summary>
///     Request model for deactivating a tenant
/// </summary>
/// <param name="Reason">Reason for deactivation</param>
public record DeactivateRequest(string Reason);
