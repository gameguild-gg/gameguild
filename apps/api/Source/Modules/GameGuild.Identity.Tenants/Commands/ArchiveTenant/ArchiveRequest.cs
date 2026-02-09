namespace GameGuild.Identity.Tenants;

/// <summary>
///     Request model for archiving a tenant
/// </summary>
/// <param name="Reason">Reason for archiving the tenant</param>
public sealed record ArchiveRequest(string Reason);
