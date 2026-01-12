namespace GameGuild.Identity.Tenants;

/// <summary>
///     Request model for recovering a soft-deleted tenant
/// </summary>
/// <param name="Reason">Reason for recovering the tenant</param>
public record RecoverRequest(string Reason);
