using GameGuild.CQRS;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Command to perform bulk activation of tenants
/// </summary>
public abstract record BulkActivateTenantsCommand(IEnumerable<Guid> TenantIds) : ICommand<BulkOperationResponse>;
