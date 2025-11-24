using GameGuild.CQRS;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Command to recover a soft-deleted (archived) tenant
/// </summary>
public record RecoverTenantCommand(Guid TenantId, string Reason) : ICommand<RecoverTenantResponse>;
