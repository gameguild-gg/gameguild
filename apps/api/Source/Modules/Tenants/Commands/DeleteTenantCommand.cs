using GameGuild.CQRS;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Command to soft delete a tenant
/// </summary>
public class DeleteTenantCommand(Guid id) : ICommand<Result<bool>> {
  public Guid Id { get; init; } = id;
}
