using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Command to activate a tenant
/// </summary>
public class ActivateTenantCommand(Guid id) : ICommand<Common.Result<bool>> {
  public Guid Id { get; init; } = id;
}