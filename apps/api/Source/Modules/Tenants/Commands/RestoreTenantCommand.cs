using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Command to restore a soft-deleted tenant
/// </summary>
public class RestoreTenantCommand(Guid id) : ICommand<Common.Result<bool>> {
  public Guid Id { get; init; } = id;
}