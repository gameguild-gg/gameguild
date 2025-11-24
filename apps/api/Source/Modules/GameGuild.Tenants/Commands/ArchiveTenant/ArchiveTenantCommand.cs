using GameGuild.CQRS;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Command to archive a tenant
/// </summary>
public record ArchiveTenantCommand(Guid TenantId, string Reason) : ICommand<ArchiveTenantResponse>;
