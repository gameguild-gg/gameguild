using GameGuild.CQRS;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Command to perform bulk deactivation of tenants
/// </summary>
public abstract record BulkDeactivateTenantsCommand(IEnumerable<Guid> TenantIds) : ICommand<BulkOperationResponse>;
