using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to archive a tenant
/// </summary>
public record ArchiveTenantCommand(Guid TenantId, string Reason) : ICommand<ArchiveTenantResponse>;
