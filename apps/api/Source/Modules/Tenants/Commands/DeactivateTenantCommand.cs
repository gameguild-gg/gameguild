using GameGuild.CQRS;


namespace GameGuild.Modules.Tenants;

/// <summary> Command to deactivate a tenant </summary>
public class DeactivateTenantCommand(Guid id) : ICommand<Result<bool>> {
  public Guid Id { get; init; } = id;
}
