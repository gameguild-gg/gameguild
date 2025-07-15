using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Command to soft delete a tenant
/// </summary>
public class DeleteTenantCommand(Guid id) : ICommand<Common.Result<bool>> {
  public Guid Id { get; init; } = id;
}
