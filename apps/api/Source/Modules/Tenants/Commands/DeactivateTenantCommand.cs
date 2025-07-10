using GameGuild.Common;


namespace GameGuild.Modules.Tenants.Commands;

/// <summary>
/// Command to deactivate a tenant
/// </summary>
public class DeactivateTenantCommand(Guid id) : ICommand<Common.Result<bool>> {
  public Guid Id { get; init; } = id;
}