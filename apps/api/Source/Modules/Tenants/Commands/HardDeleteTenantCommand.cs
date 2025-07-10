using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Command to hard delete a tenant permanently
/// </summary>
public class HardDeleteTenantCommand(Guid id) : ICommand<Common.Result<bool>> {
  public Guid Id { get; init; } = id;
}