using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to recover a soft-deleted (archived) tenant
/// </summary>
public record RecoverTenantCommand(Guid TenantId, string Reason) : ICommand<RecoverTenantResponse>;
